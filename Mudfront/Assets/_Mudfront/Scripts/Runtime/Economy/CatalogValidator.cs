using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mudfront.Economy
{
    /// <summary>Validates an <see cref="EconomyConfig"/>. Returns human-readable errors; empty = valid.</summary>
    public static class CatalogValidator
    {
        static readonly Regex IdPattern = new Regex("^[a-z0-9][a-z0-9_.]*$");

        public static List<string> Validate(EconomyConfig c)
        {
            var e = new List<string>();
            if (c == null) { e.Add("EconomyConfig is missing."); return e; }

            if (c.MaxBalance <= 0) e.Add($"MaxBalance must be > 0 (is {c.MaxBalance}).");
            if (c.MatchEntryGrant < 0 || c.MatchEntryGrant > c.MaxBalance)
                e.Add($"MatchEntryGrant {c.MatchEntryGrant} must be within 0..MaxBalance ({c.MaxBalance}).");
            if (float.IsNaN(c.ZoneMultiplier) || float.IsInfinity(c.ZoneMultiplier) || c.ZoneMultiplier < 1f || c.ZoneMultiplier > 10f)
                e.Add($"ZoneMultiplier {c.ZoneMultiplier} must be within 1..10.");

            var ids = new HashSet<string>();
            void CheckId(string kind, string name, string id)
            {
                if (string.IsNullOrEmpty(id)) { e.Add($"{kind} '{name}' has no Id."); return; }
                if (!IdPattern.IsMatch(id)) e.Add($"{kind} '{name}' Id '{id}' must be lowercase [a-z0-9_.].");
                if (!ids.Add(id)) e.Add($"Duplicate Id '{id}' ({kind} '{name}').");
            }
            void CheckPrice(string what, int price)
            {
                if (price < 0) e.Add($"{what}: Price {price} is negative.");
                else if (price > c.MaxBalance) e.Add($"{what}: Price {price} exceeds MaxBalance {c.MaxBalance}.");
            }

            for (int n = 0; n < c.Items.Count; n++)
            {
                var i = c.Items[n];
                if (i == null) { e.Add($"Items[{n}] is null."); continue; }
                string w = $"Item '{i.Id}'";
                CheckId("Item", i.name, i.Id);
                CheckPrice(w, i.Price);
                if (i.AllowedClasses == ClassMask.None) e.Add($"{w}: no class can use it.");
                if (i.QuantityPerPurchase < 1) e.Add($"{w}: QuantityPerPurchase must be >= 1.");
                if (i.MaxStack < 1) e.Add($"{w}: MaxStack must be >= 1.");
                if (i.QuantityPerPurchase > i.MaxStack) e.Add($"{w}: QuantityPerPurchase {i.QuantityPerPurchase} exceeds MaxStack {i.MaxStack}.");
                if (string.IsNullOrEmpty(i.BehaviorId)) e.Add($"{w}: missing BehaviorId.");
                else if (!ItemBehaviors.Known.Contains(i.BehaviorId)) e.Add($"{w}: unknown BehaviorId '{i.BehaviorId}'.");
                if (!SlotFits(i.Category, i.Slot)) e.Add($"{w}: slot {i.Slot} is incompatible with category {i.Category}.");

                switch (i.Category)
                {
                    case ItemCategory.Weapon:
                        if (i.BehaviorId != ItemBehaviors.HitscanWeapon) e.Add($"{w}: weapon needs behaviour {ItemBehaviors.HitscanWeapon}.");
                        if (i.Damage <= 0 || i.FireRate <= 0 || i.Range <= 0 || i.MagazineSize < 1 || i.ReloadTime < 0)
                            e.Add($"{w}: weapon needs Damage, FireRate, Range > 0, MagazineSize >= 1, ReloadTime >= 0.");
                        var ammo = c.FindItem(i.AmmoItemId);
                        if (ammo == null) e.Add($"{w}: AmmoItemId '{i.AmmoItemId}' not found in catalog.");
                        else if (ammo.Category != ItemCategory.Ammo) e.Add($"{w}: AmmoItemId '{i.AmmoItemId}' is not an Ammo item.");
                        break;
                    case ItemCategory.Ammo:
                        if (i.BehaviorId != ItemBehaviors.ReserveAmmo) e.Add($"{w}: ammo needs behaviour {ItemBehaviors.ReserveAmmo}.");
                        break;
                    case ItemCategory.Medical:
                    case ItemCategory.Tool:
                        if (i.EffectAmount <= 0) e.Add($"{w}: EffectAmount must be > 0.");
                        if (i.UseDuration < 0) e.Add($"{w}: UseDuration must be >= 0.");
                        break;
                }
            }

            for (int n = 0; n < c.Vehicles.Count; n++)
            {
                var v = c.Vehicles[n];
                if (v == null) { e.Add($"Vehicles[{n}] is null."); continue; }
                string w = $"Vehicle '{v.Id}'";
                CheckId("Vehicle", v.name, v.Id);
                CheckPrice(w, v.Price);
                if (v.AllowedClasses == ClassMask.None) e.Add($"{w}: no class can buy it.");
                if (string.IsNullOrEmpty(v.BehaviorId) || !ItemBehaviors.Known.Contains(v.BehaviorId)) e.Add($"{w}: missing or unknown BehaviorId '{v.BehaviorId}'.");
                if (ItemBehaviors.IsImplemented(v.BehaviorId) && v.Prefab == null) e.Add($"{w}: behaviour is implemented but Prefab is missing.");
                if (v.Seats < 1) e.Add($"{w}: Seats must be >= 1.");
                if (v.MaxHealth <= 0) e.Add($"{w}: MaxHealth must be > 0.");
                if (v.MaxAlivePerBuyer < 1) e.Add($"{w}: MaxAlivePerBuyer must be >= 1.");
                if (v.AbandonDespawnTime < 0) e.Add($"{w}: AbandonDespawnTime must be >= 0.");
            }

            for (int n = 0; n < c.Stations.Count; n++)
            {
                var s = c.Stations[n];
                if (s == null) { e.Add($"Stations[{n}] is null."); continue; }
                string w = $"Station '{s.Id}'";
                CheckId("Station", s.name, s.Id);
                if (s.InteractRange <= 0) e.Add($"{w}: InteractRange must be > 0.");
                if (s.Kind == StationKind.Equipment && s.Vehicles.Count > 0) e.Add($"{w}: equipment station lists vehicles.");
                if (s.Kind == StationKind.VehicleDelivery && s.Items.Count > 0) e.Add($"{w}: vehicle station lists items.");
                foreach (var i in s.Items)
                    if (i == null || !c.Items.Contains(i)) e.Add($"{w}: item '{i?.Id}' is null or not in the catalog.");
                    else if (!i.Purchasable) e.Add($"{w}: sells non-purchasable item '{i.Id}'.");
                foreach (var v in s.Vehicles)
                    if (v == null || !c.Vehicles.Contains(v)) e.Add($"{w}: vehicle '{v?.Id}' is null or not in the catalog.");
            }

            bool kitWeapon = false;
            foreach (var k in c.FreeKit)
            {
                if (k == null || k.Item == null || !c.Items.Contains(k.Item)) { e.Add("FreeKit entry is null or not in the catalog."); continue; }
                if (k.Quantity < 1 || k.Quantity > k.Item.MaxStack) e.Add($"FreeKit '{k.Item.Id}': Quantity {k.Quantity} must be within 1..MaxStack {k.Item.MaxStack}.");
                if (k.Item.Category == ItemCategory.Weapon) kitWeapon = true;
            }
            if (!kitWeapon) e.Add("FreeKit must contain a weapon.");

            var reasons = new HashSet<RewardReason>();
            foreach (var r in c.Rewards)
            {
                if (r == null) { e.Add("Rewards contains a null entry."); continue; }
                string w = $"Reward {r.Reason}";
                if (!reasons.Add(r.Reason)) e.Add($"{w}: duplicate reason.");
                if (r.Amount < 0) e.Add($"{w}: Amount is negative.");
                if (r.Cap < 0) e.Add($"{w}: Cap is negative.");
                if (r.CapScope != CapScope.None && r.Cap == 0) e.Add($"{w}: CapScope {r.CapScope} needs Cap > 0.");
                if (r.PerUnit && r.CapScope == CapScope.None) e.Add($"{w}: per-unit rewards need a cap.");
                if (r.WindowSeconds < 0 || r.IntervalSeconds < 0 || r.MinDistance < 0 || r.MinSeconds < 0) e.Add($"{w}: timing/distance values must be >= 0.");
                double worst = (double)r.Amount * (r.ZoneBonusEligible ? c.ZoneMultiplier : 1f);
                if (worst > c.MaxBalance) e.Add($"{w}: single payout {worst} can exceed MaxBalance.");
            }
            return e;
        }

        public static bool SlotFits(ItemCategory cat, ItemSlot slot)
        {
            switch (cat)
            {
                case ItemCategory.Weapon: return slot == ItemSlot.Primary || slot == ItemSlot.Secondary;
                case ItemCategory.Ammo: return slot == ItemSlot.AmmoReserve;
                case ItemCategory.Medical: return slot == ItemSlot.Consumable;
                case ItemCategory.Utility: return slot == ItemSlot.Gadget || slot == ItemSlot.Consumable;
                case ItemCategory.Tool: return slot == ItemSlot.Gadget;
            }
            return false;
        }
    }
}
