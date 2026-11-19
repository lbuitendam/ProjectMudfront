using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using Mudfront.Economy;
using UnityEngine;

namespace Mudfront.Net
{
    /// <summary>Command-line entry for the dedicated server and scripted test clients (ECON-04 harness).
    /// Server: -mfRole server -mfPort 7770 -mfSecret S -mfLedger path [-mfExpect 2] [-mfOut file]
    /// Client: -mfRole client -mfPort 7770 -mfAccount A -mfToken T -mfScenario name -mfOut file
    /// Results are written as "key=value" lines and the process exits with 0 on PASS.</summary>
    public class NetHarnessBootstrap : MonoBehaviour
    {
        public NetworkManager Network;
        public AccountAuthenticator Auth;
        public EconomyServer Server;

        readonly List<string> _out = new List<string>();
        string _outPath;
        float _deadline;

        static string Arg(string name, string def = null)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return def;
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            _outPath = Arg("-mfOut");
            _deadline = Time.realtimeSinceStartup + float.Parse(Arg("-mfTimeout", "40"));
            string role = Arg("-mfRole");
            ushort port = ushort.Parse(Arg("-mfPort", "7770"));
            var tug = Network.GetComponent<Tugboat>();
            tug.SetPort(port);
            Network.ServerManager.SetAuthenticator(Auth);
            if (role == "server") StartCoroutine(RunServer(port));
            else if (role == "client") StartCoroutine(RunClient(port, Arg("-mfScenario", "basic")));
            else Debug.Log("[Net] no -mfRole given; idle.");
        }

        void Update()
        {
            if (_outPath != null && Time.realtimeSinceStartup > _deadline) Finish(false, "timeout");
        }

        void Log(string k, object v) { string l = $"{k}={v}"; _out.Add(l); Debug.Log("[Harness] " + l); }

        void Finish(bool pass, string why)
        {
            Log("result", pass ? "PASS" : "FAIL");
            Log("why", why);
            if (_outPath != null) File.WriteAllLines(_outPath, _out);
            _outPath = null;
            Application.Quit(pass ? 0 : 1);
        }

        // ---------- Server ----------
        IEnumerator RunServer(ushort port)
        {
            Auth.ServerSecret = Arg("-mfSecret");
            Server.StartServer(Network, Auth, Arg("-mfLedger"), port);
            Log("server_headless", Application.isBatchMode && SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
            int expect = int.Parse(Arg("-mfExpect", "2"));
            string rewardAccount = Arg("-mfRewardAccount");
            bool rewarded = false;
            while (true)
            {
                int n = 0; foreach (var _ in Server.ConnectedAccounts()) n++;
                // Server-side gameplay credit for one account only, once both are connected,
                // to prove wallet isolation across connections.
                if (!rewarded && n >= expect && rewardAccount != null)
                {
                    yield return new WaitForSeconds(1f);
                    var r = Server.ServerReward(rewardAccount, RewardReason.Kill, 150, "harness-kill-1");
                    Log("server_reward", $"{rewardAccount}:{r.Status}:{r.Balance}");
                    rewarded = true;
                }
                if (File.Exists(Arg("-mfStopFile", "__none__"))) break;
                yield return new WaitForSeconds(0.25f);
            }
            Log("rejected_forged_identity", Server.RejectedForgedIdentity);
            Log("rejected_reward_claims", Server.RejectedRewardClaims);
            Log("rate_limited", Server.RateLimited);
            foreach (var r in Server.Ledger.Records) Log("ledger", $"{r.AccountId}:{r.Kind}:{r.Amount}:{r.BalanceAfter}");
            Log("reconciles", Server.Ledger.Reconciles(out _));
            Finish(Server.Ledger.Reconciles(out var why), why ?? "server stopped by harness");
        }

        // ---------- Client scenarios ----------
        AuthResult? _auth;
        WalletSnapshot? _wallet;
        readonly Dictionary<string, TxResultMessage> _tx = new Dictionary<string, TxResultMessage>();
        int _notices;

        IEnumerator RunClient(ushort port, string scenario)
        {
            Auth.ClientAccountId = Arg("-mfAccount");
            Auth.ClientToken = Arg("-mfToken");
            Auth.ClientResult += r => _auth = r;
            Network.ClientManager.RegisterBroadcast<WalletSnapshot>((s, _) => _wallet = s);
            Network.ClientManager.RegisterBroadcast<TxResultMessage>((t, _) => _tx[t.RequestId] = t);
            Network.ClientManager.RegisterBroadcast<RewardNotice>((n, _) => _notices++);
            Network.ClientManager.StartConnection(Arg("-mfAddress", "127.0.0.1"), port);

            while (_auth == null) yield return null;
            Log("auth", $"{_auth.Value.Passed}:{_auth.Value.Reason}");
            if (scenario == "expect_auth_fail") { Finish(!_auth.Value.Passed, "auth must fail: " + _auth.Value.Reason); yield break; }
            if (!_auth.Value.Passed) { Finish(false, "auth failed"); yield break; }

            Network.ClientManager.Broadcast(new WalletRequest());
            while (_wallet == null) yield return null;
            Log("wallet_initial", $"{_wallet.Value.AccountId}:{_wallet.Value.Balance}");

            if (scenario == "attacker")
            {
                Network.ClientManager.Broadcast(new RewardClaim { RequestId = "mint", Amount = 999999, Reason = (int)RewardReason.Kill });
                Network.ClientManager.Broadcast(new PurchaseRequest { RequestId = "forge", ItemId = "rifle_service", ClaimedPrice = 0, ActingAccountId = Arg("-mfVictim") });
                Network.ClientManager.Broadcast(new PurchaseRequest { RequestId = "cheap", ItemId = "rifle_service", ClaimedPrice = -5000 });
                while (_tx.Count < 3) yield return null;
                foreach (var t in _tx.Values) Log("tx_" + t.RequestId, t.Status);
                _wallet = null;
                Network.ClientManager.Broadcast(new WalletRequest());
                while (_wallet == null) yield return null;
                Log("wallet_after", _wallet.Value.Balance);
                bool pass = _tx["mint"].Status == "Rejected:ClientRewardNotAllowed"
                            && _tx["forge"].Status == "Rejected:ForgedIdentity"
                            && _tx["cheap"].Status.StartsWith("Rejected")
                            && _wallet.Value.Balance == int.Parse(Arg("-mfExpectBalance", "10000"));
                Finish(pass, "attacker requests all rejected and balance unchanged");
                yield break;
            }

            if (scenario == "rewarded")
            {
                int expect = int.Parse(Arg("-mfExpectBalance", "10150"));
                while (_wallet.Value.Balance != expect) yield return null;
                Log("wallet_after", _wallet.Value.Balance);
                Log("reward_notices", _notices);
                yield return new WaitForSeconds(float.Parse(Arg("-mfHold", "0"))); // keep session open for duplicate-session test
                Finish(_notices >= 1, "server-side reward reached only this wallet");
                yield break;
            }

            if (scenario == "duplicate_session")
            {
                Finish(false, "second session for same account must not pass authentication");
                yield break;
            }

            Finish(true, "basic");
        }
    }
}
