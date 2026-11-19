using System.Collections.Generic;

namespace Mudfront.Economy
{
    /// <summary>Behaviour keys and which ones have working gameplay. A product is offered only when
    /// its behaviour is in <see cref="Implemented"/>; each ECON task that finishes a behaviour (with
    /// passing acceptance tests) adds its key there.</summary>
    public static class ItemBehaviors
    {
        public const string HitscanWeapon = "weapon.hitscan";
        public const string ReserveAmmo = "ammo.reserve";
        public const string Bandage = "medical.bandage";
        public const string Parachute = "utility.parachute";
        public const string RepairTool = "tool.repair";
        public const string TransportVehicle = "vehicle.transport";

        public static readonly HashSet<string> Known = new HashSet<string>
        {
            HitscanWeapon, ReserveAmmo, Bandage, Parachute, RepairTool, TransportVehicle,
        };

        // Intentionally empty until ECON-07+ deliver working gameplay.
        static readonly HashSet<string> Implemented = new HashSet<string>();

        public static bool IsImplemented(string id) => id != null && Implemented.Contains(id);
    }
}
