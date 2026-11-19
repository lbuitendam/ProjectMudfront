using Mudfront.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Mudfront.Tests
{
    public class WalletLedgerTests
    {
        EconomyConfig _cfg;
        PlayerRegistry _reg;
        WalletLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<EconomyConfig>();
            _cfg.MatchEntryGrant = 1000;
            _cfg.MaxBalance = 5000;
            int n = 0;
            _reg = new PlayerRegistry(() => "tok" + (++n));
            _ledger = new WalletLedger(_cfg, _reg);
        }

        PlayerId Join() { var p = _reg.Register(out _); _ledger.GrantEntry(p); return p; }

        void AssertReconciles() { Assert.IsTrue(_ledger.Reconciles(out var why), why); }

        [Test] public void EntryGrant_OnlyOncePerMatch()
        {
            var p = Join();
            Assert.AreEqual(TxStatus.Duplicate, _ledger.GrantEntry(p).Status);
            Assert.AreEqual(1000, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void DuplicateReward_PaysOnce()
        {
            var p = Join();
            Assert.AreEqual(TxStatus.Ok, _ledger.Reward(p, RewardReason.Kill, 150, "kill:life7").Status);
            Assert.AreEqual(TxStatus.Duplicate, _ledger.Reward(p, RewardReason.Kill, 150, "kill:life7").Status);
            Assert.AreEqual(1150, _ledger.Balance(p));
            Assert.AreEqual(150, _ledger.Get(p).EarnedByReason[RewardReason.Kill]);
            AssertReconciles();
        }

        [Test] public void DuplicateDebit_ChargesOnce()
        {
            var p = Join();
            var a = _ledger.Debit(p, 250, "req1");
            var b = _ledger.Debit(p, 250, "req1");
            Assert.AreEqual(TxStatus.Ok, a.Status);
            Assert.AreEqual(TxStatus.Duplicate, b.Status);
            Assert.AreEqual(a.TxId, b.TxId);
            Assert.AreEqual(750, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void InsufficientFunds_NoChange()
        {
            var p = Join();
            Assert.AreEqual(TxStatus.InsufficientFunds, _ledger.Debit(p, 1001, "big").Status);
            Assert.AreEqual(1000, _ledger.Balance(p));
            Assert.AreEqual(0, _ledger.Get(p).Spent);
        }

        [Test] public void NegativeOrZeroAmounts_Rejected()
        {
            var p = Join();
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Reward(p, RewardReason.Kill, -50, "e1").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Reward(p, RewardReason.Kill, 0, "e2").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Debit(p, -100, "e3").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Reward(p, RewardReason.MatchEntry, 1000, "e4").Status);
            Assert.AreEqual(1000, _ledger.Balance(p));
        }

        [Test] public void Overflow_ClampsAtMaxBalance()
        {
            var p = Join();
            var r = _ledger.Reward(p, RewardReason.Kill, int.MaxValue, "huge");
            Assert.AreEqual(TxStatus.Capped, r.Status);
            Assert.AreEqual(5000, _ledger.Balance(p));
            Assert.AreEqual(TxStatus.Capped, _ledger.Reward(p, RewardReason.Kill, 10, "more").Status);
            Assert.AreEqual(5000, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void SameTeamPlayers_HaveSeparateWallets()
        {
            var a = Join(); var b = Join();
            Assert.AreNotEqual(a, b);
            _ledger.Reward(a, RewardReason.Kill, 150, "k1");
            _ledger.Debit(b, 100, "req1");
            _ledger.Debit(a, 100, "req1"); // same client request ID from a different player is independent
            Assert.AreEqual(1050, _ledger.Balance(a));
            Assert.AreEqual(900, _ledger.Balance(b));
            AssertReconciles();
        }

        [Test] public void Refund_OnlyForRealPurchase_AndOnce()
        {
            var p = Join();
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Refund(p, 100, "never-bought").Status);
            _ledger.Debit(p, 250, "buy1");
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Refund(p, 300, "buy1").Status);
            Assert.AreEqual(TxStatus.Ok, _ledger.Refund(p, 250, "buy1").Status);
            Assert.AreEqual(TxStatus.Duplicate, _ledger.Refund(p, 250, "buy1").Status);
            Assert.AreEqual(1000, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void Stats_CountedOnce_IndependentOfPayout()
        {
            var p = Join();
            Assert.IsTrue(_ledger.RecordStat(p, ActionStat.Kills, 1, "kill:life3"));
            Assert.IsFalse(_ledger.RecordStat(p, ActionStat.Kills, 1, "kill:life3"));
            Assert.AreEqual(1, _ledger.Stat(p, ActionStat.Kills));
            Assert.AreEqual(1000, _ledger.Balance(p));
        }

        [Test] public void NoRewardsOutsideRound()
        {
            var p = Join();
            _ledger.RoundActive = false;
            Assert.AreEqual(TxStatus.NoActiveMatch, _ledger.Reward(p, RewardReason.Kill, 150, "k").Status);
            Assert.AreEqual(1000, _ledger.Balance(p));
        }

        [Test] public void LateJoin_GetsEntry_AndNewMatchResetsExactlyOnce()
        {
            var a = Join();
            _ledger.Reward(a, RewardReason.Kill, 150, "k1");
            var late = Join();
            Assert.AreEqual(1000, _ledger.Balance(late));

            _reg.BeginNewMatch();
            Assert.AreEqual(0, _ledger.Balance(a));
            Assert.AreEqual(TxStatus.Ok, _ledger.GrantEntry(a).Status);
            Assert.AreEqual(1000, _ledger.Balance(a));
            Assert.AreEqual(0, _ledger.Stat(a, ActionStat.Kills));
            AssertReconciles();
        }

        [Test] public void Reconnect_SameMatch_KeepsIdentityAndBalance()
        {
            var p = _reg.Register(out var tok);
            _ledger.GrantEntry(p);
            _ledger.Debit(p, 300, "r1");
            _reg.MarkDisconnected(p);
            Assert.IsTrue(_reg.TryReconnect(tok, out var back));
            Assert.AreEqual(p, back);
            Assert.AreEqual(700, _ledger.Balance(back));
            Assert.AreEqual(TxStatus.Duplicate, _ledger.GrantEntry(back).Status);
        }

        [Test] public void Reconnect_ForgedOrStaleToken_Rejected()
        {
            var p = _reg.Register(out var tok);
            Assert.IsFalse(_reg.TryReconnect("tok999", out _));
            Assert.IsFalse(_reg.TryReconnect(null, out _));
            _reg.MarkDisconnected(p);
            _reg.BeginNewMatch();
            Assert.IsFalse(_reg.TryReconnect(tok, out _), "previous-match token must not map");
            Assert.IsFalse(_reg.Contains(p));
        }

        [Test] public void LifeIds_AreUniqueAndMonotonic()
        {
            var a = _reg.Register(out _); var b = _reg.Register(out _);
            int l1 = _reg.StartLife(a), l2 = _reg.StartLife(b), l3 = _reg.StartLife(a);
            Assert.That(l1 < l2 && l2 < l3);
            Assert.AreEqual(l3, _reg.CurrentLife(a));
        }
    }
}
