using System;
using System.Collections.Generic;

namespace Mudfront.Economy
{
    /// <summary>Server-assigned stable player identity. Never derived from TeamId, display name or
    /// a scene instance ID.</summary>
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

    /// <summary>Issues player, match and life IDs and maps reconnects. Owned by the authority
    /// (offline host or dedicated server). Reconnect uses a secret token the server issued to that
    /// player; a client-chosen PlayerId is never trusted.</summary>
    public class PlayerRegistry
    {
        class Entry { public PlayerId Id; public string Token; public int LifeId; public bool Connected; }

        readonly Dictionary<PlayerId, Entry> _players = new Dictionary<PlayerId, Entry>();
        readonly Dictionary<string, PlayerId> _byToken = new Dictionary<string, PlayerId>();
        readonly Func<string> _tokenSource;
        int _nextPlayer = 1, _nextLife = 1;

        public int MatchId { get; private set; } = 1;
        public event Action<int> MatchStarted;

        public PlayerRegistry(Func<string> tokenSource = null)
        {
            _tokenSource = tokenSource ?? (() => Guid.NewGuid().ToString("N"));
        }

        public IEnumerable<PlayerId> Players => _players.Keys;
        public bool Contains(PlayerId p) => _players.ContainsKey(p);
        public bool IsConnected(PlayerId p) => _players.TryGetValue(p, out var e) && e.Connected;

        /// <summary>New player joining. Returns the ID and the private reconnect token for this match.</summary>
        public PlayerId Register(out string reconnectToken)
        {
            var e = new Entry { Id = new PlayerId(_nextPlayer++), Connected = true };
            e.Token = _tokenSource();
            _players[e.Id] = e;
            _byToken[e.Token] = e.Id;
            reconnectToken = e.Token;
            return e.Id;
        }

        public void MarkDisconnected(PlayerId p) { if (_players.TryGetValue(p, out var e)) e.Connected = false; }

        /// <summary>Maps a reconnect token back to its player within the same match. Unknown or
        /// expired tokens fail; the caller should then Register a new player.</summary>
        public bool TryReconnect(string token, out PlayerId player)
        {
            player = default;
            if (string.IsNullOrEmpty(token) || !_byToken.TryGetValue(token, out var id)) return false;
            var e = _players[id];
            e.Connected = true;
            player = id;
            return true;
        }

        public bool TryGetToken(PlayerId p, out string token)
        {
            token = _players.TryGetValue(p, out var e) ? e.Token : null;
            return token != null;
        }

        /// <summary>Starts a new life (after final death or at first spawn). Revives keep the LifeId.</summary>
        public int StartLife(PlayerId p)
        {
            var e = _players[p];
            e.LifeId = _nextLife++;
            return e.LifeId;
        }

        public int CurrentLife(PlayerId p) => _players.TryGetValue(p, out var e) ? e.LifeId : 0;

        /// <summary>Begins the next match: new MatchId, fresh reconnect tokens for everyone, drops
        /// disconnected players (their match-only state is gone).</summary>
        public int BeginNewMatch()
        {
            MatchId++;
            _byToken.Clear();
            var gone = new List<PlayerId>();
            foreach (var e in _players.Values)
            {
                if (!e.Connected) { gone.Add(e.Id); continue; }
                e.Token = _tokenSource();
                e.LifeId = 0;
                _byToken[e.Token] = e.Id;
            }
            foreach (var g in gone) _players.Remove(g);
            MatchStarted?.Invoke(MatchId);
            return MatchId;
        }
    }
}
