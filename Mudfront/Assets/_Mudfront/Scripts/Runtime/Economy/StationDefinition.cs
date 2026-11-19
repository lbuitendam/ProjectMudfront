using System.Collections.Generic;
using UnityEngine;

namespace Mudfront.Economy
{
    /// <summary>What a physical shop sells. Scene stations reference one of these.</summary>
    [CreateAssetMenu(menuName = "Mudfront/Economy/Station", fileName = "Station_")]
    public class StationDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public StationKind Kind;
        public float InteractRange = 3f;
        public List<ItemDefinition> Items = new List<ItemDefinition>();
        public List<VehicleDefinition> Vehicles = new List<VehicleDefinition>();
    }
}
