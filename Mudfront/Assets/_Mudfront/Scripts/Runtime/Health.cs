using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    public class Health : MonoBehaviour
    {
        public float Max = 100f;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public event Action<Health> Died;
        public event Action<Health> Revived;

        void Awake() => Current = Max;

        /// <summary>Apply damage. Friendly fire (same team) is ignored.</summary>
        public void Damage(float amount, int attackerTeam = -1)
        {
            if (IsDead || amount <= 0f) return;
            var tm = GetComponent<TeamMember>();
            if (tm != null && attackerTeam >= 0 && tm.TeamId == attackerTeam) return;
            Current = Mathf.Max(0f, Current - amount);
            if (IsDead) Died?.Invoke(this);
        }

        public void ResetHealth()
        {
            Current = Max;
            Revived?.Invoke(this);
        }
    }
}
