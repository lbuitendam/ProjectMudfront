using System.Collections.Generic;

namespace Mudfront.Net
{
    /// <summary>Per-connection token bucket for client requests. Reusable server validation.</summary>
    public class RequestRateLimiter
    {
        class Bucket { public double Tokens; public double Last; public int Violations; }

        readonly double _rate, _burst;
        readonly int _kickAfter;
        readonly Dictionary<int, Bucket> _b = new Dictionary<int, Bucket>();

        public RequestRateLimiter(double perSecond, double burst, int kickAfterViolations)
        {
            _rate = perSecond; _burst = burst; _kickAfter = kickAfterViolations;
        }

        public enum Verdict { Allow, Drop, Kick }

        public Verdict Check(int connId, double now)
        {
            if (!_b.TryGetValue(connId, out var b)) _b[connId] = b = new Bucket { Tokens = _burst, Last = now };
            b.Tokens = System.Math.Min(_burst, b.Tokens + (now - b.Last) * _rate);
            b.Last = now;
            if (b.Tokens >= 1) { b.Tokens -= 1; return Verdict.Allow; }
            return ++b.Violations >= _kickAfter ? Verdict.Kick : Verdict.Drop;
        }

        public void Forget(int connId) => _b.Remove(connId);
    }
}
