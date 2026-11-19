using System.Collections.Generic;
using System.IO;
using System.Text;
using Mudfront.Economy;
using UnityEditor;
using UnityEngine;

namespace Mudfront.EditorTools
{
    /// <summary>Creates missing default economy assets, migrates the config to the current data
    /// version (adding missing defaults to lists, never overwriting edited values) and validates.
    /// Batch: Unity -batchmode -projectPath Mudfront -executeMethod Mudfront.EditorTools.EconomyCatalogBuilder.BuildAndValidate -quit</summary>
    public static class EconomyCatalogBuilder
    {
        public const string Folder = "Assets/_Mudfront/Data/Economy";
        public const string ConfigPath = Folder + "/EconomyConfig.asset";
        const string ReportPath = "../docs/verification/economy_catalog_report.md";

        [MenuItem("Mudfront/Economy/Create Or Migrate Catalog")]
        public static EconomyConfig CreateMissingDefaults()
        {
            foreach (var d in new[] { Folder, Folder + "/Items", Folder + "/Vehicles", Folder + "/Stations" }) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            // --- Starter & basic gear (tuning table) ---
            var ammoRifle = Item("ammo_rifle", "Rifle Ammo (30)", ItemCategory.Ammo, ItemSlot.AmmoReserve, 30, i =>
            { i.QuantityPerPurchase = 30; i.MaxStack = 180; i.BehaviorId = ItemBehaviors.ReserveAmmo; });
            var ammoPistol = Item("ammo_pistol", "Pistol Ammo (24)", ItemCategory.Ammo, ItemSlot.AmmoReserve, 15, i =>
            { i.QuantityPerPurchase = 24; i.MaxStack = 72; i.BehaviorId = ItemBehaviors.ReserveAmmo; });
            var rifleBasic = Item("rifle_basic", "Field Rifle (starter)", ItemCategory.Weapon, ItemSlot.Primary, 0, i =>
            { i.Purchasable = false; Weapon(i, 25f, 8f, 150f, 30, 1.5f, "ammo_rifle"); });
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

            // --- Revision 2 equipment (proposed defaults) ---
            var armour = Item("armour_vest", "Armour Vest", ItemCategory.Armour, ItemSlot.Armour, 200, i =>
            { i.BehaviorId = ItemBehaviors.Armour; i.DamageReduction = 0.35f; i.Durability = 120f; });
            var frag = Item("grenade_frag", "Frag Grenade", ItemCategory.Grenade, ItemSlot.Grenade, 75, i =>
            { i.MaxStack = 2; i.BehaviorId = ItemBehaviors.FragGrenade; i.FuseTime = 3f; i.BlastRadius = 6f; i.BlastDamage = 120f; i.UnlockTrack = UnlockTrack.Assault; i.UnlockXp = 100; i.UnlockFee = 300; });
            var smoke = Item("grenade_smoke", "Smoke Grenade", ItemCategory.Grenade, ItemSlot.Grenade, 50, i =>
            { i.MaxStack = 2; i.BehaviorId = ItemBehaviors.SmokeGrenade; i.FuseTime = 1.5f; i.BlastRadius = 7f; i.EffectDuration = 20f; });
            var scope = Item("scope_rifle", "Rifle Scope", ItemCategory.Attachment, ItemSlot.Attachment, 120, i =>
            {
                i.BehaviorId = ItemBehaviors.Attachment; i.SpreadMultiplier = 0.6f; i.ZoomMultiplier = 2f;
                i.CompatibleWeaponIds.AddRange(new[] { "rifle_basic", "rifle_service" });
                i.UnlockTrack = UnlockTrack.Recon; i.UnlockXp = 100; i.UnlockFee = 300;
            });
            var extMag = Item("extmag_rifle", "Extended Rifle Magazine", ItemCategory.Attachment, ItemSlot.Attachment, 100, i =>
            { i.BehaviorId = ItemBehaviors.Attachment; i.MagazineBonus = 10; i.CompatibleWeaponIds.AddRange(new[] { "rifle_basic", "rifle_service" }); });
            var medkit = Item("medkit_large", "Large Medical Kit", ItemCategory.Medical, ItemSlot.Gadget, 300, i =>
            {
                i.BehaviorId = ItemBehaviors.LargeMedkit; i.EffectAmount = 100f; i.UseDuration = 4f; i.EffectRange = 2.5f;
                i.Charges = 3; i.ReviveTimeMultiplier = 0.4f;
                i.UnlockTrack = UnlockTrack.Medical; i.UnlockXp = 200; i.UnlockFee = 1000;
            });
            var buildTool = Item("build_tool", "Construction Kit", ItemCategory.Tool, ItemSlot.Gadget, 250, i =>
            { i.BehaviorId = ItemBehaviors.BuildTool; i.EffectRange = 4f; i.UnlockTrack = UnlockTrack.Support; i.UnlockXp = 200; i.UnlockFee = 1000; });

            // --- Vehicles ---
            var jeep = Vehicle("jeep_transport", "Transport Jeep", 600, ItemBehaviors.TransportVehicle, v => { v.Seats = 4; v.MaxHealth = 300f; });
            var heliT = Vehicle("heli_transport", "Transport Helicopter", 2500, ItemBehaviors.TransportHelicopter, v =>
            { Heli(v, 6, 0, 500f); v.UnlockTrack = UnlockTrack.Pilot; v.UnlockFee = 2000; });
            var heliA = Vehicle("heli_armed", "Armed Transport Helicopter", 4000, ItemBehaviors.ArmedHelicopter, v =>
            {
                Heli(v, 6, 2, 600f); v.GunDamage = 18f; v.GunFireRate = 10f; v.GunAmmoCapacity = 400; v.AmmoPricePerRound = 1;
                v.UnlockTrack = UnlockTrack.Pilot; v.UnlockXp = 300; v.UnlockFee = 3000;
            });
            var heliK = Vehicle("heli_attack", "Attack Helicopter", 6000, ItemBehaviors.AttackHelicopter, v =>
            {
                Heli(v, 2, 1, 700f); v.GunDamage = 25f; v.GunFireRate = 8f; v.GunAmmoCapacity = 300; v.AmmoPricePerRound = 1;
                v.RocketCount = 14; v.RocketDamage = 150f; v.RocketBlastRadius = 5f; v.RocketPrice = 40;
                v.UnlockTrack = UnlockTrack.Pilot; v.UnlockXp = 800; v.UnlockFee = 5000;
            });

            var shopItems = new[] { rifle, pistol, ammoRifle, ammoPistol, bandage, chute, repair, armour, frag, smoke, scope, extMag, medkit, buildTool };
            var equip = Load<StationDefinition>(Folder + "/Stations/Station_hq_equipment.asset", s =>
            { s.Id = "hq_equipment"; s.DisplayName = "HQ Trader"; s.Kind = StationKind.Equipment; });
            var motor = Load<StationDefinition>(Folder + "/Stations/Station_hq_vehicles.asset", s =>
            { s.Id = "hq_vehicles"; s.DisplayName = "HQ Motor Pool"; s.Kind = StationKind.VehicleDelivery; });
            var helipad = Load<StationDefinition>(Folder + "/Stations/Station_hq_helipad.asset", s =>
            { s.Id = "hq_helipad"; s.DisplayName = "HQ Helipad"; s.Kind = StationKind.VehicleDelivery; });
            AddMissing(equip, equip.Items, shopItems);
            AddMissing(motor, motor.Vehicles, new[] { jeep });
            AddMissing(helipad, helipad.Vehicles, new[] { heliT, heliA, heliK });

            var cfg = Load<EconomyConfig>(ConfigPath, c => { });
            AddMissing(cfg, cfg.Items, new[] { rifleBasic }); AddMissing(cfg, cfg.Items, shopItems);
            AddMissing(cfg, cfg.Vehicles, new[] { jeep, heliT, heliA, heliK });
            AddMissing(cfg, cfg.Stations, new[] { equip, motor, helipad });
            if (cfg.FreeKit.Count == 0)
            {
                cfg.FreeKit.Add(new FreeKitEntry { Item = rifleBasic, Quantity = 1 });
                cfg.FreeKit.Add(new FreeKitEntry { Item = ammoRifle, Quantity = 60 });
            }
            foreach (var r in DefaultRewards())
                if (cfg.FindReward(r.Reason) == null) cfg.Rewards.Add(r);
                else
                {
                    var have = cfg.FindReward(r.Reason); // v1 -> v2: XP tracks are new fields
                    if (cfg.DataVersion < 2 && have.XpTrack == UnlockTrack.None) { have.XpTrack = r.XpTrack; have.Xp = r.Xp; }
                }
            if (cfg.DataVersion < 2)
            {
                // Revision 2 migration: persistent 10,000 seed replaces the 1,000 match grant.
                cfg.AccountSeedGrant = 10000;
                if (cfg.MaxBalance < 10000000) cfg.MaxBalance = 10000000;
                cfg.DataVersion = 2;
            }
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            return cfg;
        }

        static IEnumerable<RewardRule> DefaultRewards() => new[]
        {
            new RewardRule { Reason = RewardReason.Kill, Amount = 150, ZoneBonusEligible = true, CapScope = CapScope.PerVictimLife, Cap = 150, XpTrack = UnlockTrack.Assault, Xp = 10 },
            new RewardRule { Reason = RewardReason.DamageAssist, Amount = 75, ZoneBonusEligible = true, CapScope = CapScope.PerVictimLife, Cap = 75, WindowSeconds = 15f, XpTrack = UnlockTrack.Assault, Xp = 5 },
            new RewardRule { Reason = RewardReason.Revive, Amount = 150, ZoneBonusEligible = true, CapScope = CapScope.PerEvent, Cap = 150, XpTrack = UnlockTrack.Medical, Xp = 20 },
            new RewardRule { Reason = RewardReason.TeammateHeal, Amount = 1, PerUnit = true, ZoneBonusEligible = true, CapScope = CapScope.PerRecipientLife, Cap = 100, XpTrack = UnlockTrack.Medical, Xp = 1 },
            new RewardRule { Reason = RewardReason.ObjectivePresence, Amount = 20, IntervalSeconds = 10f, XpTrack = UnlockTrack.Assault, Xp = 2 },
            new RewardRule { Reason = RewardReason.PassengerTrip, Amount = 100, CapScope = CapScope.PerEvent, Cap = 100, MinDistance = 75f, MinSeconds = 10f, XpTrack = UnlockTrack.Driver, Xp = 15 },
            new RewardRule { Reason = RewardReason.FollowUpKill, Amount = 25, CapScope = CapScope.PerProviderRecipientLife, Cap = 150, SharedCap = 150, WindowSeconds = 120f },
            new RewardRule { Reason = RewardReason.Repair, Amount = 1, PerUnit = true, ZoneBonusEligible = true, CapScope = CapScope.PerAssetLife, Cap = 150, XpTrack = UnlockTrack.Support, Xp = 1 },
            new RewardRule { Reason = RewardReason.SpotAssist, Amount = 50, ZoneBonusEligible = true, WindowSeconds = 15f, XpTrack = UnlockTrack.Recon, Xp = 10 },
            new RewardRule { Reason = RewardReason.Construction, Amount = 50, ZoneBonusEligible = true, XpTrack = UnlockTrack.Support, Xp = 10 },
            new RewardRule { Reason = RewardReason.Resupply, Amount = 1, PerUnit = true, ZoneBonusEligible = true, CapScope = CapScope.PerRecipientLife, Cap = 60, XpTrack = UnlockTrack.Support, Xp = 1 },
            new RewardRule { Reason = RewardReason.VehicleDestroyed, Amount = 200, ZoneBonusEligible = true, CapScope = CapScope.PerAssetLife, Cap = 200, XpTrack = UnlockTrack.Assault, Xp = 20 },
            new RewardRule { Reason = RewardReason.BuildableDestroyed, Amount = 50, ZoneBonusEligible = true, CapScope = CapScope.PerAssetLife, Cap = 50, XpTrack = UnlockTrack.Assault, Xp = 5 },
        };

        static void AddMissing<T>(Object owner, List<T> list, IEnumerable<T> items) where T : Object
        {
            foreach (var i in items) if (i != null && !list.Contains(i)) { list.Add(i); EditorUtility.SetDirty(owner); }
        }

        static void Weapon(ItemDefinition i, float dmg, float rate, float range, int mag, float reload, string ammo)
        {
            i.BehaviorId = ItemBehaviors.HitscanWeapon; i.Damage = dmg; i.FireRate = rate; i.Range = range;
            i.MagazineSize = mag; i.ReloadTime = reload; i.AmmoItemId = ammo;
        }

        static void Heli(VehicleDefinition v, int seats, int gunners, float hp)
        {
            v.Kind = VehicleKind.Helicopter; v.Seats = seats; v.GunnerSeats = gunners; v.MaxHealth = hp;
            v.FuelCapacity = 100f; v.FuelBurnPerSecond = 0.5f; v.FuelPricePerUnit = 0.5f;
            v.FlareCount = 6; v.FlareCooldown = 8f; v.FlarePrice = 25;
        }

        static ItemDefinition Item(string id, string name, ItemCategory cat, ItemSlot slot, int price, System.Action<ItemDefinition> init) =>
            Load<ItemDefinition>($"{Folder}/Items/Item_{id}.asset", i =>
            { i.Id = id; i.DisplayName = name; i.Category = cat; i.Slot = slot; i.Price = price; init(i); });

        static VehicleDefinition Vehicle(string id, string name, int price, string behaviour, System.Action<VehicleDefinition> init) =>
            Load<VehicleDefinition>($"{Folder}/Vehicles/Vehicle_{id}.asset", v =>
            { v.Id = id; v.DisplayName = name; v.Price = price; v.BehaviorId = behaviour; init(v); });

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
                sb.AppendLine($"\nDataVersion {cfg.DataVersion} · account seed {cfg.AccountSeedGrant} · max balance {cfg.MaxBalance} · owner final death: {cfg.OwnerFinalDeath} · disconnect grace {cfg.OwnerDisconnectGrace}s\n");
                sb.AppendLine("| Item | Category | Slot | Price | Unlock | Qty/buy | Max | Behaviour | Offered |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
                foreach (var i in cfg.Items)
                    if (i != null)
                        sb.AppendLine($"| {i.Id} | {i.Category} | {i.Slot} | {(i.Purchasable ? i.Price.ToString() : "starter (trader)")} | {Unlock(i.UnlockTrack, i.UnlockXp, i.UnlockFee)} | {i.QuantityPerPurchase} | {i.MaxStack} | {i.BehaviorId} | {(EconomyConfig.IsAvailable(i) ? "yes" : "hidden (not implemented)")} |");
                foreach (var v in cfg.Vehicles)
                    if (v != null)
                        sb.AppendLine($"| {v.Id} | {v.Kind} | {v.Seats} seats/{v.GunnerSeats} gun | {v.Price} | {Unlock(v.UnlockTrack, v.UnlockXp, v.UnlockFee)} | 1 | {v.MaxAlivePerBuyer} | {v.BehaviorId} | {(EconomyConfig.IsAvailable(v) ? "yes" : "hidden (not implemented)")} |");
            }
            File.WriteAllText(ReportPath, sb.ToString());
            foreach (var err in errors) Debug.LogError("[Economy] " + err);
            Debug.Log($"[Economy] Catalog validation: {(errors.Count == 0 ? "VALID" : errors.Count + " errors")}");
            return errors.Count == 0;
        }

        static string Unlock(UnlockTrack t, int xp, int fee) => t == UnlockTrack.None ? "—" : $"{t} {xp} XP + {fee}";

        public static void BuildAndValidate()
        {
            CreateMissingDefaults();
            bool ok = ValidateAndReport();
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
