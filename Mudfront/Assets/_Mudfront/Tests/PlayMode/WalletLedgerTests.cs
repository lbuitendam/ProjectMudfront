using Mudfront.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Mudfront.Tests
{
    /// <summary>Revision 2 ledger contract: persistent account money, one-time 10,000 seed, no match reset.</summary>
    public class WalletLedgerTests
    {
        EconomyConfig _cfg;
        PlayerRegistry _reg;
        WalletLedger _ledger;
        int M => _reg.MatchId;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<EconomyConfig>();
            _cfg.AccountSeedGrant = 10000;
            _cfg.MaxBalance = 50000;
            _reg = new PlayerRegistry();
            _ledger = new WalletLedger(_cfg, _reg);
        }

        PlayerId Join(string account)
        {
            Assert.AreEqual(JoinStatus.Ok, _reg.Join(account, out var p));
            _ledger.GrantSeed(p);
            return p;
        }

        void AssertReconciles() { Assert.IsTrue(_ledger.Reconciles(out var why), why); }

        [Test] public void Seed_OnlyOncePerAccount()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.Duplicate, _ledger.GrantSeed(p).Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void NewMatch_PreservesBalance_AndNeverReseeds()
        {
            var p = Join("acc-a");
            _ledger.Reward(p, RewardReason.Kill, 150, M, "kill:life7");
            _ledger.Debit(p, 250, M, "req1");
            _ledger.RecordStat(p, ActionStat.Kills, 1, "kill:life7");
            _reg.BeginNewMatch();
            Assert.AreEqual(9900, _ledger.Balance(p), "superseded: match entry no longer resets the wallet");
            Assert.AreEqual(TxStatus.Duplicate, _ledger.GrantSeed(p).Status);
            Assert.AreEqual(9900, _ledger.Balance(p));
            Assert.AreEqual(0, _ledger.Stat(p, ActionStat.Kills), "match statistics reset");
            AssertReconciles();
        }

        [Test] public void OldMatchActions_AreRejected()
        {
            var p = Join("acc-a");
            int old = M;
            _reg.BeginNewMatch();
            Assert.AreEqual(TxStatus.StaleMatch, _ledger.Reward(p, RewardReason.Kill, 150, old, "kill:life1").Status);
            Assert.AreEqual(TxStatus.StaleMatch, _ledger.Debit(p, 100, old, "r1").Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
        }

        [Test] public void DuplicateReward_PaysOnce()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.Ok, _ledger.Reward(p, RewardReason.Kill, 150, M, "kill:life7").Status);
            Assert.AreEqual(TxStatus.Duplicate, _ledger.Reward(p, RewardReason.Kill, 150, M, "kill:life7").Status);
            Assert.AreEqual(10150, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void DuplicateDebit_ChargesOnce()
        {
            var p = Join("acc-a");
            var a = _ledger.Debit(p, 250, M, "req1");
            var b = _ledger.Debit(p, 250, M, "req1");
            Assert.AreEqual(TxStatus.Ok, a.Status);
            Assert.AreEqual(TxStatus.Duplicate, b.Status);
            Assert.AreEqual(a.TxId, b.TxId);
            Assert.AreEqual(9750, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void InsufficientFunds_NoChange()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.InsufficientFunds, _ledger.Debit(p, 10001, M, "big").Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
            Assert.AreEqual(0, _ledger.Get(p).Spent);
        }

        [Test] public void NegativeZeroAndForgedSeed_Rejected()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Reward(p, RewardReason.Kill, -50, M, "e1").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Reward(p, RewardReason.Kill, 0, M, "e2").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Debit(p, -100, M, "e3").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Reward(p, RewardReason.AccountSeed, 10000, M, "e4").Status);
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Debit(p, 10, M, "e5", kind: LedgerKind.Refund).Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
        }

        [Test] public void Overflow_ClampsAtMaxBalance()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.Capped, _ledger.Reward(p, RewardReason.Kill, int.MaxValue, M, "huge").Status);
            Assert.AreEqual(50000, _ledger.Balance(p));
            Assert.AreEqual(TxStatus.Capped, _ledger.Reward(p, RewardReason.Kill, 10, M, "more").Status);
            Assert.AreEqual(50000, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void SameTeamPlayers_HaveSeparateWallets()
        {
            var a = Join("acc-a"); var b = Join("acc-b");
            Assert.AreNotEqual(a, b);
            _ledger.Reward(a, RewardReason.Kill, 150, M, "k1");
            _ledger.Debit(b, 100, M, "req1");
            _ledger.Debit(a, 100, M, "req1"); // same client request ID from another account is independent
            Assert.AreEqual(10050, _ledger.Balance(a));
            Assert.AreEqual(9900, _ledger.Balance(b));
            AssertReconciles();
        }

        [Test] public void Refund_OnlyForRealPurchase_AndOnce()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Refund(p, 100, M, "never-bought").Status);
            _ledger.Debit(p, 250, M, "buy1");
            Assert.AreEqual(TxStatus.InvalidAmount, _ledger.Refund(p, 300, M, "buy1").Status);
            Assert.AreEqual(TxStatus.Ok, _ledger.Refund(p, 250, M, "buy1").Status);
            Assert.AreEqual(TxStatus.Duplicate, _ledger.Refund(p, 250, M, "buy1").Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
            AssertReconciles();
        }

        [Test] public void UnlockFee_IsASpend()
        {
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.Ok, _ledger.Debit(p, 1000, M, "unlock:medkit_large", kind: LedgerKind.Unlock).Status);
            Assert.AreEqual(9000, _ledger.Balance(p));
            Assert.AreEqual(1000, _ledger.Get(p).Spent);
        }

        [Test] public void Stats_CountedOnce_IndependentOfPayout()
        {
            var p = Join("acc-a");
            Assert.IsTrue(_ledger.RecordStat(p, ActionStat.Kills, 1, "kill:life3"));
            Assert.IsFalse(_ledger.RecordStat(p, ActionStat.Kills, 1, "kill:life3"));
            Assert.AreEqual(1, _ledger.Stat(p, ActionStat.Kills));
            Assert.AreEqual(10000, _ledger.Balance(p));
        }

        [Test] public void NoRewardsOutsideRound()
        {
            var p = Join("acc-a");
            _ledger.RoundActive = false;
            Assert.AreEqual(TxStatus.NoActiveMatch, _ledger.Reward(p, RewardReason.Kill, 150, M, "k").Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
        }

        [Test] public void Reconnect_SameAccount_KeepsIdentityAndBalance()
        {
            var p = Join("acc-a");
            _ledger.Debit(p, 300, M, "r1");
            _reg.MarkDisconnected(p);
            Assert.AreEqual(JoinStatus.Reconnected, _reg.Join("acc-a", out var back));
            Assert.AreEqual(p, back);
            Assert.AreEqual(9700, _ledger.Balance(back));
            Assert.AreEqual(TxStatus.Duplicate, _ledger.GrantSeed(back).Status);
        }

        [Test] public void SecondSimultaneousSession_Refused_AndBlankAccountRejected()
        {
            Join("acc-a");
            Assert.AreEqual(JoinStatus.AlreadyConnected, _reg.Join("acc-a", out _));
            Assert.AreEqual(JoinStatus.InvalidAccount, _reg.Join("  ", out _));
            Assert.AreEqual(JoinStatus.InvalidAccount, _reg.Join(null, out _));
        }

        [Test] public void LifeIds_AreUniqueAndMonotonic()
        {
            var a = Join("acc-a"); var b = Join("acc-b");
            int l1 = _reg.StartLife(a), l2 = _reg.StartLife(b), l3 = _reg.StartLife(a);
            Assert.That(l1 < l2 && l2 < l3);
            Assert.AreEqual(l3, _reg.CurrentLife(a));
        }
    }
}
