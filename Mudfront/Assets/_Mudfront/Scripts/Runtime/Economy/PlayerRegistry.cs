using System;
using System.Collections.Generic;

namespace Mudfront.Economy
{
    /// <summary>Session handle for a connected account on this server. Never derived from TeamId,
    /// display name or a scene instance ID. The persistent identity is the AccountId.</summary>
    [Serializable]
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly int Value;
        public PlayerId(int v) { Value = v; }
        public bool IsValid => Value > 0;
        public bool Equals(PlayerId o) => Value == o.Value;
        public override bool Equals(object o) => o is PlayerId p && Equals(p);
        public override int GetHashCode() => Value;
        public override string ToString() => "P" + Value;
        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
    }

    public enum JoinStatus { Ok, Reconnected, InvalidAccount, AlreadyConnected }

    /// <summary>Binds authenticated AccountIds to stable PlayerIds and issues match and life IDs.
    /// Owned by the authority. The AccountId must come from authentication (ECON-03A/04), never
    /// from an unauthenticated client field. An account has at most one live session.</summary>
    public class PlayerRegistry
    {
        class Entry { public PlayerId Id; public string AccountId; public int LifeId; public bool Connected; }

        readonly Dictionary<PlayerId, Entry> _players = new Dictionary<PlayerId, Entry>();
        readonly Dictionary<string, PlayerId> _byAccount = new Dictionary<string, PlayerId>();
        int _nextPlayer = 1, _nextLife = 1;

        public int MatchId { get; private set; } = 1;
        public event Action<int> MatchStarted;

        public IEnumerable<PlayerId> Players => _players.Keys;
        public bool Contains(PlayerId p) => _players.ContainsKey(p);
        public bool IsConnected(PlayerId p) => _players.TryGetValue(p, out var e) && e.Connected;
        public string AccountOf(PlayerId p) => _players.TryGetValue(p, out var e) ? e.AccountId : null;

        /// <summary>Authenticated account joins (or rejoins). The same account always maps to the
        /// same PlayerId on this server. A second simultaneous session is refused.</summary>
        public JoinStatus Join(string authenticatedAccountId, out PlayerId player)
        {
            player = default;
            if (string.IsNullOrWhiteSpace(authenticatedAccountId)) return JoinStatus.InvalidAccount;
            if (_byAccount.TryGetValue(authenticatedAccountId, out var existing))
            {
                var e = _players[existing];
                if (e.Connected) return JoinStatus.AlreadyConnected;
                e.Connected = true;
                player = existing;
                return JoinStatus.Reconnected;
            }
            var n = new Entry { Id = new PlayerId(_nextPlayer++), AccountId = authenticatedAccountId, Connected = true };
            _players[n.Id] = n;
            _byAccount[authenticatedAccountId] = n.Id;
            player = n.Id;
            return JoinStatus.Ok;
        }

        public void MarkDisconnected(PlayerId p) { if (_players.TryGetValue(p, out var e)) e.Connected = false; }

        /// <summary>Starts a new life (first spawn or after final death). Revives keep the LifeId.</summary>
        public int StartLife(PlayerId p)
        {
            var e = _players[p];
            e.LifeId = _nextLife++;
            return e.LifeId;
        }

        public int CurrentLife(PlayerId p) => _players.TryGetValue(p, out var e) ? e.LifeId : 0;

        /// <summary>After a server restart, continue numbering above every match in the durable
        /// ledger so actions from earlier matches stay invalid.</summary>
        public void ResumeAfter(int lastPersistedMatch)
        {
            if (lastPersistedMatch >= MatchId) MatchId = lastPersistedMatch + 1;
        }

        /// <summary>Begins the next match: new MatchId, lives end. Accounts, money and unlocks persist.</summary>
        public int BeginNewMatch()
        {
            MatchId++;
            foreach (var e in _players.Values) e.LifeId = 0;
            MatchStarted?.Invoke(MatchId);
            return MatchId;
        }
    }
}
