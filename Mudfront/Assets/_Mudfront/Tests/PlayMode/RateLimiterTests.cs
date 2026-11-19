using Mudfront.Net;
using NUnit.Framework;

namespace Mudfront.Tests
{
    public class RateLimiterTests
    {
        [Test] public void Burst_ThenDrop_ThenKick_AndRefills()
        {
            var l = new RequestRateLimiter(perSecond: 10, burst: 5, kickAfterViolations: 3);
            for (int i = 0; i < 5; i++) Assert.AreEqual(RequestRateLimiter.Verdict.Allow, l.Check(1, 0));
            Assert.AreEqual(RequestRateLimiter.Verdict.Drop, l.Check(1, 0));
            Assert.AreEqual(RequestRateLimiter.Verdict.Drop, l.Check(1, 0));
            Assert.AreEqual(RequestRateLimiter.Verdict.Kick, l.Check(1, 0));
            Assert.AreEqual(RequestRateLimiter.Verdict.Allow, l.Check(2, 0), "other connections unaffected");
            Assert.AreEqual(RequestRateLimiter.Verdict.Allow, l.Check(1, 1.0), "refills over time");
        }

        [Test] public void DevToken_VerifiesOnlyMatchingAccountAndSecret()
        {
            string t = AccountAuthenticator.IssueDevToken("acc-a", "s1");
            Assert.IsTrue(AccountAuthenticator.Verify("acc-a", t, "s1"));
            Assert.IsFalse(AccountAuthenticator.Verify("acc-b", t, "s1"));
            Assert.IsFalse(AccountAuthenticator.Verify("acc-a", t, "s2"));
            Assert.IsFalse(AccountAuthenticator.Verify("acc-a", "", "s1"));
            Assert.IsFalse(AccountAuthenticator.Verify(" ", t, "s1"));
        }
    }
}
