using UnityEngine;

namespace Mudfront.Economy
{
    [CreateAssetMenu(menuName = "Mudfront/Economy/Vehicle", fileName = "Vehicle_")]
    public class VehicleDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public ClassMask AllowedClasses = ClassMask.All;
        public int Price;
        public string BehaviorId;
        [Tooltip("Required before the vehicle can become available.")]
        public GameObject Prefab;
        public float MaxHealth = 300f;
        [Tooltip("Total seats including the driver.")]
        public int Seats = 4;
        [Tooltip("Seconds an unused delivered vehicle waits before despawning (0 = never).")]
        public float AbandonDespawnTime = 120f;
        [Tooltip("Per-buyer cap on simultaneously alive purchased vehicles of this type.")]
        public int MaxAlivePerBuyer = 1;
    }
}
