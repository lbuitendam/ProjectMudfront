using System;
using System.Collections.Generic;

namespace Mudfront.Economy
{
    public enum LedgerKind { Entry, Reward, Purchase, Refund }

    public enum TxStatus { Ok, Duplicate, InsufficientFunds, InvalidAmount, UnknownPlayer, Capped, NoActiveMatch }

    /// <summary>Per-match action counters. They are separate from money: caps limit payout, not counts.</summary>
    public enum ActionStat
    {
        Kills, Assists, Revives, HpHealed, PassengersDelivered, HpRepaired, ObjectiveSeconds,
        Constructions, RoundsSupplied, SpotAssists, VehiclesDestroyed, BuildablesDestroyed,
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

    public class LedgerRecord
    {
        public long TxId;
        public string EventId;
        public PlayerId Player;
        public LedgerKind Kind;
        public int Code;          // RewardReason for rewards, item/vehicle-neutral code otherwise
        public int Amount;        // signed: + credit, - debit
        public int BalanceAfter;
        public string Detail;
    }

    public class WalletState
    {
        public int Balance;
        public long Earned;       // rewards + entry + refunds
        public long Spent;        // purchases
        public readonly Dictionary<RewardReason, long> EarnedByReason = new Dictionary<RewardReason, long>();
        public readonly Dictionary<ActionStat, long> Stats = new Dictionary<ActionStat, long>();
        public bool EntryGranted;
    }

    /// <summary>Authoritative match-only wallets. All mutations take a caller-supplied unique
    /// event ID; replays return the original result and never change money twice. Integer credits,
    /// checked arithmetic, ceiling at <see cref="EconomyConfig.MaxBalance"/>.</summary>
    public class WalletLedger
    {
        readonly EconomyConfig _cfg;
        readonly PlayerRegistry _players;
        readonly Dictionary<PlayerId, WalletState> _wallets = new Dictionary<PlayerId, WalletState>();
        readonly Dictionary<string, TxResult> _seen = new Dictionary<string, TxResult>();
        readonly HashSet<string> _seenStats = new HashSet<string>();
        readonly List<LedgerRecord> _records = new List<LedgerRecord>();
        long _nextTx = 1;
        int _resetMatch;

        public bool RoundActive { get; set; } = true;
        public IReadOnlyList<LedgerRecord> Records => _records;
        public event Action<PlayerId, LedgerRecord> Changed;

        public WalletLedger(EconomyConfig cfg, PlayerRegistry players)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _resetMatch = players.MatchId;
            players.MatchStarted += ResetForMatch;
        }

        public int Balance(PlayerId p) => _wallets.TryGetValue(p, out var w) ? w.Balance : 0;
        public WalletState Get(PlayerId p) => _wallets.TryGetValue(p, out var w) ? w : null;

        WalletState Wallet(PlayerId p)
        {
            if (!_wallets.TryGetValue(p, out var w)) _wallets[p] = w = new WalletState();
            return w;
        }

        /// <summary>Grants the match entry funds once per player per match (also for late joiners).</summary>
        public TxResult GrantEntry(PlayerId p)
        {
            if (!_players.Contains(p)) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            var w = Wallet(p);
            string key = $"entry:{_players.MatchId}:{p.Value}";
            if (w.EntryGranted) return _seen.TryGetValue(key, out var prev) ? Dup(prev) : new TxResult(TxStatus.Duplicate, 0, 0, w.Balance);
            w.EntryGranted = true;
            return Apply(p, w, key, LedgerKind.Entry, 0, _cfg.MatchEntryGrant, "match entry");
        }

        /// <summary>Credits a reward. Pays nothing outside an active round.</summary>
        public TxResult Reward(PlayerId p, RewardReason reason, int amount, string eventId, string detail = null)
        {
            if (!RoundActive) return new TxResult(TxStatus.NoActiveMatch, 0, 0, Balance(p));
            if (reason == RewardReason.MatchEntry) return new TxResult(TxStatus.InvalidAmount, 0, 0, Balance(p));
            return Credit(p, LedgerKind.Reward, (int)reason, amount, eventId, detail);
        }

        /// <summary>Refund of a failed purchase; must reference the original purchase event.</summary>
        public TxResult Refund(PlayerId p, int amount, string purchaseEventId, string detail = null)
        {
            string key = PurchaseKey(p, purchaseEventId);
            if (!_seen.TryGetValue(key, out var buy) || !buy.Ok || amount > -FindAmount(buy.TxId))
                return new TxResult(TxStatus.InvalidAmount, 0, 0, Balance(p));
            return Credit(p, LedgerKind.Refund, 0, amount, "refund:" + key, detail);
        }

        // Purchase request IDs come from clients, so they are namespaced per player and per match:
        // one player can never collide with or pre-empt another's request.
        string PurchaseKey(PlayerId p, string requestId) => $"buy:{_players.MatchId}:{p.Value}:{requestId}";

        int FindAmount(long txId)
        {
            for (int i = _records.Count - 1; i >= 0; i--) if (_records[i].TxId == txId) return _records[i].Amount;
            return 0;
        }

        TxResult Credit(PlayerId p, LedgerKind kind, int code, int amount, string eventId, string detail)
        {
            if (!_players.Contains(p)) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            if (string.IsNullOrEmpty(eventId) || amount <= 0) return new TxResult(TxStatus.InvalidAmount, 0, 0, Balance(p));
            if (_seen.TryGetValue(eventId, out var prev)) return Dup(prev);
            return Apply(p, Wallet(p), eventId, kind, code, amount, detail);
        }

        /// <summary>Debits for a purchase. Fails with no change when funds are insufficient.</summary>
        public TxResult Debit(PlayerId p, int amount, string eventId, string detail = null)
        {
            if (!_players.Contains(p)) return new TxResult(TxStatus.UnknownPlayer, 0, 0, 0);
            if (string.IsNullOrEmpty(eventId) || amount < 0) return new TxResult(TxStatus.InvalidAmount, 0, 0, Balance(p));
            eventId = PurchaseKey(p, eventId);
            if (_seen.TryGetValue(eventId, out var prev)) return Dup(prev);
            var w = Wallet(p);
            if (amount > w.Balance)
            {
                var fail = new TxResult(TxStatus.InsufficientFunds, 0, 0, w.Balance);
                return fail; // not recorded: a later retry with more money may succeed
            }
            return Apply(p, w, eventId, LedgerKind.Purchase, 0, -amount, detail);
        }

        TxResult Apply(PlayerId p, WalletState w, string eventId, LedgerKind kind, int code, int delta, string detail)
        {
            var status = TxStatus.Ok;
            if (delta > 0)
            {
                long room = (long)_cfg.MaxBalance - w.Balance;
                if (room <= 0) { var c = new TxResult(TxStatus.Capped, 0, 0, w.Balance); _seen[eventId] = c; return c; }
                if (delta > room) { delta = (int)room; status = TxStatus.Capped; }
            }
            checked
            {
                w.Balance += delta;
                if (delta > 0) w.Earned += delta; else w.Spent += -delta;
            }
            if (kind == LedgerKind.Reward)
            {
                var r = (RewardReason)code;
                w.EarnedByReason.TryGetValue(r, out var e);
                w.EarnedByReason[r] = e + delta;
            }
            var rec = new LedgerRecord { TxId = _nextTx++, EventId = eventId, Player = p, Kind = kind, Code = code, Amount = delta, BalanceAfter = w.Balance, Detail = detail };
            _records.Add(rec);
            var res = new TxResult(status, rec.TxId, delta, w.Balance);
            _seen[eventId] = res;
            Changed?.Invoke(p, rec);
            return res;
        }

        static TxResult Dup(TxResult prev) => new TxResult(TxStatus.Duplicate, prev.TxId, prev.Amount, prev.Balance);

        /// <summary>Counts a valid action once per event ID. Independent of whether it paid.</summary>
        public bool RecordStat(PlayerId p, ActionStat stat, long count, string eventId)
        {
            if (!_players.Contains(p) || count <= 0 || string.IsNullOrEmpty(eventId)) return false;
            if (!_seenStats.Add("stat:" + eventId)) return false;
            var w = Wallet(p);
            w.Stats.TryGetValue(stat, out var v);
            w.Stats[stat] = checked(v + count);
            return true;
        }

        public long Stat(PlayerId p, ActionStat s) => _wallets.TryGetValue(p, out var w) && w.Stats.TryGetValue(s, out var v) ? v : 0;

        /// <summary>True when every wallet equals the sum of its ledger records and earned - spent.</summary>
        public bool Reconciles(out string problem)
        {
            var sums = new Dictionary<PlayerId, long>();
            foreach (var r in _records) { sums.TryGetValue(r.Player, out var s); sums[r.Player] = s + r.Amount; }
            foreach (var kv in _wallets)
            {
                sums.TryGetValue(kv.Key, out var s);
                if (s != kv.Value.Balance || kv.Value.Earned - kv.Value.Spent != kv.Value.Balance)
                { problem = $"{kv.Key}: balance {kv.Value.Balance}, ledger {s}, earned-spent {kv.Value.Earned - kv.Value.Spent}"; return false; }
            }
            problem = null;
            return true;
        }

        /// <summary>Clears all match-only money, records and replay protection exactly once per match.</summary>
        void ResetForMatch(int matchId)
        {
            if (matchId == _resetMatch) return;
            _resetMatch = matchId;
            _wallets.Clear();
            _seen.Clear();
            _seenStats.Clear();
            _records.Clear();
        }
    }
}
