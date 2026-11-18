using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    public class Respawner : MonoBehaviour
    {
        public float RespawnDelay = 6f;
        public int Deaths { get; private set; }
        public bool IsWaiting { get; private set; }
        Health _health;
        TeamMember _team;

        void Awake()
        {
            _health = GetComponent<Health>();
            _team = GetComponent<TeamMember>();
            _health.Died += OnDied;
        }

        void OnDied(Health h)
        {
            Deaths++;
            var ctrl = GetComponent<ThirdPersonController>();
            if (ctrl != null && ctrl.CurrentVehicle != null) ctrl.CurrentVehicle.Exit();
            StartCoroutine(Routine());
        }

        IEnumerator Routine()
        {
            IsWaiting = true;
            SetActiveParts(false);
            yield return new WaitForSeconds(RespawnDelay);
            var sp = _team != null ? SpawnPoint.Get(_team.TeamId) : null;
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            if (sp != null) transform.SetPositionAndRotation(sp.transform.position, sp.transform.rotation);
            if (cc) cc.enabled = true;
            _health.ResetHealth();
            SetActiveParts(true);
            IsWaiting = false;
        }

        void SetActiveParts(bool on)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            foreach (var c in GetComponentsInChildren<Collider>(true)) if (!(c is CharacterController)) c.enabled = on;
            var ctrl = GetComponent<ThirdPersonController>();
            if (ctrl) ctrl.enabled = on;
            var sh = GetComponent<Shooter>();
            if (sh) sh.enabled = on;
        }
    }
}
