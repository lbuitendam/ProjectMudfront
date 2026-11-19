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

            if (c.DataVersion != EconomyConfig.CurrentDataVersion)
                e.Add($"DataVersion {c.DataVersion} is outdated (current {EconomyConfig.CurrentDataVersion}); run the catalog migration.");
            if (c.MaxBalance <= 0) e.Add($"MaxBalance must be > 0 (is {c.MaxBalance}).");
            if (c.AccountSeedGrant < 0 || c.AccountSeedGrant > c.MaxBalance)
                e.Add($"AccountSeedGrant {c.AccountSeedGrant} must be within 0..MaxBalance ({c.MaxBalance}).");
            if (float.IsNaN(c.ZoneMultiplier) || float.IsInfinity(c.ZoneMultiplier) || c.ZoneMultiplier < 1f || c.ZoneMultiplier > 10f)
                e.Add($"ZoneMultiplier {c.ZoneMultiplier} must be within 1..10.");
            if (c.OwnerDisconnectGrace < 0) e.Add("OwnerDisconnectGrace must be >= 0.");
            if (c.BasicReviveSeconds <= 0) e.Add("BasicReviveSeconds must be > 0.");

            var ids = new HashSet<string>();
            void CheckId(string kind, string name, string id)
            {
                if (string.IsNullOrEmpty(id)) { e.Add($"{kind} '{name}' has no Id."); return; }
                if (!IdPattern.IsMatch(id)) e.Add($"{kind} '{name}' Id '{id}' must be lowercase [a-z0-9_.].");
                if (!ids.Add(id)) e.Add($"Duplicate Id '{id}' ({kind} '{name}').");
            }
            void CheckMoney(string what, string field, long v)
            {
                if (v < 0) e.Add($"{what}: {field} {v} is negative.");
                else if (v > c.MaxBalance) e.Add($"{what}: {field} {v} exceeds MaxBalance {c.MaxBalance}.");
            }
            void CheckUnlock(string what, UnlockTrack t, int xp, int fee)
            {
                CheckMoney(what, "UnlockFee", fee);
                if (xp < 0) e.Add($"{what}: UnlockXp is negative.");
                if (t == UnlockTrack.None && (xp > 0 || fee > 0)) e.Add($"{what}: unlock XP/fee set but UnlockTrack is None.");
            }

            for (int n = 0; n < c.Items.Count; n++)
            {
                var i = c.Items[n];
                if (i == null) { e.Add($"Items[{n}] is null."); continue; }
                string w = $"Item '{i.Id}'";
                CheckId("Item", i.name, i.Id);
                CheckMoney(w, "Price", i.Price);
                CheckUnlock(w, i.UnlockTrack, i.UnlockXp, i.UnlockFee);
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
                        if (i.Damage <= 0 || i.FireRate <= 0 || i.Range <= 0 || i.MagazineSize < 1 || i.ReloadTime < 0 || i.Spread < 0)
                            e.Add($"{w}: weapon needs Damage, FireRate, Range > 0, MagazineSize >= 1, ReloadTime and Spread >= 0.");
                        var ammo = c.FindItem(i.AmmoItemId);
                        if (ammo == null) e.Add($"{w}: AmmoItemId '{i.AmmoItemId}' not found in catalog.");
                        else if (ammo.Category != ItemCategory.Ammo) e.Add($"{w}: AmmoItemId '{i.AmmoItemId}' is not an Ammo item.");
                        break;
                    case ItemCategory.Ammo:
                        if (i.BehaviorId != ItemBehaviors.ReserveAmmo) e.Add($"{w}: ammo needs behaviour {ItemBehaviors.ReserveAmmo}.");
                        break;
                    case ItemCategory.Medical:
                    case ItemCategory.Tool:
                        if (i.EffectAmount <= 0 && i.BehaviorId != ItemBehaviors.BuildTool) e.Add($"{w}: EffectAmount must be > 0.");
                        if (i.UseDuration < 0) e.Add($"{w}: UseDuration must be >= 0.");
                        if (i.Charges < 1) e.Add($"{w}: Charges must be >= 1.");
                        if (i.ReviveTimeMultiplier <= 0 || i.ReviveTimeMultiplier > 1) e.Add($"{w}: ReviveTimeMultiplier must be within (0, 1].");
                        break;
                    case ItemCategory.Armour:
                        if (i.BehaviorId != ItemBehaviors.Armour) e.Add($"{w}: armour needs behaviour {ItemBehaviors.Armour}.");
                        if (i.DamageReduction <= 0 || i.DamageReduction > 0.9f || i.Durability <= 0) e.Add($"{w}: armour needs DamageReduction in (0, 0.9] and Durability > 0.");
                        break;
                    case ItemCategory.Grenade:
                        if (i.BehaviorId != ItemBehaviors.FragGrenade && i.BehaviorId != ItemBehaviors.SmokeGrenade) e.Add($"{w}: grenade needs a grenade behaviour.");
                        if (i.FuseTime < 0 || i.BlastRadius <= 0) e.Add($"{w}: grenade needs FuseTime >= 0 and BlastRadius > 0.");
                        if (i.BehaviorId == ItemBehaviors.FragGrenade && i.BlastDamage <= 0) e.Add($"{w}: frag grenade needs BlastDamage > 0.");
                        if (i.BehaviorId == ItemBehaviors.SmokeGrenade && i.EffectDuration <= 0) e.Add($"{w}: smoke grenade needs EffectDuration > 0.");
                        break;
                    case ItemCategory.Attachment:
                        if (i.BehaviorId != ItemBehaviors.Attachment) e.Add($"{w}: attachment needs behaviour {ItemBehaviors.Attachment}.");
                        if (i.CompatibleWeaponIds.Count == 0) e.Add($"{w}: attachment fits no weapon.");
                        foreach (var wid in i.CompatibleWeaponIds)
                        {
                            var wpn = c.FindItem(wid);
                            if (wpn == null || wpn.Category != ItemCategory.Weapon) e.Add($"{w}: compatible weapon '{wid}' is not a weapon in the catalog.");
                        }
                        if (i.SpreadMultiplier <= 0 || i.ZoomMultiplier < 1 || i.MagazineBonus < 0) e.Add($"{w}: needs SpreadMultiplier > 0, ZoomMultiplier >= 1, MagazineBonus >= 0.");
                        if (i.SpreadMultiplier == 1 && i.ZoomMultiplier == 1 && i.MagazineBonus == 0) e.Add($"{w}: attachment has no effect.");
                        break;
                }
            }

            for (int n = 0; n < c.Vehicles.Count; n++)
            {
                var v = c.Vehicles[n];
                if (v == null) { e.Add($"Vehicles[{n}] is null."); continue; }
                string w = $"Vehicle '{v.Id}'";
                CheckId("Vehicle", v.name, v.Id);
                CheckMoney(w, "Price", v.Price);
                CheckUnlock(w, v.UnlockTrack, v.UnlockXp, v.UnlockFee);
                CheckMoney(w, "AmmoPricePerRound", v.AmmoPricePerRound);
                CheckMoney(w, "RocketPrice", v.RocketPrice);
                CheckMoney(w, "FlarePrice", v.FlarePrice);
                if (v.FuelPricePerUnit < 0) e.Add($"{w}: FuelPricePerUnit is negative.");
                if (string.IsNullOrEmpty(v.BehaviorId) || !ItemBehaviors.Known.Contains(v.BehaviorId)) e.Add($"{w}: missing or unknown BehaviorId '{v.BehaviorId}'.");
                if (ItemBehaviors.IsImplemented(v.BehaviorId) && v.Prefab == null) e.Add($"{w}: behaviour is implemented but Prefab is missing.");
                if (v.Seats < 1) e.Add($"{w}: Seats must be >= 1.");
                if (v.GunnerSeats < 0 || v.GunnerSeats >= v.Seats) e.Add($"{w}: GunnerSeats must be within 0..Seats-1.");
                if (v.MaxHealth <= 0) e.Add($"{w}: MaxHealth must be > 0.");
                if (v.MaxAlivePerBuyer < 1) e.Add($"{w}: MaxAlivePerBuyer must be >= 1.");
                if (v.AbandonDespawnTime < 0) e.Add($"{w}: AbandonDespawnTime must be >= 0.");
                if (v.FlareCount < 0 || v.FlareCooldown < 0 || v.RocketCount < 0 || v.GunAmmoCapacity < 0) e.Add($"{w}: weapon/flare counts must be >= 0.");

                bool heli = v.BehaviorId == ItemBehaviors.TransportHelicopter || v.BehaviorId == ItemBehaviors.ArmedHelicopter || v.BehaviorId == ItemBehaviors.AttackHelicopter;
                if (heli != (v.Kind == VehicleKind.Helicopter)) e.Add($"{w}: Kind {v.Kind} does not match behaviour {v.BehaviorId}.");
                if (heli)
                {
                    if (v.FuelCapacity <= 0 || v.FuelBurnPerSecond <= 0) e.Add($"{w}: helicopter needs FuelCapacity and FuelBurnPerSecond > 0.");
                    if (v.FlareCount < 1 || v.FlareCooldown <= 0) e.Add($"{w}: helicopter needs FlareCount >= 1 and FlareCooldown > 0.");
                }
                if (v.BehaviorId == ItemBehaviors.ArmedHelicopter || v.BehaviorId == ItemBehaviors.AttackHelicopter)
                    if (v.GunnerSeats < 1 || v.GunDamage <= 0 || v.GunFireRate <= 0 || v.GunAmmoCapacity < 1) e.Add($"{w}: armed aircraft needs a gunner seat and gun damage/rate/ammo.");
                if (v.BehaviorId == ItemBehaviors.AttackHelicopter && (v.RocketCount < 1 || v.RocketDamage <= 0 || v.RocketBlastRadius <= 0))
                    e.Add($"{w}: attack helicopter needs rockets with damage and blast radius.");
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
                if (k.Item.UnlockTrack != UnlockTrack.None) e.Add($"FreeKit '{k.Item.Id}': starter items must not require an unlock.");
                if (k.Item.Category == ItemCategory.Weapon) kitWeapon = true;
            }
            if (!kitWeapon) e.Add("FreeKit must contain a weapon.");

            var reasons = new HashSet<RewardReason>();
            foreach (var r in c.Rewards)
            {
                if (r == null) { e.Add("Rewards contains a null entry."); continue; }
                string w = $"Reward {r.Reason}";
                if (r.Reason == RewardReason.AccountSeed) e.Add($"{w}: the account seed is AccountSeedGrant, not a reward rule.");
                if (!reasons.Add(r.Reason)) e.Add($"{w}: duplicate reason.");
                if (r.Amount < 0) e.Add($"{w}: Amount is negative.");
                if (r.Cap < 0 || r.SharedCap < 0) e.Add($"{w}: Cap/SharedCap is negative.");
                if (r.CapScope != CapScope.None && r.Cap == 0) e.Add($"{w}: CapScope {r.CapScope} needs Cap > 0.");
                if (r.PerUnit && r.CapScope == CapScope.None) e.Add($"{w}: per-unit rewards need a cap.");
                if (r.WindowSeconds < 0 || r.IntervalSeconds < 0 || r.MinDistance < 0 || r.MinSeconds < 0) e.Add($"{w}: timing/distance values must be >= 0.");
                if (r.Xp < 0) e.Add($"{w}: Xp is negative.");
                if (r.Xp > 0 && r.XpTrack == UnlockTrack.None) e.Add($"{w}: Xp set without an XpTrack.");
                if (r.Reason == RewardReason.FollowUpKill &&
                    (r.WindowSeconds <= 0 || r.CapScope != CapScope.PerProviderRecipientLife || r.SharedCap <= 0))
                    e.Add($"{w}: follow-up kills need WindowSeconds > 0, CapScope PerProviderRecipientLife and SharedCap > 0.");
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
                case ItemCategory.Medical: return slot == ItemSlot.Consumable || slot == ItemSlot.Gadget;
                case ItemCategory.Utility: return slot == ItemSlot.Gadget || slot == ItemSlot.Consumable;
                case ItemCategory.Tool: return slot == ItemSlot.Gadget;
                case ItemCategory.Armour: return slot == ItemSlot.Armour;
                case ItemCategory.Grenade: return slot == ItemSlot.Grenade;
                case ItemCategory.Attachment: return slot == ItemSlot.Attachment;
            }
            return false;
        }
    }
}
