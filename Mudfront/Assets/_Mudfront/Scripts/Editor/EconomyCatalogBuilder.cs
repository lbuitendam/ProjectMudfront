using System.IO;
using System.Text;
using Mudfront.Economy;
using UnityEditor;
using UnityEngine;

namespace Mudfront.EditorTools
{
    /// <summary>Creates the default economy data assets (only those that are missing, so designer
    /// edits are never overwritten) and validates the catalog.
    /// Batch: Unity -batchmode -projectPath Mudfront -executeMethod Mudfront.EditorTools.EconomyCatalogBuilder.BuildAndValidate -quit</summary>
    public static class EconomyCatalogBuilder
    {
        public const string Folder = "Assets/_Mudfront/Data/Economy";
        public const string ConfigPath = Folder + "/EconomyConfig.asset";
        const string ReportPath = "../docs/verification/economy_catalog_report.md";

        [MenuItem("Mudfront/Economy/Create Missing Default Catalog")]
        public static EconomyConfig CreateMissingDefaults()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Folder + "/Items");
            Directory.CreateDirectory(Folder + "/Vehicles");
            Directory.CreateDirectory(Folder + "/Stations");
            AssetDatabase.Refresh();

            var ammoRifle = Item("ammo_rifle", "Rifle Ammo (30)", ItemCategory.Ammo, ItemSlot.AmmoReserve, 30, i =>
            { i.QuantityPerPurchase = 30; i.MaxStack = 180; i.BehaviorId = ItemBehaviors.ReserveAmmo; });
            var ammoPistol = Item("ammo_pistol", "Pistol Ammo (24)", ItemCategory.Ammo, ItemSlot.AmmoReserve, 15, i =>
            { i.QuantityPerPurchase = 24; i.MaxStack = 72; i.BehaviorId = ItemBehaviors.ReserveAmmo; });
            var rifleBasic = Item("rifle_basic", "Field Rifle (free)", ItemCategory.Weapon, ItemSlot.Primary, 0, i =>
            {
                i.Purchasable = false; Weapon(i, 25f, 8f, 150f, 30, 1.5f, "ammo_rifle");
            });
            var rifle = Item("rifle_service", "Service Rifle", ItemCategory.Weapon, ItemSlot.Primary, 250, i =>
                Weapon(i, 34f, 6f, 200f, 25, 1.8f, "ammo_rifle"));
            var pistol = Item("pistol", "Sidearm", ItemCategory.Weapon, ItemSlot.Secondary, 100, i =>
                Weapon(i, 30f, 4f, 60f, 12, 1.2f, "ammo_pistol"));
            var bandage = Item("bandage", "Bandage", ItemCategory.Medical, ItemSlot.Consumable, 50, i =>
            { i.MaxStack = 3; i.BehaviorId = ItemBehaviors.Bandage; i.EffectAmount = 40f; i.UseDuration = 2.5f; i.EffectRange = 2f; });
            var chute = Item("parachute", "Parachute", ItemCategory.Utility, ItemSlot.Gadget, 150, i =>
            { i.BehaviorId = ItemBehaviors.Parachute; i.EffectAmount = 6f; });
            var repair = Item("repair_tool", "Repair Tool", ItemCategory.Tool, ItemSlot.Gadget, 150, i =>
            { i.BehaviorId = ItemBehaviors.RepairTool; i.EffectAmount = 25f; i.EffectRange = 3f; });

            var jeep = Load<VehicleDefinition>(Folder + "/Vehicles/Vehicle_jeep_transport.asset", v =>
            {
                v.Id = "jeep_transport"; v.DisplayName = "Transport Jeep"; v.Price = 600;
                v.BehaviorId = ItemBehaviors.TransportVehicle; v.Seats = 4; v.MaxHealth = 300f;
            });

            var equip = Load<StationDefinition>(Folder + "/Stations/Station_hq_equipment.asset", s =>
            {
                s.Id = "hq_equipment"; s.DisplayName = "HQ Quartermaster"; s.Kind = StationKind.Equipment;
                s.Items.AddRange(new[] { rifle, pistol, ammoRifle, ammoPistol, bandage, chute, repair });
            });
            var motor = Load<StationDefinition>(Folder + "/Stations/Station_hq_vehicles.asset", s =>
            {
                s.Id = "hq_vehicles"; s.DisplayName = "HQ Motor Pool"; s.Kind = StationKind.VehicleDelivery;
                s.Vehicles.Add(jeep);
            });

            var cfg = Load<EconomyConfig>(ConfigPath, c =>
            {
                c.Items.AddRange(new[] { rifleBasic, rifle, pistol, ammoRifle, ammoPistol, bandage, chute, repair });
                c.Vehicles.Add(jeep);
                c.Stations.AddRange(new[] { equip, motor });
                c.FreeKit.Add(new FreeKitEntry { Item = rifleBasic, Quantity = 1 });
                c.FreeKit.Add(new FreeKitEntry { Item = ammoRifle, Quantity = 60 });
                c.Rewards.AddRange(new[]
                {
                    new RewardRule { Reason = RewardReason.Kill, Amount = 150, ZoneBonusEligible = true, CapScope = CapScope.PerVictimLife, Cap = 150 },
                    new RewardRule { Reason = RewardReason.DamageAssist, Amount = 75, ZoneBonusEligible = true, CapScope = CapScope.PerVictimLife, Cap = 75, WindowSeconds = 15f },
                    new RewardRule { Reason = RewardReason.Revive, Amount = 150, ZoneBonusEligible = true, CapScope = CapScope.PerEvent, Cap = 150 },
                    new RewardRule { Reason = RewardReason.TeammateHeal, Amount = 1, PerUnit = true, ZoneBonusEligible = true, CapScope = CapScope.PerRecipientLife, Cap = 100 },
                    new RewardRule { Reason = RewardReason.ObjectivePresence, Amount = 20, IntervalSeconds = 10f },
                    new RewardRule { Reason = RewardReason.PassengerTrip, Amount = 100, CapScope = CapScope.PerEvent, Cap = 100, MinDistance = 75f, MinSeconds = 10f },
                    new RewardRule { Reason = RewardReason.Repair, Amount = 1, PerUnit = true, ZoneBonusEligible = true, CapScope = CapScope.PerAssetLife, Cap = 150 },
                    new RewardRule { Reason = RewardReason.SpotAssist, Amount = 50, ZoneBonusEligible = true, WindowSeconds = 15f },
                    new RewardRule { Reason = RewardReason.Construction, Amount = 50, ZoneBonusEligible = true },
                    new RewardRule { Reason = RewardReason.Resupply, Amount = 1, PerUnit = true, ZoneBonusEligible = true, CapScope = CapScope.PerRecipientLife, Cap = 60 },
                    new RewardRule { Reason = RewardReason.VehicleDestroyed, Amount = 200, ZoneBonusEligible = true, CapScope = CapScope.PerAssetLife, Cap = 200 },
                    new RewardRule { Reason = RewardReason.BuildableDestroyed, Amount = 50, ZoneBonusEligible = true, CapScope = CapScope.PerAssetLife, Cap = 50 },
                });
            });
            AssetDatabase.SaveAssets();
            return cfg;
        }

        static void Weapon(ItemDefinition i, float dmg, float rate, float range, int mag, float reload, string ammo)
        {
            i.BehaviorId = ItemBehaviors.HitscanWeapon; i.Damage = dmg; i.FireRate = rate; i.Range = range;
            i.MagazineSize = mag; i.ReloadTime = reload; i.AmmoItemId = ammo;
        }

        static ItemDefinition Item(string id, string name, ItemCategory cat, ItemSlot slot, int price, System.Action<ItemDefinition> init) =>
            Load<ItemDefinition>($"{Folder}/Items/Item_{id}.asset", i =>
            {
                i.Id = id; i.DisplayName = name; i.Category = cat; i.Slot = slot; i.Price = price;
                init(i);
            });

        /// <summary>Loads an existing asset untouched, or creates and initialises it.</summary>
        static T Load<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            init(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        [MenuItem("Mudfront/Economy/Validate Catalog")]
        public static bool ValidateAndReport()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<EconomyConfig>(ConfigPath);
            var errors = CatalogValidator.Validate(cfg);
            var sb = new StringBuilder();
            sb.AppendLine("# Economy catalog validation\n");
            sb.AppendLine($"Generated {System.DateTime.Now:yyyy-MM-dd HH:mm} by `EconomyCatalogBuilder` (Unity {Application.unityVersion}). Source: `{ConfigPath}`.\n");
            sb.AppendLine(errors.Count == 0 ? "**Result: VALID**\n" : $"**Result: {errors.Count} ERROR(S)**\n");
            foreach (var err in errors) sb.AppendLine($"- {err}");
            if (cfg != null)
            {
                sb.AppendLine("\n| Item | Category | Slot | Price | Qty/buy | Max | Behaviour | Offered |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|");
                foreach (var i in cfg.Items)
                    if (i != null)
                        sb.AppendLine($"| {i.Id} | {i.Category} | {i.Slot} | {(i.Purchasable ? i.Price.ToString() : "free kit")} | {i.QuantityPerPurchase} | {i.MaxStack} | {i.BehaviorId} | {(EconomyConfig.IsAvailable(i) ? "yes" : "hidden (not implemented)")} |");
                foreach (var v in cfg.Vehicles)
                    if (v != null)
                        sb.AppendLine($"| {v.Id} | Vehicle | {v.Seats} seats | {v.Price} | 1 | {v.MaxAlivePerBuyer} | {v.BehaviorId} | {(EconomyConfig.IsAvailable(v) ? "yes" : "hidden (not implemented)")} |");
            }
            File.WriteAllText(ReportPath, sb.ToString());
            foreach (var err in errors) Debug.LogError("[Economy] " + err);
            Debug.Log($"[Economy] Catalog validation: {(errors.Count == 0 ? "VALID" : errors.Count + " errors")}");
            return errors.Count == 0;
        }

        public static void BuildAndValidate()
        {
            CreateMissingDefaults();
            bool ok = ValidateAndReport();
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
