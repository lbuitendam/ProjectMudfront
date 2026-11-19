using System.Collections.Generic;
using UnityEngine;

namespace Mudfront.Economy
{
    /// <summary>Root economy asset: catalog, stations, rewards and global rules (DECISIONS.md revision 2).</summary>
    [CreateAssetMenu(menuName = "Mudfront/Economy/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        public const int CurrentDataVersion = 2;
        [Tooltip("Schema version of this asset; migrated by EconomyCatalogBuilder.")]
        public int DataVersion = 1; // assets predating revision 2 deserialize as 1; the builder migrates

        [Header("Persistent account wallet")]
        [Tooltip("Credited once when an account is created. Never on match entry, death or respawn.")]
        public int AccountSeedGrant = 10000;
        [Tooltip("Hard ceiling on any balance; credits above it are not paid.")]
        public int MaxBalance = 10000000;

        [Header("Zone bonus")]
        [Tooltip("Applied once to eligible rewards earned inside the active Control Zone, then rounded.")]
        public float ZoneMultiplier = 1.5f;

        [Header("Purchased vehicles")]
        public OwnerFinalDeathPolicy OwnerFinalDeath = OwnerFinalDeathPolicy.UnlockForEveryone;
        [Tooltip("Seconds a disconnected owner's vehicle waits before despawning without refund.")]
        public float OwnerDisconnectGrace = 60f;

        [Header("Revive")]
        [Tooltip("Base hold-to-revive time with no specialist kit.")]
        public float BasicReviveSeconds = 8f;

        [Header("Catalog")]
        public List<ItemDefinition> Items = new List<ItemDefinition>();
        public List<VehicleDefinition> Vehicles = new List<VehicleDefinition>();
        public List<StationDefinition> Stations = new List<StationDefinition>();
        [Tooltip("Claimed once per life from a friendly trader; cannot stack or be sold.")]
        public List<FreeKitEntry> FreeKit = new List<FreeKitEntry>();

        [Header("Rewards")]
        public List<RewardRule> Rewards = new List<RewardRule>();

        public ItemDefinition FindItem(string id) => Items.Find(i => i != null && i.Id == id);
        public VehicleDefinition FindVehicle(string id) => Vehicles.Find(v => v != null && v.Id == id);
        public RewardRule FindReward(RewardReason r) => Rewards.Find(x => x != null && x.Reason == r);

        /// <summary>Purchasable and its gameplay behaviour is implemented. (Unlock state is per account
        /// and checked by the purchase service.)</summary>
        public static bool IsAvailable(ItemDefinition i) =>
            i != null && i.Purchasable && ItemBehaviors.IsImplemented(i.BehaviorId);

        public static bool IsAvailable(VehicleDefinition v) =>
            v != null && v.Prefab != null && ItemBehaviors.IsImplemented(v.BehaviorId);
    }
}
