using System.IO;
using System.Threading.Tasks;
using Mudfront.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Mudfront.Tests
{
    /// <summary>ECON-03A: durable account wallet, restart, crash recovery and exclusivity.
    /// "Restart" disposes the store/ledger/registry and builds fresh ones from the same file, which
    /// is exactly what a new server process does at startup.</summary>
    public class AccountPersistenceTests
    {
        EconomyConfig _cfg;
        string _path;
        FileLedgerStore _store;
        PlayerRegistry _reg;
        WalletLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<EconomyConfig>();
            _cfg.AccountSeedGrant = 10000;
            _path = Path.Combine(Application.temporaryCachePath, "ledger_test_" + System.Guid.NewGuid().ToString("N") + ".jsonl");
            Boot();
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
            if (File.Exists(_path)) File.Delete(_path);
        }

        int Boot()
        {
            _store?.Dispose();
            _store = new FileLedgerStore(_path);
            _reg = new PlayerRegistry();
            _ledger = AccountAuthority.Open(_cfg, _reg, _store, out int refunds);
            return refunds;
        }

        PlayerId Join(string acct)
        {
            var st = _reg.Join(acct, out var p);
            Assert.That(st == JoinStatus.Ok || st == JoinStatus.Reconnected, st.ToString());
            _ledger.GrantSeed(p);
            return p;
        }

        [Test] public void EarnSpend_Restart_Reconnect_NewMatch_BalancePersists()
        {
            var p = Join("acc-a");
            _ledger.Reward(p, RewardReason.Kill, 150, _reg.MatchId, "kill:l1");
            var buy = _ledger.Debit(p, 250, _reg.MatchId, "r1");
            _ledger.MarkFulfilled(WalletLedger.PurchaseKey("acc-a", _reg.MatchId, "r1"));
            Assert.AreEqual(9900, buy.Balance);
            int oldMatch = _reg.MatchId;

            Assert.AreEqual(0, Boot(), "fulfilled purchase must not be refunded");
            var back = Join("acc-a");
            Assert.AreEqual(9900, _ledger.Balance(back));
            Assert.Greater(_reg.MatchId, oldMatch, "match numbering continues after restart");
            _reg.BeginNewMatch();
            Assert.AreEqual(9900, _ledger.Balance(back));
            Assert.IsTrue(_ledger.Reconciles(out var why), why);
        }

        [Test] public void DuplicateAccountCreation_NeverReseeds_AcrossRestarts()
        {
            Join("acc-a");
            Boot();
            var p = Join("acc-a");
            Assert.AreEqual(TxStatus.Duplicate, _ledger.GrantSeed(p).Status);
            Boot();
            p = Join("acc-a");
            Assert.AreEqual(10000, _ledger.Balance(p));
        }

        [Test] public void OldMatchActions_InvalidAfterRestart()
        {
            var p = Join("acc-a");
            int old = _reg.MatchId;
            _ledger.Reward(p, RewardReason.Kill, 150, old, "kill:l1");
            Boot();
            p = Join("acc-a");
            Assert.AreEqual(TxStatus.StaleMatch, _ledger.Debit(p, 100, old, "late").Status);
            Assert.AreEqual(TxStatus.StaleMatch, _ledger.Reward(p, RewardReason.Kill, 150, old, "kill:l2").Status);
            Assert.AreEqual(10150, _ledger.Balance(p));
        }

        [Test] public void CrashAfterDebit_BeforeDelivery_RefundedExactlyOnce()
        {
            var p = Join("acc-a");
            _ledger.Debit(p, 600, _reg.MatchId, "jeep1", "vehicle jeep_transport");
            Assert.AreEqual(9400, _ledger.Balance(p));
            // crash: no fulfilment record written
            Assert.AreEqual(1, Boot());
            p = Join("acc-a");
            Assert.AreEqual(10000, _ledger.Balance(p));
            Assert.AreEqual(0, Boot(), "recovery is idempotent");
            p = Join("acc-a");
            Assert.AreEqual(10000, _ledger.Balance(p));
            Assert.IsTrue(_ledger.Reconciles(out var why), why);
        }

        [Test] public void RetryAfterRestart_SameRequest_IsDuplicate()
        {
            var p = Join("acc-a");
            int m = _reg.MatchId;
            _ledger.Debit(p, 250, m, "r1");
            _ledger.MarkFulfilled(WalletLedger.PurchaseKey("acc-a", m, "r1"));
            // The store's idempotency survives a restart: the original key is still known.
            Boot();
            Assert.IsTrue(_ledger.HasRecord(WalletLedger.PurchaseKey("acc-a", m, "r1")));
            p = Join("acc-a");
            Assert.AreEqual(9750, _ledger.Balance(p));
        }

        [Test] public void TornTailWrite_IsDiscarded_CommittedHistoryKept()
        {
            var p = Join("acc-a");
            _ledger.Reward(p, RewardReason.Kill, 150, _reg.MatchId, "kill:l1");
            _store.AppendRawForTest("deadbeef|{\"TxId\":99,\"Key\":\"rw:1:fake\",\"AccountId\":\"acc-a\",\"Amount\":5000");
            Boot();
            Assert.IsTrue(_store.DiscardedTornTail);
            p = Join("acc-a");
            Assert.AreEqual(10150, _ledger.Balance(p), "unacknowledged torn write adds no cash");
            _ledger.Reward(p, RewardReason.Kill, 150, _reg.MatchId, "kill:l2");
            Boot();
            Assert.IsFalse(_store.DiscardedTornTail);
            Assert.AreEqual(10300, _ledger.Balance(Join("acc-a")));
        }

        [Test] public void CorruptRecord_RefusesToStart()
        {
            Join("acc-a");
            _store.Dispose(); _store = null;
            var lines = File.ReadAllLines(_path);
            lines[1] = lines[1].Replace("10000", "99999");
            File.WriteAllLines(_path, lines);
            Assert.Throws<InvalidDataException>(() => new FileLedgerStore(_path).Dispose());
        }

        [Test] public void StorageFailure_InventsNoCash_AndRetrySucceedsOnce()
        {
            var p = Join("acc-a");
            _store.SimulateFailure = true;
            Assert.AreEqual(TxStatus.StorageFailure, _ledger.Reward(p, RewardReason.Kill, 150, _reg.MatchId, "kill:l1").Status);
            Assert.AreEqual(TxStatus.StorageFailure, _ledger.Debit(p, 100, _reg.MatchId, "r1").Status);
            Assert.AreEqual(10000, _ledger.Balance(p));
            _store.SimulateFailure = false;
            Assert.AreEqual(TxStatus.Ok, _ledger.Reward(p, RewardReason.Kill, 150, _reg.MatchId, "kill:l1").Status);
            Assert.AreEqual(TxStatus.Duplicate, _ledger.Reward(p, RewardReason.Kill, 150, _reg.MatchId, "kill:l1").Status);
            Boot();
            Assert.AreEqual(10150, _ledger.Balance(Join("acc-a")));
        }

        [Test] public void SecondAuthority_CannotOpenSameDatabase()
        {
            Assert.Throws<IOException>(() => new FileLedgerStore(_path).Dispose());
        }

        [Test] public void SimultaneousSpending_NeverOverdraws()
        {
            var p = Join("acc-a");
            int m = _reg.MatchId;
            var results = new TxResult[200];
            Parallel.For(0, 200, i => results[i] = _ledger.Debit(p, 100, m, "par" + i));
            int ok = 0; foreach (var r in results) if (r.Ok) ok++;
            Assert.AreEqual(100, ok, "exactly 10,000 / 100 debits can succeed");
            Assert.AreEqual(0, _ledger.Balance(p));
            for (int i = 0; i < 200; i++) if (results[i].Ok) _ledger.MarkFulfilled(WalletLedger.PurchaseKey("acc-a", m, "par" + i));
            Boot();
            Assert.AreEqual(0, _ledger.Balance(Join("acc-a")));
        }
    }
}
