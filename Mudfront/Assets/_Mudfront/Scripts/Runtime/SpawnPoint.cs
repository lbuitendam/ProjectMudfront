using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    public class SpawnPoint : MonoBehaviour
    {
        public static readonly List<SpawnPoint> All = new List<SpawnPoint>();
        public int TeamId;
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public static SpawnPoint Get(int team)
        {
            var list = All.FindAll(s => s.TeamId == team);
            return list.Count == 0 ? null : list[UnityEngine.Random.Range(0, list.Count)];
        }

        void OnDrawGizmos()
        {
            Gizmos.color = TeamColors.Get(TeamId);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 1f);
        }
    }
}
