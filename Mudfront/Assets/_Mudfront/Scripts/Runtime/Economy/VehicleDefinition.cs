using UnityEngine;

namespace Mudfront.Economy
{
    [CreateAssetMenu(menuName = "Mudfront/Economy/Vehicle", fileName = "Vehicle_")]
    public class VehicleDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public VehicleKind Kind = VehicleKind.Ground;
        public int Price;
        public string BehaviorId;
        [Tooltip("Required before the vehicle can become available.")]
        public GameObject Prefab;

        [Header("Unlock (persistent, per account)")]
        public UnlockTrack UnlockTrack = UnlockTrack.None;
        public int UnlockXp;
        public int UnlockFee;

        [Header("Body")]
        public float MaxHealth = 300f;
        [Tooltip("Total seats including the driver/pilot.")]
        public int Seats = 4;
        [Tooltip("Seats with a mounted weapon (door gunner / front gunner).")]
        public int GunnerSeats;
        public VehicleLockMode InitialLock = VehicleLockMode.OwnerOnly;
        [Tooltip("Seconds an unused delivered vehicle waits before despawning (0 = never).")]
        public float AbandonDespawnTime = 120f;
        [Tooltip("Per-buyer cap on simultaneously alive purchased vehicles of this type.")]
        public int MaxAlivePerBuyer = 1;

        [Header("Fuel (0 capacity = no fuel system)")]
        public float FuelCapacity;
        public float FuelBurnPerSecond;
        [Tooltip("Credits per unit of fuel at a refuel point.")]
        public float FuelPricePerUnit;

        [Header("Mounted weapons")]
        public float GunDamage;
        public float GunFireRate;
        public int GunAmmoCapacity;
        public int RocketCount;
        public float RocketDamage;
        public float RocketBlastRadius;
        [Tooltip("Credits per mounted-gun round / per rocket when rearming.")]
        public int AmmoPricePerRound;
        public int RocketPrice;

        [Header("Countermeasures")]
        public int FlareCount;
        public float FlareCooldown;
        public int FlarePrice;
    }
}
