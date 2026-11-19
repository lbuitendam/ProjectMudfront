using FishNet.Broadcast;

namespace Mudfront.Net
{
    // Wire contract (ECON-04). Clients only REQUEST; every amount, price, owner and identity is
    // decided on the server from the authenticated connection and server data.

    /// <summary>Client -> server, before authentication. Token is issued out of band by the
    /// account service (dev: HMAC of the AccountId with the server secret).</summary>
    public struct AuthRequest : IBroadcast { public string AccountId; public string Token; }

    /// <summary>Server -> client.</summary>
    public struct AuthResult : IBroadcast { public bool Passed; public string Reason; public string AccountId; }

    /// <summary>Client -> server: ask for my wallet.</summary>
    public struct WalletRequest : IBroadcast { }

    /// <summary>Server -> owning client only.</summary>
    public struct WalletSnapshot : IBroadcast { public string AccountId; public int Balance; public long Earned; public long Spent; public int MatchId; }

    /// <summary>Client -> server purchase intent. ClaimedPrice and ActingAccountId exist only so the
    /// server can prove it ignores/rejects them; they are never trusted.</summary>
    public struct PurchaseRequest : IBroadcast { public string RequestId; public string ItemId; public int ClaimedPrice; public string ActingAccountId; }

    /// <summary>Client -> server attempt to mint money directly. Always rejected: rewards are
    /// produced only by server-side gameplay events.</summary>
    public struct RewardClaim : IBroadcast { public string RequestId; public int Amount; public int Reason; }

    /// <summary>Server -> client result of a request.</summary>
    public struct TxResultMessage : IBroadcast { public string RequestId; public string Status; public int Balance; }

    /// <summary>Server -> client notification of a server-side credit (reward) to this account.</summary>
    public struct RewardNotice : IBroadcast { public int Reason; public int Amount; public int Balance; }
}
