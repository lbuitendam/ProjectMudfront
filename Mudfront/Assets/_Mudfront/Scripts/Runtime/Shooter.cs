using UnityEngine;
using UnityEngine.InputSystem;

namespace Mudfront
{
    /// <summary>Basic hitscan rifle: LMB fire, R reload. Aims through the screen centre.</summary>
    public class Shooter : MonoBehaviour
    {
        public float Damage = 25f;
        public float FireRate = 8f;
        public float Range = 150f;
        public int MagazineSize = 30;
        public float ReloadTime = 1.5f;
        public bool UseLocalInput = true;

        public int Ammo { get; private set; }
        public bool Reloading => _reloadEnd > Time.time;
        public int Hits { get; private set; }

        float _nextShot, _reloadEnd;
        TeamMember _team;
        ThirdPersonController _ctrl;
        LineRenderer _tracer;
        float _tracerOff;

        void Awake()
        {
            _team = GetComponent<TeamMember>();
            _ctrl = GetComponent<ThirdPersonController>();
            Ammo = MagazineSize;
            _tracer = new GameObject("Tracer").AddComponent<LineRenderer>();
            _tracer.transform.SetParent(transform, false);
            _tracer.widthMultiplier = 0.04f;
            _tracer.material = new Material(Shader.Find("Sprites/Default"));
            _tracer.startColor = _tracer.endColor = new Color(1f, 0.85f, 0.4f);
            _tracer.enabled = false;
        }

        void Update()
        {
            if (_tracer.enabled && Time.time > _tracerOff) _tracer.enabled = false;
            if (Reloading) return;
            if (Ammo == 0 && _reloadEnd > 0 && _reloadEnd <= Time.time) { Ammo = MagazineSize; _reloadEnd = 0; }
            if (!UseLocalInput || (_ctrl != null && _ctrl.CurrentVehicle != null)) return;
            var mouse = Mouse.current; var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) StartReload();
            if (mouse != null && mouse.leftButton.isPressed && Cursor.lockState == CursorLockMode.Locked)
            {
                var cam = Camera.main;
                if (cam != null) TryFire(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f)));
            }
        }

        public void StartReload()
        {
            if (Reloading || Ammo == MagazineSize) return;
            Ammo = 0;
            _reloadEnd = Time.time + ReloadTime;
        }

        /// <summary>Fire one shot along a ray. Returns the Health hit (or null).</summary>
        public Health TryFire(Ray ray)
        {
            if (Time.time < _nextShot || Reloading || Ammo <= 0) return null;
            _nextShot = Time.time + 1f / FireRate;
            Ammo--;
            if (Ammo == 0) StartReload();

            Health target = null;
            Vector3 end = ray.origin + ray.direction * Range;
            var hits = Physics.RaycastAll(ray, Range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(transform)) continue;
                end = h.point;
                target = h.collider.GetComponentInParent<Health>();
                break;
            }
            if (target != null)
            {
                target.Damage(Damage, _team != null ? _team.TeamId : -1);
                Hits++;
            }
            _tracer.SetPosition(0, transform.position + transform.forward * 0.6f + Vector3.up * 1.2f);
            _tracer.SetPosition(1, end);
            _tracer.enabled = true;
            _tracerOff = Time.time + 0.04f;
            return target;
        }
    }
}
