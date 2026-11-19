using System;
using System.Collections.Generic;

namespace Mudfront.Economy
{
    public enum LedgerKind { Seed, Reward, Purchase, Refund, Unlock }

    public enum TxStatus { Ok, Duplicate, InsufficientFunds, InvalidAmount, UnknownPlayer, Capped, NoActiveMatch, StaleMatch, StorageFailure }

    /// <summary>Per-match action counters. Separate from money: caps limit payout, not counts.</summary>
    public enum ActionStat
    {
        Kills, Assists, Revives, HpHealed, PassengersDelivered, HpRepaired, ObjectiveSeconds,
        Constructions, RoundsSupplied, SpotAssists, VehiclesDestroyed, BuildablesDestroyed, FollowUpKills,
    }

    public readonly struct TxResult
    {
        public readonly TxStatus Status;
        public readonly long TxId;
        public readonly int Amount;   // actually applied (may be lower than requested when capped)
        public readonly int Balance;
        public TxResult(TxStatus s, long id, int amount, int balance) { Status = s; TxId = id; Amount = amount; Balance = balance; }
        public bool Ok => Status == TxStatus.Ok || Status == TxStatus.Capped;
        public override string ToString() => $"{Status} tx{TxId} {Amount} -> {Balance}";
    }

    [Serializable]
    public class LedgerRecord
    {
        public long TxId;
        public string Key;        // unique idempotency key (durable)
        public string AccountId;
        public LedgerKind Kind;
        public int Code;          // RewardReason for rewards
        public int Amount;        // signed: + credit, - debit
        public int BalanceAfter;
        public int MatchId;
        public string Detail;
    }

    /// <summary>Persistent per-account money (revision 2). Counts are per match.</summary>
    public class AccountWallet
    {
        public string AccountId;
        public int Balance;
        public long Earned;       // seed + rewards + refunds
        public long Spent;        // purchases + unlocks
        public bool Seeded;
        public readonly Dictionary<RewardReason, long> EarnedByReason = new Dictionary<RewardReason, long>();
        public readonly Dictionary<ActionStat, long> MatchStats = new Dictionary<ActionStat, long>();
    }

    /// <summary>Durable store behind the ledger (ECON-03A). <see cref="Commit"/> must persist the
    /// record before returning true; the ledger only acknowledges after that.</summary>
    public interface ILedgerStore
    {
        bool Commit(LedgerRecord record);
    }

    /// <summary>Authoritative persistent account wallets. Every mutation takes a unique key; replays
    /// return the original result and never change money twice. Integer credits, checked arithmetic,
    /// ceiling at <see cref="EconomyConfig.MaxBalance"/>. Rewards and purchases must carry the
    /// current MatchId so actions from an old match stay invalid although the wallet persists.</summary>
    public class WalletLedger
    {
        readonly EconomyConfig _cfg;
        readonly PlayerRegistry _players;
        readonly ILedgerStore _store;
        readonly Dictionary<string, AccountWallet> _wallets = new Dictionary<string, AccountWallet>();
        readonly Dictionary<string, TxResult> _seen = new Dictionary<string, TxResult>();
        readonly Dictionary<string, LedgerRecord> _byKey = new Dictionary<string, LedgerRecord>();
        readonly HashSet<string> _seenStats = new HashSet<string>();
        readonly List<LedgerRecord> _records = new List<LedgerRecord>();
        long _nextTx = 1;

        public bool RoundActive { get; set; } = true;
        public IReadOnlyList<LedgerRecord> Records => _records;
        public event Action<string, LedgerRecord> Changed;

        public WalletLedger(EconomyConfig cfg, PlayerRegistry players, ILedgerStore store = null)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _store = store;
            players.MatchStarted += _ => { foreach (var w in _wallets.Values) w.MatchStats.Clear(); _seenStats.Clear(); };
        }

        public int MatchId => _players.MatchId;
        public int Balance(PlayerId p) => Get(p)?.Balance ?? 0;
        public AccountWallet Get(PlayerId p) { var a = _players.AccountOf(p); return a != null && _wallets.TryGetValue(a, out var w) ? w : null; }
        public AccountWallet GetAccount(string accountId) => accountId != null && _wallets.TryGetValue(accountId, out var w) ? w : null;

        AccountWallet Wallet(string account)
        {
            if (!_wallets.TryGetValue(account, out var w)) _wallets[account] = w = new AccountWallet { AccountId = account };
            return w;
        }

        /// <summary>Loads committed history (ECON-03A recovery). Rebuilds balances and idempotency.</summary>
        public void Restore(IEnumerable<LedgerRecord> committed)
        {
            foreach (var r in committed)
            {
                if (_byKey.ContainsKey(r.Key)) continue;
                var w = Wallet(r.AccountId);
                ApplyToWallet(w, r);
                r.BalanceAfter = w.Balance;
                Remember(r, r.Amount > 0 && r.Kind != LedgerKind.Purchase ? TxStatus.Ok : TxStatus.Ok);
                if (r.TxId >= _nextTx) _nextTx = r.TxId + 1;
            }
        }

        /// <summary>Credits the one-time account seed. Never repeats for the same account.</summary>
        public TxResult GrantSeed(PlayerId p)
        {
            var acct = _players.AccountOf(p);
            if (acct == null) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            return Credit(acct, LedgerKind.Seed, (int)RewardReason.AccountSeed, _cfg.AccountSeedGrant, "seed:" + acct, "account seed", allowZero: true);
        }

        /// <summary>Credits a reward for an action in match <paramref name="matchId"/>. Pays nothing
        /// outside an active round or for an old match.</summary>
        public TxResult Reward(PlayerId p, RewardReason reason, int amount, int matchId, string eventId, string detail = null)
        {
            var acct = _players.AccountOf(p);
            if (acct == null) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            if (matchId != MatchId) return new TxResult(TxStatus.StaleMatch, 0, 0, Balance(p));
            if (!RoundActive) return new TxResult(TxStatus.NoActiveMatch, 0, 0, Balance(p));
            if (reason == RewardReason.AccountSeed || string.IsNullOrEmpty(eventId)) return new TxResult(TxStatus.InvalidAmount, 0, 0, Balance(p));
            return Credit(acct, LedgerKind.Reward, (int)reason, amount, $"rw:{matchId}:{eventId}", detail);
        }

        /// <summary>Debits for a purchase or unlock. Fails with no change when funds are insufficient.
        /// Client request IDs are namespaced per account and match.</summary>
        public TxResult Debit(PlayerId p, int amount, int matchId, string requestId, string detail = null, LedgerKind kind = LedgerKind.Purchase)
        {
            var acct = _players.AccountOf(p);
            if (acct == null) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            if (matchId != MatchId) return new TxResult(TxStatus.StaleMatch, 0, 0, Balance(p));
            if (string.IsNullOrEmpty(requestId) || amount < 0 || (kind != LedgerKind.Purchase && kind != LedgerKind.Unlock))
                return new TxResult(TxStatus.InvalidAmount, 0, 0, Balance(p));
            string key = PurchaseKey(acct, matchId, requestId);
            if (_seen.TryGetValue(key, out var prev)) return Dup(prev);
            var w = Wallet(acct);
            if (amount > w.Balance) return new TxResult(TxStatus.InsufficientFunds, 0, 0, w.Balance); // not remembered: may retry
            return Apply(w, key, kind, 0, -amount, matchId, detail);
        }

        /// <summary>Refund of a failed purchase; must reference a real successful purchase, at most its amount, once.</summary>
        public TxResult Refund(PlayerId p, int amount, int matchId, string requestId, string detail = null)
        {
            var acct = _players.AccountOf(p);
            if (acct == null) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            return RefundByKey(acct, PurchaseKey(acct, matchId, requestId), amount, detail);
        }

        /// <summary>Recovery path: refund an outstanding purchase by its durable key (no live session needed).</summary>
        public TxResult RefundByKey(string account, string purchaseKey, int amount, string detail = null)
        {
            if (!_byKey.TryGetValue(purchaseKey, out var buy) || buy.AccountId != account || buy.Kind == LedgerKind.Refund || amount > -buy.Amount)
                return new TxResult(TxStatus.InvalidAmount, 0, 0, GetAccount(account)?.Balance ?? 0);
            return Credit(account, LedgerKind.Refund, 0, amount, "refund:" + purchaseKey, detail, matchId: buy.MatchId);
        }

        public static string PurchaseKey(string account, int matchId, string requestId) => $"buy:{account}:{matchId}:{requestId}";

        TxResult Credit(string acct, LedgerKind kind, int code, int amount, string key, string detail, bool allowZero = false, int matchId = -1)
        {
            int bal = GetAccount(acct)?.Balance ?? 0;
            if (amount < 0 || (amount == 0 && !allowZero)) return new TxResult(TxStatus.InvalidAmount, 0, 0, bal);
            if (_seen.TryGetValue(key, out var prev)) return Dup(prev);
            return Apply(Wallet(acct), key, kind, code, amount, matchId < 0 ? MatchId : matchId, detail);
        }

        TxResult Apply(AccountWallet w, string key, LedgerKind kind, int code, int delta, int matchId, string detail)
        {
            var status = TxStatus.Ok;
            if (delta > 0)
            {
                long room = (long)_cfg.MaxBalance - w.Balance;
                if (room <= 0) { delta = 0; status = TxStatus.Capped; }
                else if (delta > room) { delta = (int)room; status = TxStatus.Capped; }
                if (delta == 0 && kind != LedgerKind.Seed) { var c = new TxResult(TxStatus.Capped, 0, 0, w.Balance); _seen[key] = c; return c; }
            }
            var rec = new LedgerRecord { TxId = _nextTx, Key = key, AccountId = w.AccountId, Kind = kind, Code = code, Amount = delta, MatchId = matchId, Detail = detail };
            checked { rec.BalanceAfter = w.Balance + delta; }
            // Durable before acknowledged: a failed write changes nothing and is not remembered.
            if (_store != null && !_store.Commit(rec)) return new TxResult(TxStatus.StorageFailure, 0, 0, w.Balance);
            _nextTx++;
            ApplyToWallet(w, rec);
            var res = Remember(rec, status);
            Changed?.Invoke(w.AccountId, rec);
            return res;
        }

        static void ApplyToWallet(AccountWallet w, LedgerRecord r)
        {
            checked
            {
                w.Balance += r.Amount;
                if (r.Amount > 0) w.Earned += r.Amount; else w.Spent += -r.Amount;
            }
            if (r.Kind == LedgerKind.Seed) w.Seeded = true;
            if (r.Kind == LedgerKind.Reward)
            {
                var reason = (RewardReason)r.Code;
                w.EarnedByReason.TryGetValue(reason, out var e);
                w.EarnedByReason[reason] = e + r.Amount;
            }
        }

        TxResult Remember(LedgerRecord r, TxStatus status)
        {
            _records.Add(r);
            _byKey[r.Key] = r;
            var res = new TxResult(status, r.TxId, r.Amount, r.BalanceAfter);
            _seen[r.Key] = res;
            return res;
        }

        static TxResult Dup(TxResult prev) => new TxResult(TxStatus.Duplicate, prev.TxId, prev.Amount, prev.Balance);

        public bool HasRecord(string key) => _byKey.ContainsKey(key);

        /// <summary>Counts a valid action once per event ID in the current match. Independent of payout.</summary>
        public bool RecordStat(PlayerId p, ActionStat stat, long count, string eventId)
        {
            var acct = _players.AccountOf(p);
            if (acct == null || count <= 0 || string.IsNullOrEmpty(eventId)) return false;
            if (!_seenStats.Add($"stat:{MatchId}:{eventId}")) return false;
            var w = Wallet(acct);
            w.MatchStats.TryGetValue(stat, out var v);
            w.MatchStats[stat] = checked(v + count);
            return true;
        }

        public long Stat(PlayerId p, ActionStat s) { var w = Get(p); return w != null && w.MatchStats.TryGetValue(s, out var v) ? v : 0; }

        /// <summary>True when every wallet equals the sum of its ledger records and earned - spent.</summary>
        public bool Reconciles(out string problem)
        {
            var sums = new Dictionary<string, long>();
            foreach (var r in _records) { sums.TryGetValue(r.AccountId, out var s); sums[r.AccountId] = s + r.Amount; }
            foreach (var kv in _wallets)
            {
                sums.TryGetValue(kv.Key, out var s);
                if (s != kv.Value.Balance || kv.Value.Earned - kv.Value.Spent != kv.Value.Balance)
                { problem = $"{kv.Key}: balance {kv.Value.Balance}, ledger {s}, earned-spent {kv.Value.Earned - kv.Value.Spent}"; return false; }
            }
            problem = null;
            return true;
        }
    }
}
