using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using Mudfront.Economy;
using UnityEngine;

namespace Mudfront.Net
{
    /// <summary>Server-owned economy endpoint. Uses the same domain services as offline play
    /// (PlayerRegistry, WalletLedger, FileLedgerStore). Identity comes only from the authenticated
    /// connection; clients can request but never set prices, amounts, owners or balances.</summary>
    public class EconomyServer : MonoBehaviour
    {
        public EconomyConfig Config;

        NetworkManager _nm;
        AccountAuthenticator _auth;
        FileLedgerStore _store;
        PlayerRegistry _registry;
        WalletLedger _ledger;
        readonly Dictionary<int, PlayerId> _players = new Dictionary<int, PlayerId>();
        readonly RequestRateLimiter _limiter = new RequestRateLimiter(perSecond: 10, burst: 20, kickAfterViolations: 50);

        public WalletLedger Ledger => _ledger;
        public PlayerRegistry Registry => _registry;
        public int RejectedForgedIdentity { get; private set; }
        public int RejectedRewardClaims { get; private set; }
        public int RateLimited { get; private set; }

        public void StartServer(NetworkManager nm, AccountAuthenticator auth, string ledgerPath, ushort port)
        {
            _nm = nm; _auth = auth;
            _store = new FileLedgerStore(ledgerPath);
            _registry = new PlayerRegistry();
            _ledger = AccountAuthority.Open(Config, _registry, _store, out int refunds);
            Debug.Log($"[Net] server ledger '{ledgerPath}' restored {_store.Loaded.Count} records, recovery refunds {refunds}, match {_registry.MatchId}");

            auth.AdmitAccount = Admit;
            nm.ServerManager.OnRemoteConnectionState += OnRemoteState;
            nm.ServerManager.RegisterBroadcast<WalletRequest>(OnWalletRequest);
            nm.ServerManager.RegisterBroadcast<PurchaseRequest>(OnPurchase);
            nm.ServerManager.RegisterBroadcast<RewardClaim>(OnRewardClaim);
            nm.ServerManager.StartConnection(port);
        }

        string Admit(NetworkConnection conn, string account)
        {
            var st = _registry.Join(account, out var p);
            if (st == JoinStatus.AlreadyConnected) return "account already has an active session";
            if (st == JoinStatus.InvalidAccount) return "invalid account";
            _players[conn.ClientId] = p;
            var seed = _ledger.GrantSeed(p);
            Debug.Log($"[Net] {account} -> {p} ({st}), seed {seed.Status}, balance {_ledger.Balance(p)}");
            return null;
        }

        void OnRemoteState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped) return;
            if (_players.TryGetValue(conn.ClientId, out var p)) _registry.MarkDisconnected(p);
            _players.Remove(conn.ClientId);
            _auth.Forget(conn);
            _limiter.Forget(conn.ClientId);
        }

        bool Allowed(NetworkConnection conn, out PlayerId p)
        {
            p = default;
            switch (_limiter.Check(conn.ClientId, Time.unscaledTimeAsDouble))
            {
                case RequestRateLimiter.Verdict.Drop: RateLimited++; return false;
                case RequestRateLimiter.Verdict.Kick: RateLimited++; conn.Disconnect(false); return false;
            }
            return _players.TryGetValue(conn.ClientId, out p);
        }

        void OnWalletRequest(NetworkConnection conn, WalletRequest _, Channel ch)
        {
            if (Allowed(conn, out var p)) SendSnapshot(conn, p);
        }

        void OnPurchase(NetworkConnection conn, PurchaseRequest req, Channel ch)
        {
            if (!Allowed(conn, out var p)) return;
            string account = _registry.AccountOf(p);
            string status;
            if (!string.IsNullOrEmpty(req.ActingAccountId) && req.ActingAccountId != account)
            {
                RejectedForgedIdentity++;
                status = "Rejected:ForgedIdentity";
            }
            else
            {
                var item = Config.FindItem(req.ItemId);
                // Fulfilment arrives with ECON-06/13; until a behaviour is implemented nothing is sold.
                // ClaimedPrice is never read for pricing.
                status = item == null ? "Rejected:UnknownItem" : !EconomyConfig.IsAvailable(item) ? "Rejected:NotAvailable" : "Rejected:PurchaseServicePending";
            }
            Debug.Log($"[Net] purchase {account} item={req.ItemId} claimedPrice={req.ClaimedPrice} acting='{req.ActingAccountId}' => {status}");
            _nm.ServerManager.Broadcast(conn, new TxResultMessage { RequestId = req.RequestId, Status = status, Balance = _ledger.Balance(p) });
        }

        void OnRewardClaim(NetworkConnection conn, RewardClaim req, Channel ch)
        {
            if (!Allowed(conn, out var p)) return;
            RejectedRewardClaims++;
            Debug.Log($"[Net] REJECTED client reward claim from {_registry.AccountOf(p)} amount={req.Amount}");
            _nm.ServerManager.Broadcast(conn, new TxResultMessage { RequestId = req.RequestId, Status = "Rejected:ClientRewardNotAllowed", Balance = _ledger.Balance(p) });
        }

        /// <summary>Server-side gameplay event credit (the only way money is earned).</summary>
        public TxResult ServerReward(string account, RewardReason reason, int amount, string eventId)
        {
            foreach (var kv in _players)
            {
                if (_registry.AccountOf(kv.Value) != account) continue;
                var r = _ledger.Reward(kv.Value, reason, amount, _registry.MatchId, eventId);
                if (r.Ok && _nm.ServerManager.Clients.TryGetValue(kv.Key, out var conn))
                {
                    _nm.ServerManager.Broadcast(conn, new RewardNotice { Reason = (int)reason, Amount = r.Amount, Balance = r.Balance });
                    SendSnapshot(conn, kv.Value);
                }
                return r;
            }
            return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
        }

        public IEnumerable<string> ConnectedAccounts() { foreach (var p in _players.Values) yield return _registry.AccountOf(p); }

        void SendSnapshot(NetworkConnection conn, PlayerId p)
        {
            var w = _ledger.Get(p);
            _nm.ServerManager.Broadcast(conn, new WalletSnapshot
            {
                AccountId = _registry.AccountOf(p), Balance = w?.Balance ?? 0, Earned = w?.Earned ?? 0, Spent = w?.Spent ?? 0, MatchId = _registry.MatchId,
            });
        }

        void OnDestroy() => _store?.Dispose();
    }
}
