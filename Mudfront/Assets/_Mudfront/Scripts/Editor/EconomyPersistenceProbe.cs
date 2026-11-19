using System.IO;
using Mudfront.Economy;
using UnityEditor;
using UnityEngine;

namespace Mudfront.EditorTools
{
    /// <summary>Real two-process restart check for the durable account ledger (ECON-03A).
    /// Phase 1 (process A): seed, earn, spend, leave a purchase undelivered, exit without cleanup.
    /// Phase 2 (process B, fresh Unity): open the same database, recover, verify, join a new match.
    /// Batch: -executeMethod Mudfront.EditorTools.EconomyPersistenceProbe.Phase1 / .Phase2
    /// Database: -probeDb &lt;path&gt; (defaults to Library/EconomyProbe/ledger.jsonl).</summary>
    public static class EconomyPersistenceProbe
    {
        static string DbPath()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-probeDb") return args[i + 1];
            return Path.GetFullPath("Library/EconomyProbe/ledger.jsonl");
        }

        static EconomyConfig Config() => AssetDatabase.LoadAssetAtPath<EconomyConfig>(EconomyCatalogBuilder.ConfigPath);

        public static void Phase1()
        {
            string db = DbPath();
            if (File.Exists(db)) File.Delete(db);
            var store = new FileLedgerStore(db);
            var reg = new PlayerRegistry();
            var ledger = AccountAuthority.Open(Config(), reg, store, out _);
            reg.Join("probe-account", out var p);
            ledger.GrantSeed(p);
            int m = reg.MatchId;
            ledger.Reward(p, RewardReason.Kill, 150, m, "kill:probe");
            ledger.Debit(p, 250, m, "buy-rifle");
            ledger.MarkFulfilled(WalletLedger.PurchaseKey("probe-account", m, "buy-rifle"));
            ledger.Debit(p, 600, m, "buy-jeep"); // never delivered: process "crashes" now
            Debug.Log($"[Probe] PHASE1 match={m} balance={ledger.Balance(p)} (expect 9300)");
            // Exit without disposing the store, like an abrupt process end.
            EditorApplication.Exit(ledger.Balance(p) == 9300 ? 0 : 1);
        }

        public static void Phase2()
        {
            string db = DbPath();
            using (var store = new FileLedgerStore(db))
            {
                var reg = new PlayerRegistry();
                var ledger = AccountAuthority.Open(Config(), reg, store, out int refunds);
                reg.Join("probe-account", out var p);
                var seed = ledger.GrantSeed(p);
                int afterRestart = ledger.Balance(p);
                int stale = (int)ledger.Debit(p, 100, 1, "stale").Status;
                reg.BeginNewMatch();
                int afterNewMatch = ledger.Balance(p);
                bool ok = refunds == 1 && seed.Status == TxStatus.Duplicate && afterRestart == 9900
                          && afterNewMatch == 9900 && stale == (int)TxStatus.StaleMatch && ledger.Reconciles(out _);
                Debug.Log($"[Probe] PHASE2 refunds={refunds} seed={seed.Status} balanceAfterRestart={afterRestart} " +
                          $"staleOldMatchDebit={(TxStatus)stale} balanceAfterNewMatch={afterNewMatch} match={reg.MatchId} => {(ok ? "PASS" : "FAIL")}");
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }
    }
}
