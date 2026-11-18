using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    /// <summary>Arcade single-driver vehicle. Destroyed vehicles respawn at their start pose.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Vehicle : MonoBehaviour
    {
        public static readonly List<Vehicle> All = new List<Vehicle>();

        public float Acceleration = 14f;
        public float MaxSpeed = 18f;
        public float TurnRate = 75f;
        public float Grip = 0.25f;
        public float RespawnDelay = 8f;
        public float EnterRadiusBonus = 1.5f;
        public Transform Seat;

        [HideInInspector] public float Throttle;
        [HideInInspector] public float Steer;

        public ThirdPersonController Driver { get; private set; }
        public bool IsDestroyed => _health != null && _health.IsDead;
        public int Respawns { get; private set; }
        public float ForwardSpeed => Vector3.Dot(_rb.linearVelocity, transform.forward);

        Rigidbody _rb;
        Health _health;
        Vector3 _spawnPos;
        Quaternion _spawnRot;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _health = GetComponent<Health>();
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
            if (_health != null) _health.Died += _ => StartCoroutine(DestroyedRoutine());
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public bool TryEnter(ThirdPersonController p)
        {
            if (Driver != null || IsDestroyed || p == null) return false;
            Driver = p;
            p.EnterVehicle(this);
            return true;
        }

        public void Exit()
        {
            if (Driver == null) return;
            var p = Driver;
            Driver = null;
            Throttle = Steer = 0f;
            p.ExitVehicle(transform.position - transform.right * 2.4f + Vector3.up * 0.3f);
        }

        void FixedUpdate()
        {
            if (IsDestroyed) return;
            float dt = Time.fixedDeltaTime;
            bool grounded = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
            if (!grounded) return;

            var fwd = transform.forward;
            float speed = ForwardSpeed;
            if (Driver != null)
            {
                if (Mathf.Abs(speed) < MaxSpeed || Mathf.Sign(speed) != Mathf.Sign(Throttle))
                    _rb.AddForce(fwd * Throttle * Acceleration, ForceMode.Acceleration);
                float turn = Steer * TurnRate * dt * Mathf.Clamp(speed / 4f, -1f, 1f);
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turn, 0f));
            }
            // kill sideways slide (arcade grip) and add rolling drag
            float lateral = Vector3.Dot(_rb.linearVelocity, transform.right);
            _rb.AddForce(-transform.right * lateral * Grip, ForceMode.VelocityChange);
            if (Driver == null || Mathf.Abs(Throttle) < 0.05f)
                _rb.AddForce(-fwd * speed * 0.8f * dt, ForceMode.VelocityChange);
        }

        IEnumerator DestroyedRoutine()
        {
            Exit();
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            _rb.isKinematic = true;
            yield return new WaitForSeconds(RespawnDelay);
            transform.SetPositionAndRotation(_spawnPos, _spawnRot);
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
            _health.ResetHealth();
            Respawns++;
        }
    }
}
