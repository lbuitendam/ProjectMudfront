using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using FishNet.Authenticating;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

namespace Mudfront.Net
{
    /// <summary>Binds each connection to an authenticated AccountId. Development scheme: the token is
    /// HMAC-SHA256(serverSecret, accountId), issued by the dev account tool; a client cannot mint a
    /// token for another account without the server secret. Production replaces <see cref="Verify"/>
    /// with platform-ticket verification (see economy_report ECON-03A). The client-supplied
    /// AccountId is trusted only after verification, and the server keeps the mapping.</summary>
    public class AccountAuthenticator : Authenticator
    {
        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        /// <summary>Server only. Never shipped to clients.</summary>
        [NonSerialized] public string ServerSecret;
        [NonSerialized] public string ClientAccountId;
        [NonSerialized] public string ClientToken;

        /// <summary>Server: returns false to refuse (e.g. account already has a live session).</summary>
        public Func<NetworkConnection, string, string> AdmitAccount; // returns null if OK, else reason

        public event Action<AuthResult> ClientResult;

        readonly Dictionary<int, string> _accounts = new Dictionary<int, string>();

        public bool TryGetAccount(NetworkConnection c, out string account) => _accounts.TryGetValue(c.ClientId, out account);
        public void Forget(NetworkConnection c) => _accounts.Remove(c.ClientId);

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            NetworkManager.ServerManager.RegisterBroadcast<AuthRequest>(OnAuthRequest, false);
            NetworkManager.ClientManager.RegisterBroadcast<AuthResult>((r, _) => ClientResult?.Invoke(r));
            NetworkManager.ClientManager.OnClientConnectionState += args =>
            {
                if (args.ConnectionState == LocalConnectionState.Started)
                    NetworkManager.ClientManager.Broadcast(new AuthRequest { AccountId = ClientAccountId, Token = ClientToken });
            };
        }

        void OnAuthRequest(NetworkConnection conn, AuthRequest req, Channel ch)
        {
            if (conn.IsAuthenticated) { conn.Disconnect(true); return; }
            string reason = null;
            if (string.IsNullOrEmpty(ServerSecret)) reason = "server has no auth secret";
            else if (!Verify(req.AccountId, req.Token, ServerSecret)) reason = "invalid credentials";
            else if (AdmitAccount != null) reason = AdmitAccount(conn, req.AccountId);

            bool ok = reason == null;
            if (ok) _accounts[conn.ClientId] = req.AccountId;
            NetworkManager.ServerManager.Broadcast(conn, new AuthResult { Passed = ok, Reason = reason ?? "ok", AccountId = ok ? req.AccountId : null }, false);
            Debug.Log($"[Net] auth conn{conn.ClientId} account='{req.AccountId}' => {(ok ? "PASS" : "FAIL: " + reason)}");
            OnAuthenticationResult?.Invoke(conn, ok);
        }

        public static string IssueDevToken(string accountId, string secret)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(accountId ?? ""))).Replace("-", "").ToLowerInvariant();
        }

        public static bool Verify(string accountId, string token, string secret)
        {
            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrEmpty(token)) return false;
            string expect = IssueDevToken(accountId, secret);
            if (expect.Length != token.Length) return false;
            int diff = 0; // constant-time compare
            for (int i = 0; i < expect.Length; i++) diff |= expect[i] ^ token[i];
            return diff == 0;
        }
    }
}
