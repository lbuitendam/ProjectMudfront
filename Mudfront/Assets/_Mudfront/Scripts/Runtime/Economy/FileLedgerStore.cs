using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Mudfront.Economy
{
    /// <summary>Server-side durable account ledger (ECON-03A). An append-only journal of checksummed
    /// JSON lines, each flushed to disk (fsync) before <see cref="Commit"/> returns, so the ledger
    /// never acknowledges money that is not on disk. The file is opened exclusively: only one
    /// authority process can own a given account database, which prevents two servers spending the
    /// same balance concurrently. Never use this from a client: a client-side file is never trusted
    /// for online cash.</summary>
    public sealed class FileLedgerStore : ILedgerStore, IDisposable
    {
        public const int SchemaVersion = 1;
        const string HeaderPrefix = "#mudfront-ledger schema=";

        readonly FileStream _file;
        readonly List<LedgerRecord> _loaded = new List<LedgerRecord>();

        public string Path { get; }
        public IReadOnlyList<LedgerRecord> Loaded => _loaded;
        /// <summary>True when a torn trailing write (crash mid-commit) was discarded at open.</summary>
        public bool DiscardedTornTail { get; private set; }
        /// <summary>Test hook: makes the next commits fail as if the disk were full/unavailable.</summary>
        public bool SimulateFailure { get; set; }

        public FileLedgerStore(string path)
        {
            Path = path;
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            // FileShare.None: a second authority (process or handle) cannot open the same database.
            _file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try { Load(); }
            catch { _file.Dispose(); throw; } // never keep the database locked after refusing it
        }

        void Load()
        {
            _file.Position = 0;
            var bytes = new byte[_file.Length];
            int read = 0;
            while (read < bytes.Length) read += _file.Read(bytes, read, bytes.Length - read);
            string text = Encoding.UTF8.GetString(bytes);

            if (text.Length == 0)
            {
                WriteLine(HeaderPrefix + SchemaVersion);
                return;
            }

            long goodEnd = 0;
            int pos = 0, lineNo = 0;
            while (pos < text.Length)
            {
                int nl = text.IndexOf('\n', pos);
                if (nl < 0)
                {
                    // Torn tail: the process died mid-write. That record was never acknowledged.
                    DiscardedTornTail = true;
                    break;
                }
                string line = text.Substring(pos, nl - pos).TrimEnd('\r');
                lineNo++;
                if (lineNo == 1)
                {
                    if (!line.StartsWith(HeaderPrefix) || !int.TryParse(line.Substring(HeaderPrefix.Length), out int v))
                        throw new InvalidDataException($"{Path}: missing ledger header.");
                    if (v > SchemaVersion)
                        throw new InvalidDataException($"{Path}: schema {v} is newer than supported {SchemaVersion}.");
                }
                else if (line.Length > 0)
                {
                    int bar = line.IndexOf('|');
                    if (bar < 0 || Checksum(line.Substring(bar + 1)) != line.Substring(0, bar))
                    {
                        // A complete line with a bad checksum is corruption, not a crash: refuse to
                        // start rather than invent or lose spendable cash. Restore from backup.
                        throw new InvalidDataException($"{Path}: corrupt record at line {lineNo}; restore from backup.");
                    }
                    _loaded.Add(JsonUtility.FromJson<LedgerRecord>(line.Substring(bar + 1)));
                }
                pos = nl + 1;
                goodEnd = Encoding.UTF8.GetByteCount(text.Substring(0, pos));
            }
            if (DiscardedTornTail) _file.SetLength(goodEnd);
            _file.Position = _file.Length;
        }

        public bool Commit(LedgerRecord record)
        {
            if (SimulateFailure) return false;
            try
            {
                string json = JsonUtility.ToJson(record);
                WriteLine(Checksum(json) + "|" + json);
                return true;
            }
            catch (IOException) { return false; }
        }

        void WriteLine(string line)
        {
            var b = Encoding.UTF8.GetBytes(line + "\n");
            _file.Position = _file.Length;
            _file.Write(b, 0, b.Length);
            _file.Flush(true); // durable before acknowledged
        }

        /// <summary>Consistent copy for backups, taken while the authority holds the file.</summary>
        public void Backup(string destination)
        {
            _file.Flush(true);
            long keep = _file.Position;
            _file.Position = 0;
            using (var o = new FileStream(destination, FileMode.Create, FileAccess.Write))
                _file.CopyTo(o);
            _file.Position = keep;
        }

        /// <summary>Test/crash hook: appends raw bytes without a newline, like a write cut by a crash.</summary>
        public void AppendRawForTest(string partial)
        {
            var b = Encoding.UTF8.GetBytes(partial);
            _file.Position = _file.Length;
            _file.Write(b, 0, b.Length);
            _file.Flush(true);
        }

        static string Checksum(string s)
        {
            // FNV-1a 64-bit: detects torn/corrupt lines; not a security signature.
            ulong h = 14695981039346656037UL;
            foreach (byte c in Encoding.UTF8.GetBytes(s)) { h ^= c; h *= 1099511628211UL; }
            return h.ToString("x16");
        }

        public void Dispose() => _file.Dispose();
    }

    /// <summary>Wires a durable store to a ledger and performs crash recovery on startup.</summary>
    public static class AccountAuthority
    {
        public static WalletLedger Open(EconomyConfig cfg, PlayerRegistry players, FileLedgerStore store, out int recoveredRefunds)
        {
            int lastMatch = 0;
            foreach (var r in store.Loaded) if (r.MatchId > lastMatch) lastMatch = r.MatchId;
            players.ResumeAfter(lastMatch);
            var ledger = new WalletLedger(cfg, players, store);
            ledger.Restore(store.Loaded);
            recoveredRefunds = ledger.RecoverInterruptedPurchases();
            return ledger;
        }
    }
}
