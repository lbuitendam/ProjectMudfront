using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    public class TeamMember : MonoBehaviour
    {
        public static readonly List<TeamMember> All = new List<TeamMember>();
        public int TeamId;
        Health _health;

        public bool IsAlive => isActiveAndEnabled && (_health == null || !_health.IsDead);

        void Awake() => _health = GetComponent<Health>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);
    }
}
