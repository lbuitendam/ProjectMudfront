using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    /// <summary>
    /// Control zone: a single team inside captures it over CaptureTime; more than one team = contested (frozen).
    /// The owner scores PointsPerSecond while not contested.
    /// </summary>
    public class CaptureZone : MonoBehaviour
    {
        public float Radius = 8f;
        public float Height = 6f;
        public float CaptureTime = 3f;
        public float PointsPerSecond = 1f;

        public int Owner { get; private set; } = -1;
        public int CapturingTeam { get; private set; } = -1;
        public float Progress { get; private set; }
        public bool Contested { get; private set; }
        public event Action<int> Captured;

        readonly HashSet<int> _present = new HashSet<int>();

        public bool IsInside(Vector3 p)
        {
            var d = p - transform.position;
            float dy = d.y; d.y = 0;
            return d.sqrMagnitude <= Radius * Radius && dy > -1f && dy < Height;
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>Move the zone to a new spot and make it neutral again.</summary>
        public void Relocate(Vector3 position, float radius)
        {
            transform.position = position;
            Radius = radius;
            Owner = -1; CapturingTeam = -1; Progress = 0f; Contested = false;
        }

        public void Tick(float dt)
        {
            _present.Clear();
            foreach (var m in TeamMember.All)
                if (m.IsAlive && IsInside(m.transform.position)) _present.Add(m.TeamId);

            Contested = _present.Count > 1;
            if (_present.Count == 1)
            {
                int t = -1;
                foreach (var x in _present) t = x;
                if (t != Owner)
                {
                    if (CapturingTeam != t) { CapturingTeam = t; Progress = 0f; }
                    Progress += dt / CaptureTime;
                    if (Progress >= 1f)
                    {
                        Owner = t; Progress = 0f; CapturingTeam = -1;
                        Captured?.Invoke(t);
                    }
                }
            }
            else if (_present.Count == 0 && CapturingTeam >= 0)
            {
                Progress = Mathf.Max(0f, Progress - dt / (CaptureTime * 2f));
                if (Progress <= 0f) CapturingTeam = -1;
            }

            if (Owner >= 0 && !Contested) MatchManager.AddScore(Owner, PointsPerSecond * dt);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Owner >= 0 ? TeamColors.Get(Owner) : Color.white;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
