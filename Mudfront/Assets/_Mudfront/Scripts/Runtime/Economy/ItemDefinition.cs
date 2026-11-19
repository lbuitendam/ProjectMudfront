using UnityEngine;

namespace Mudfront.Economy
{
    /// <summary>One buyable or granted piece of equipment. Gameplay values live here, not in code.</summary>
    [CreateAssetMenu(menuName = "Mudfront/Economy/Item", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable, immutable ID used by the ledger, inventory and network. Never rename once shipped.")]
        public string Id;
        public string DisplayName;
        public ItemCategory Category;
        public ItemSlot Slot;
        public ClassMask AllowedClasses = ClassMask.All;

        [Header("Shop")]
        [Tooltip("False for free-kit-only items.")]
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

        [Header("Use (Medical / Utility / Tool)")]
        public float UseDuration;
        [Tooltip("HP healed (bandage), HP repaired per second (repair tool), deploy height (parachute).")]
        public float EffectAmount;
        public float EffectRange;
    }
}
