using System.Collections.Generic;
using UnityEngine;

namespace Mudfront.Economy
{
    /// <summary>One buyable or trader-claimed piece of equipment. Gameplay values live here, not in code.
    /// Anyone may buy any item once its unlock requirement is met (revision 2: no class gates).</summary>
    [CreateAssetMenu(menuName = "Mudfront/Economy/Item", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable, immutable ID used by the ledger, inventory and network. Never rename once shipped.")]
        public string Id;
        public string DisplayName;
        public ItemCategory Category;
        public ItemSlot Slot;

        [Header("Unlock (persistent, per account)")]
        public UnlockTrack UnlockTrack = UnlockTrack.None;
        [Tooltip("Role XP on UnlockTrack required before the unlock can be bought.")]
        public int UnlockXp;
        [Tooltip("One-time cash fee to unlock. 0 with track None = available immediately.")]
        public int UnlockFee;

        [Header("Shop")]
        [Tooltip("False for trader-claimed starter items.")]
        public bool Purchasable = true;
        public int Price;
        [Tooltip("Units granted per purchase (rounds for ammo, 1 for weapons).")]
        public int QuantityPerPurchase = 1;
        [Tooltip("Max units carried in this slot or reserve pool.")]
        public int MaxStack = 1;

        [Header("Behaviour")]
        [Tooltip("Gameplay behaviour key. Items whose behaviour is not implemented stay hidden.")]
        public string BehaviorId;
        public GameObject Prefab;

        [Header("Weapon (Category = Weapon)")]
        public float Damage;
        public float FireRate;
        public float Range;
        public int MagazineSize;
        public float ReloadTime;
        [Tooltip("ID of the ammo item this weapon consumes.")]
        public string AmmoItemId;
        [Tooltip("Hip-fire spread in degrees.")]
        public float Spread = 1f;

        [Header("Use (Medical / Utility / Tool)")]
        public float UseDuration;
        [Tooltip("HP healed (bandage/medkit), HP repaired per second (repair tool), deploy height (parachute).")]
        public float EffectAmount;
        public float EffectRange;
        [Tooltip("Large medical kits: uses before the kit is spent. Revive-capable kits shorten revive time.")]
        public int Charges = 1;
        [Tooltip("Multiplier on revive duration while this item is carried (1 = no effect).")]
        public float ReviveTimeMultiplier = 1f;

        [Header("Armour (Category = Armour)")]
        [Range(0f, 0.9f)] public float DamageReduction;
        [Tooltip("Damage the armour absorbs before it breaks.")]
        public float Durability;

        [Header("Grenade (Category = Grenade)")]
        public float FuseTime;
        public float BlastRadius;
        [Tooltip("Frag: max damage at centre. Smoke: unused.")]
        public float BlastDamage;
        [Tooltip("Smoke: seconds the cloud lasts.")]
        public float EffectDuration;

        [Header("Attachment (Category = Attachment)")]
        [Tooltip("Weapon item IDs this attachment fits.")]
        public List<string> CompatibleWeaponIds = new List<string>();
        [Tooltip("Multiplier on weapon spread (scope/grip < 1).")]
        public float SpreadMultiplier = 1f;
        [Tooltip("Aim zoom factor (1 = none).")]
        public float ZoomMultiplier = 1f;
        [Tooltip("Extra rounds per magazine (extended mag).")]
        public int MagazineBonus;
    }
}
