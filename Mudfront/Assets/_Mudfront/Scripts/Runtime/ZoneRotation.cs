using System;
using UnityEngine;

namespace Mudfront
{
    /// <summary>Moving control zone: every Interval seconds the zone jumps to another location (announced WarningTime ahead).</summary>
    [RequireComponent(typeof(CaptureZone))]
    public class ZoneRotation : MonoBehaviour
    {
        [Serializable]
        public struct ZoneSpot
        {
            public string Name;
            public Vector3 Position;
            public float Radius;
        }

        public ZoneSpot[] Spots;
        public float Interval = 150f;
        public float WarningTime = 30f;

        public int CurrentIndex { get; private set; } = -1;
        public int NextIndex { get; private set; } = -1;
        public float TimeToMove => _nextMove - Time.time;
        public string CurrentName => CurrentIndex >= 0 ? Spots[CurrentIndex].Name : "";
        public string NextName => NextIndex >= 0 ? Spots[NextIndex].Name : "";

        CaptureZone _zone;
        float _nextMove;

        void Start()
        {
            _zone = GetComponent<CaptureZone>();
            if (Spots == null || Spots.Length == 0) return;
            MoveTo(UnityEngine.Random.Range(0, Spots.Length));
        }

        void Update()
        {
            if (Spots == null || Spots.Length < 2) return;
            if (NextIndex < 0 && TimeToMove <= WarningTime) NextIndex = PickNext();
            if (TimeToMove <= 0f) MoveTo(NextIndex >= 0 ? NextIndex : PickNext());
        }

        int PickNext()
        {
            int i;
            do i = UnityEngine.Random.Range(0, Spots.Length); while (i == CurrentIndex);
            return i;
        }

        public void MoveTo(int index)
        {
            CurrentIndex = index;
            NextIndex = -1;
            _zone.Relocate(Spots[index].Position, Spots[index].Radius);
            _nextMove = Time.time + Interval;
        }
    }
}
