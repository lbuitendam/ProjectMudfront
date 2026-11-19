using System;

namespace Mudfront.Economy
{
    /// <summary>The four GDD classes. Flags so an item can list several eligible classes.</summary>
    [Flags]
    public enum ClassMask
    {
        None = 0,
        Assault = 1 << 0,
        Scout = 1 << 1,
        Engineer = 1 << 2,
        Medic = 1 << 3,
        All = Assault | Scout | Engineer | Medic,
    }

    public enum ItemCategory { Weapon, Ammo, Medical, Utility, Tool }

    /// <summary>Inventory slot an item occupies. Ammo lives in the reserve pool, not a hand slot.</summary>
    public enum ItemSlot { Primary, Secondary, Gadget, Consumable, AmmoReserve }

    public enum StationKind { Equipment, VehicleDelivery }

    /// <summary>Reason codes for credited money. Stable integers: they are stored in the ledger.</summary>
    public enum RewardReason
    {
        MatchEntry = 1,
        Kill = 10,
        DamageAssist = 11,
        Revive = 20,
        TeammateHeal = 21,
        ObjectivePresence = 30,
        PassengerTrip = 40,
        Repair = 50,
        SpotAssist = 60,
        Construction = 70,
        Resupply = 71,
        VehicleDestroyed = 80,
        BuildableDestroyed = 81,
    }

    /// <summary>What a reward cap is counted against.</summary>
    public enum CapScope { None, PerEvent, PerVictimLife, PerRecipientLife, PerAssetLife, PerMatch }
}
