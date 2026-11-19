using System;

namespace Mudfront.Economy
{
    /// <summary>One reward line. Amount is a flat payout, or a per-unit rate when PerUnit is true
    /// (per HP healed or repaired, per round supplied). Window, interval, distance and time fields
    /// are only read by the reasons that need them.</summary>
    [Serializable]
    public class RewardRule
    {
        public RewardReason Reason;
        public int Amount;
        public bool PerUnit;
        public bool ZoneBonusEligible;
        public CapScope CapScope = CapScope.None;
        public int Cap;
        public float WindowSeconds;
        public float IntervalSeconds;
        public float MinDistance;
        public float MinSeconds;
    }

    [Serializable]
    public class FreeKitEntry
    {
        public ItemDefinition Item;
        public int Quantity = 1;
    }
}
