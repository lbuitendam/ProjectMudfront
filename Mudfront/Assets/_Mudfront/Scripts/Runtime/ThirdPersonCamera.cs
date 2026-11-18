using UnityEngine;
using UnityEngine.Rendering;

namespace Mudfront
{
    /// <summary>
    /// Over-the-shoulder camera with aim zoom, crouch height, first-person mode and smooth wall collision.
    /// Rotation follows the mouse directly (no lag); height changes and collision push-out are smoothed.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        public ThirdPersonController Target;

        [Header("Third person")]
        public Vector3 ShoulderOffset = new Vector3(0.6f, 0f, -4f);
        public Vector3 AimOffset = new Vector3(0.75f, 0.05f, -2.0f);
        public float PivotHeight = 1.6f;
        public float CrouchPivotHeight = 1.1f;
        public float Fov = 65f;
        public float AimFov = 45f;
        [Tooltip("How fast aim zoom blends in/out (per second)")] public float AimBlendSpeed = 7f;

        [Header("First person")]
        public float EyeHeight = 1.62f;
        public float CrouchEyeHeight = 1.05f;

        [Header("Vehicle")]
        public Vector3 VehicleOffset = new Vector3(0f, 1.2f, -7.5f);
        public float VehicleFov = 70f;

        [Header("Collision & smoothing")]
        public float CollisionRadius = 0.25f;
        [Tooltip("Speed the camera moves back out after a wall (m/s); pulling in is instant")] public float PushOutSpeed = 6f;
        public float HeightSmoothTime = 0.06f;

        public float AimBlend { get; private set; }
        Camera _cam;
        float _pivotY, _pivotYVel, _dist = -1f;
        bool _hiddenForFp;

        void Awake() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (Target == null) return;
            float dt = Time.deltaTime;
            bool driving = Target.CurrentVehicle != null;
            var anchor = driving ? Target.CurrentVehicle.transform : Target.transform;
            var rot = Quaternion.Euler(Target.Pitch, Target.Yaw, 0f);
            AimBlend = Mathf.MoveTowards(AimBlend, Target.IsAiming ? 1f : 0f, AimBlendSpeed * dt);
            bool fp = Target.FirstPerson && !driving;
            SetModelHidden(fp);

            float crouch = Target.CrouchBlend;
            float h = driving ? 1.8f : fp ? Mathf.Lerp(EyeHeight, CrouchEyeHeight, crouch) : Mathf.Lerp(PivotHeight, CrouchPivotHeight, crouch);
            float targetY = anchor.position.y + h;
            _pivotY = _dist < 0f ? targetY : Mathf.SmoothDamp(_pivotY, targetY, ref _pivotYVel, HeightSmoothTime);
            var pivot = new Vector3(anchor.position.x, _pivotY, anchor.position.z);

            float fov = driving ? VehicleFov : Mathf.Lerp(Fov, AimFov, AimBlend);
            _cam.fieldOfView = fov;

            if (fp)
            {
                transform.SetPositionAndRotation(pivot, rot);
                _dist = 0f;
                return;
            }

            var local = driving ? VehicleOffset : Vector3.Lerp(ShoulderOffset, AimOffset, AimBlend);
            var offset = rot * local;
            float want = offset.magnitude;
            var dir = offset / want;
            float allowed = want;
            foreach (var hit in Physics.SphereCastAll(pivot, CollisionRadius, dir, want, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(Target.transform) || (driving && hit.transform.IsChildOf(anchor))) continue;
                if (hit.distance <= 0f) continue; // overlapping at start: ignore
                allowed = Mathf.Min(allowed, Mathf.Max(0.4f, hit.distance - 0.1f));
            }
            // pull in instantly (never clip), ease back out
            _dist = _dist < 0f || allowed < _dist ? allowed : Mathf.MoveTowards(_dist, allowed, PushOutSpeed * dt);
            transform.SetPositionAndRotation(pivot + dir * _dist, rot);
        }

        void SetModelHidden(bool hidden)
        {
            if (hidden == _hiddenForFp) return;
            _hiddenForFp = hidden;
            foreach (var r in Target.GetComponentsInChildren<Renderer>(true))
                if (!(r is LineRenderer))
                    r.shadowCastingMode = hidden ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }
    }
}
