namespace Mudfront.Economy
{
    // Revision 2 (DECISIONS.md): equipment defines the role. There are no class gates; the old
    // ClassMask/AllowedClasses fields are removed. Specialist gear is gated by an unlock track.

    /// <summary>Role-XP track that gates a one-time cash unlock. None = unlocked from the start.</summary>
    public enum UnlockTrack { None, Assault, Recon, Medical, Support, Driver, Pilot }

    public enum ItemCategory { Weapon, Ammo, Medical, Utility, Tool, Armour, Grenade, Attachment }

    /// <summary>Inventory slot an item occupies. Ammo lives in the reserve pool, not a hand slot.</summary>
    public enum ItemSlot { Primary, Secondary, Gadget, Consumable, AmmoReserve, Armour, Grenade, Attachment }

    public enum StationKind { Equipment, VehicleDelivery }

    public enum VehicleKind { Ground, Helicopter }

    /// <summary>Access state of a purchased vehicle. WorldUnlocked admits everyone, enemies included;
    /// it never transfers ownership. Only the buyer may change it.</summary>
    public enum VehicleLockMode { OwnerOnly, WorldUnlocked }

    /// <summary>What happens to an intact purchased vehicle when its buyer suffers final death.</summary>
    public enum OwnerFinalDeathPolicy { UnlockForEveryone, RetainOwnerLock }

    /// <summary>Reason codes for credited money. Stable integers: they are stored in the ledger.</summary>
    public enum RewardReason
    {
        AccountSeed = 1,
        Kill = 10,
        DamageAssist = 11,
        Revive = 20,
        TeammateHeal = 21,
        ObjectivePresence = 30,
        PassengerTrip = 40,
        FollowUpKill = 41,
        Repair = 50,
        SpotAssist = 60,
        Construction = 70,
        Resupply = 71,
        VehicleDestroyed = 80,
        BuildableDestroyed = 81,
    }

    /// <summary>What a reward cap is counted against.</summary>
    public enum CapScope { None, PerEvent, PerVictimLife, PerRecipientLife, PerAssetLife, PerMatch, PerProviderRecipientLife }
}
