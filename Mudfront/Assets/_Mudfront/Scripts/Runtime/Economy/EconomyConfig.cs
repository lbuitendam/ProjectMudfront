using System.Collections.Generic;
using UnityEngine;

namespace Mudfront.Economy
{
    /// <summary>Root economy asset: catalog, stations, rewards and global rules.</summary>
    [CreateAssetMenu(menuName = "Mudfront/Economy/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Wallet")]
        public int MatchEntryGrant = 1000;
        [Tooltip("Hard ceiling on any balance; credits above it are not paid.")]
        public int MaxBalance = 1000000;

        [Header("Zone bonus")]
        [Tooltip("Applied once to eligible rewards earned inside the active zone, then rounded.")]
        public float ZoneMultiplier = 1.5f;

        [Header("Catalog")]
        public List<ItemDefinition> Items = new List<ItemDefinition>();
        public List<VehicleDefinition> Vehicles = new List<VehicleDefinition>();
        public List<StationDefinition> Stations = new List<StationDefinition>();
        [Tooltip("Granted once per life at an equipment station; cannot stack or be sold.")]
        public List<FreeKitEntry> FreeKit = new List<FreeKitEntry>();

        [Header("Rewards")]
        public List<RewardRule> Rewards = new List<RewardRule>();

        public ItemDefinition FindItem(string id) => Items.Find(i => i != null && i.Id == id);
        public VehicleDefinition FindVehicle(string id) => Vehicles.Find(v => v != null && v.Id == id);
        public RewardRule FindReward(RewardReason r) => Rewards.Find(x => x != null && x.Reason == r);

        /// <summary>Purchasable and its gameplay behaviour is implemented.</summary>
        public static bool IsAvailable(ItemDefinition i) =>
            i != null && i.Purchasable && ItemBehaviors.IsImplemented(i.BehaviorId);

        public static bool IsAvailable(VehicleDefinition v) =>
            v != null && v.Prefab != null && ItemBehaviors.IsImplemented(v.BehaviorId);
    }
}
