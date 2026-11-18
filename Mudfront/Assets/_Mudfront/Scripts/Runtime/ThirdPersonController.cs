using UnityEngine;
using UnityEngine.InputSystem;

namespace Mudfront
{
    /// <summary>
    /// Shooter movement (body faces camera yaw).
    /// WASD move, Shift sprint (forward only), Ctrl/C crouch, RMB aim, Space jump, V first/third person, E vehicle.
    /// Input can be driven by code (tests/bots): set UseLocalInput = false and write the *Input fields.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        public float RunSpeed = 5.5f;
        public float SprintSpeed = 8f;
        public float CrouchSpeed = 3f;
        [Tooltip("Multiplier on speed while aiming down sights")] public float AimSpeedMultiplier = 0.6f;

        [Header("Feel")]
        [Tooltip("m/s² towards target speed on the ground")] public float Acceleration = 45f;
        [Tooltip("m/s² when releasing input on the ground")] public float Deceleration = 60f;
        [Tooltip("Fraction of ground acceleration available in the air")] public float AirControl = 0.3f;
        public float JumpHeight = 1.2f;
        public float Gravity = -20f;
        [Tooltip("Jump still allowed this long after leaving a ledge")] public float CoyoteTime = 0.12f;
        [Tooltip("A jump pressed this long before landing still fires")] public float JumpBuffer = 0.12f;

        [Header("Crouch")]
        public float StandHeight = 1.8f;
        public float CrouchHeight = 1.2f;
        public float CrouchTransitionSpeed = 8f;

        [Header("Look")]
        public float MouseSensitivity = 0.12f;
        public float AimSensitivityMultiplier = 0.6f;
        public float InteractRange = 3.5f;
        public bool UseLocalInput = true;

        [HideInInspector] public Vector2 MoveInput;
        [HideInInspector] public bool SprintInput;
        [HideInInspector] public bool JumpInput;
        [HideInInspector] public bool CrouchInput;
        [HideInInspector] public bool AimInput;
        public float Yaw;
        public float Pitch = 10f;
        public bool FirstPerson;

        public Vehicle CurrentVehicle { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsAiming => AimInput && CurrentVehicle == null;
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => _cc != null && _cc.isGrounded;
        /// <summary>Current horizontal speed in m/s.</summary>
        public float Speed => new Vector2(_velXZ.x, _velXZ.z).magnitude;
        /// <summary>0 = standing, 1 = fully crouched (for camera height).</summary>
        public float CrouchBlend => Mathf.InverseLerp(StandHeight, CrouchHeight, _cc != null ? _cc.height : StandHeight);

        CharacterController _cc;
        Vector3 _velXZ;
        float _vy;
        float _lastGroundedTime = -10f, _jumpPressedTime = -10f;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // default 0.001 m drops every step at very high frame rates (seen at ~10k fps in batch tests)
            _cc.minMoveDistance = 0f;
            StandHeight = _cc.height;
            Yaw = transform.eulerAngles.y;
        }

        void Update()
        {
            if (UseLocalInput) ReadInput();

            if (CurrentVehicle != null)
            {
                CurrentVehicle.Throttle = MoveInput.y;
                CurrentVehicle.Steer = MoveInput.x;
                return;
            }
            Move(Time.deltaTime);
        }

        void ReadInput()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;
            MoveInput = new Vector2(
                (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0),
                (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            SprintInput = kb.leftShiftKey.isPressed;
            CrouchInput = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
            if (kb.spaceKey.wasPressedThisFrame) JumpInput = true;
            if (kb.vKey.wasPressedThisFrame) FirstPerson = !FirstPerson;

            if (mouse != null)
            {
                AimInput = mouse.rightButton.isPressed;
                if (mouse.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
                if (kb.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var d = mouse.delta.ReadValue() * MouseSensitivity * (IsAiming ? AimSensitivityMultiplier : 1f);
                    Yaw += d.x;
                    Pitch = Mathf.Clamp(Pitch - d.y, -40f, 70f);
                }
            }
            if (kb.eKey.wasPressedThisFrame) Interact();
        }

        public void Move(float dt)
        {
            if (!_cc.enabled) return;
            float now = Time.time;
            var yawRot = Quaternion.Euler(0f, Yaw, 0f);
            transform.rotation = yawRot;

            // crouch (stays crouched while there is no headroom)
            UpdateCrouch(dt);

            // target horizontal velocity
            var input = Vector2.ClampMagnitude(MoveInput, 1f);
            IsSprinting = SprintInput && input.y > 0.1f && !IsCrouching && !IsAiming;
            float speed = IsCrouching ? CrouchSpeed : IsSprinting ? SprintSpeed : RunSpeed;
            if (IsAiming) speed *= AimSpeedMultiplier;
            var target = yawRot * new Vector3(input.x, 0f, input.y) * speed;

            bool grounded = _cc.isGrounded;
            if (grounded) _lastGroundedTime = now;
            float rate = input.sqrMagnitude > 0.01f ? Acceleration : Deceleration;
            if (!grounded) rate = Acceleration * AirControl;
            _velXZ = Vector3.MoveTowards(_velXZ, target, rate * dt);

            // jump with coyote time + input buffer
            if (JumpInput) { _jumpPressedTime = now; JumpInput = false; }
            bool canJump = now - _lastGroundedTime <= CoyoteTime && !IsCrouching;
            if (canJump && now - _jumpPressedTime <= JumpBuffer && _vy <= 0f)
            {
                _vy = Mathf.Sqrt(2f * JumpHeight * -Gravity);
                _jumpPressedTime = -10f;
                _lastGroundedTime = -10f;
            }
            else if (grounded && _vy < 0f) _vy = -2f;

            _vy += Gravity * dt;
            var flags = _cc.Move((_velXZ + Vector3.up * _vy) * dt);
            if ((flags & CollisionFlags.Above) != 0 && _vy > 0f) _vy = 0f;
        }

        void UpdateCrouch(float dt)
        {
            bool want = CrouchInput;
            if (!want && IsCrouching && !HasHeadroom()) want = true;
            IsCrouching = want;
            float targetH = want ? CrouchHeight : StandHeight;
            float h = Mathf.MoveTowards(_cc.height, targetH, CrouchTransitionSpeed * dt);
            _cc.height = h;
            _cc.center = new Vector3(0f, h / 2f, 0f);
        }

        /// <summary>True when a standing capsule fits at the current position.</summary>
        public bool HasHeadroom()
        {
            float r = _cc.radius * 0.95f;
            var bottom = transform.position + Vector3.up * (r + 0.05f);
            var top = transform.position + Vector3.up * (StandHeight - r);
            foreach (var c in Physics.OverlapCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(transform)) return false;
            return true;
        }

        /// <summary>E: exit current vehicle, or enter the nearest free one in range.</summary>
        public void Interact()
        {
            if (CurrentVehicle != null) { CurrentVehicle.Exit(); return; }
            Vehicle best = null;
            float bestD = InteractRange;
            foreach (var v in Vehicle.All)
            {
                float d = Vector3.Distance(v.transform.position, transform.position);
                if (d < bestD + v.EnterRadiusBonus && !v.IsDestroyed && v.Driver == null) { best = v; bestD = d; }
            }
            if (best != null) best.TryEnter(this);
        }

        internal void EnterVehicle(Vehicle v)
        {
            CurrentVehicle = v;
            _cc.enabled = false;
            _velXZ = Vector3.zero; _vy = 0f;
            transform.SetParent(v.Seat != null ? v.Seat : v.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        internal void ExitVehicle(Vector3 position)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Yaw, 0f));
            _cc.enabled = true;
            CurrentVehicle = null;
            MoveInput = Vector2.zero;
        }
    }
}
