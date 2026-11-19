using System.Linq;
using Mudfront.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Mudfront.Tests
{
    public class EconomyCatalogTests
    {
        static ItemDefinition Item(string id, ItemCategory cat, ItemSlot slot, int price = 10)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.Id = id; i.Category = cat; i.Slot = slot; i.Price = price; i.name = id;
            i.BehaviorId = cat == ItemCategory.Weapon ? ItemBehaviors.HitscanWeapon
                : cat == ItemCategory.Ammo ? ItemBehaviors.ReserveAmmo : ItemBehaviors.Bandage;
            if (cat == ItemCategory.Ammo) { i.QuantityPerPurchase = 30; i.MaxStack = 90; }
            if (cat == ItemCategory.Weapon) { i.Damage = 20; i.FireRate = 5; i.Range = 100; i.MagazineSize = 20; i.ReloadTime = 1; i.AmmoItemId = "ammo"; }
            if (cat == ItemCategory.Medical) { i.EffectAmount = 40; }
            return i;
        }

        static EconomyConfig ValidConfig()
        {
            var c = ScriptableObject.CreateInstance<EconomyConfig>();
            var ammo = Item("ammo", ItemCategory.Ammo, ItemSlot.AmmoReserve);
            var rifle = Item("rifle", ItemCategory.Weapon, ItemSlot.Primary, 250);
            var band = Item("bandage", ItemCategory.Medical, ItemSlot.Consumable, 50);
            c.Items.AddRange(new[] { ammo, rifle, band });
            c.FreeKit.Add(new FreeKitEntry { Item = rifle, Quantity = 1 });
            c.Rewards.Add(new RewardRule { Reason = RewardReason.Kill, Amount = 150, CapScope = CapScope.PerVictimLife, Cap = 150 });
            var st = ScriptableObject.CreateInstance<StationDefinition>();
            st.Id = "shop"; st.Kind = StationKind.Equipment; st.Items.Add(rifle);
            c.Stations.Add(st);
            return c;
        }

        static void AssertError(EconomyConfig c, string fragment)
        {
            var errors = CatalogValidator.Validate(c);
            Assert.IsTrue(errors.Any(e => e.Contains(fragment)), $"expected error containing '{fragment}', got: {string.Join(" | ", errors)}");
        }

        [Test] public void ValidConfig_HasNoErrors() => CollectionAssert.IsEmpty(CatalogValidator.Validate(ValidConfig()));

        [Test] public void DuplicateId_Fails()
        {
            var c = ValidConfig(); c.Items.Add(Item("rifle", ItemCategory.Weapon, ItemSlot.Secondary));
            AssertError(c, "Duplicate Id 'rifle'");
        }

        [Test] public void NegativePrice_Fails() { var c = ValidConfig(); c.Items[1].Price = -5; AssertError(c, "negative"); }

        [Test] public void PriceAboveMaxBalance_Fails() { var c = ValidConfig(); c.Items[1].Price = c.MaxBalance + 1; AssertError(c, "exceeds MaxBalance"); }

        [Test] public void MissingBehaviour_Fails() { var c = ValidConfig(); c.Items[2].BehaviorId = ""; AssertError(c, "missing BehaviorId"); }

        [Test] public void UnknownBehaviour_Fails() { var c = ValidConfig(); c.Items[2].BehaviorId = "magic.wand"; AssertError(c, "unknown BehaviorId"); }

        [Test] public void IncompatibleSlot_Fails() { var c = ValidConfig(); c.Items[1].Slot = ItemSlot.AmmoReserve; AssertError(c, "incompatible"); }

        [Test] public void WeaponWithoutAmmo_Fails() { var c = ValidConfig(); c.Items[1].AmmoItemId = "nope"; AssertError(c, "AmmoItemId 'nope'"); }

        [Test] public void StackOverflow_Fails() { var c = ValidConfig(); c.Items[0].QuantityPerPurchase = 500; AssertError(c, "exceeds MaxStack"); }

        [Test] public void CappedRewardWithoutCap_Fails()
        {
            var c = ValidConfig(); c.Rewards.Add(new RewardRule { Reason = RewardReason.Repair, Amount = 1, PerUnit = true, CapScope = CapScope.PerAssetLife, Cap = 0 });
            AssertError(c, "needs Cap > 0");
        }

        [Test] public void DuplicateRewardReason_Fails()
        {
            var c = ValidConfig(); c.Rewards.Add(new RewardRule { Reason = RewardReason.Kill, Amount = 10 });
            AssertError(c, "duplicate reason");
        }

        [Test] public void VehicleWithoutPrefab_IsNotAvailable()
        {
            var v = ScriptableObject.CreateInstance<VehicleDefinition>();
            v.Id = "jeep"; v.BehaviorId = ItemBehaviors.TransportVehicle;
            Assert.IsFalse(EconomyConfig.IsAvailable(v));
        }

        [Test] public void UnimplementedItem_IsHidden() => Assert.IsFalse(EconomyConfig.IsAvailable(ValidConfig().Items[2]));

        [Test] public void FreeKitOnlyItem_IsNeverAvailable()
        {
            var i = Item("free", ItemCategory.Weapon, ItemSlot.Primary, 0); i.Purchasable = false;
            Assert.IsFalse(EconomyConfig.IsAvailable(i));
        }

#if UNITY_EDITOR
        [Test] public void ShippedCatalog_IsValid_AndOffersNoUnimplementedProduct()
        {
            var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/_Mudfront/Data/Economy/EconomyConfig.asset");
            Assert.IsNotNull(cfg, "shipped EconomyConfig asset missing");
            CollectionAssert.IsEmpty(CatalogValidator.Validate(cfg));
            Assert.AreEqual(1000, cfg.MatchEntryGrant);
            foreach (var i in cfg.Items) if (EconomyConfig.IsAvailable(i)) Assert.IsTrue(ItemBehaviors.IsImplemented(i.BehaviorId));
            foreach (var v in cfg.Vehicles) if (EconomyConfig.IsAvailable(v)) Assert.IsNotNull(v.Prefab);
            var expect = new[] { ("rifle_service", 250), ("pistol", 100), ("ammo_rifle", 30), ("bandage", 50), ("parachute", 150), ("repair_tool", 150) };
            foreach (var (id, price) in expect) Assert.AreEqual(price, cfg.FindItem(id).Price, id);
            Assert.AreEqual(600, cfg.FindVehicle("jeep_transport").Price);
            Assert.AreEqual(60, cfg.FreeKit.Single(k => k.Item.Category == ItemCategory.Ammo).Quantity);
        }
#endif
    }
}
