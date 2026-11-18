using UnityEngine;
using UnityEngine.InputSystem;

namespace Mudfront
{
    /// <summary>
    /// Third-person shooter movement (body faces camera yaw). WASD, Shift sprint, Space jump, E enter/exit vehicle.
    /// Input can be driven by code (tests/bots) by setting UseLocalInput = false and writing the *Input fields.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        public float RunSpeed = 5.5f;
        public float SprintSpeed = 8f;
        public float JumpHeight = 1.2f;
        public float Gravity = -20f;
        public float MouseSensitivity = 0.12f;
        public float InteractRange = 3.5f;
        public bool UseLocalInput = true;

        [HideInInspector] public Vector2 MoveInput;
        [HideInInspector] public bool SprintInput;
        [HideInInspector] public bool JumpInput;
        public float Yaw;
        public float Pitch = 10f;

        public Vehicle CurrentVehicle { get; private set; }
        CharacterController _cc;
        float _vy;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // default 0.001 m drops every step at very high frame rates (seen at ~10k fps in batch tests)
            _cc.minMoveDistance = 0f;
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
            JumpInput = kb.spaceKey.wasPressedThisFrame;

            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
                if (kb.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var d = mouse.delta.ReadValue() * MouseSensitivity;
                    Yaw += d.x;
                    Pitch = Mathf.Clamp(Pitch - d.y, -40f, 70f);
                }
            }
            if (kb.eKey.wasPressedThisFrame) Interact();
        }

        public void Move(float dt)
        {
            if (!_cc.enabled) return;
            var yawRot = Quaternion.Euler(0f, Yaw, 0f);
            transform.rotation = yawRot;
            var input = Vector2.ClampMagnitude(MoveInput, 1f);
            var dir = yawRot * new Vector3(input.x, 0f, input.y);
            float speed = SprintInput && input.y > 0.1f ? SprintSpeed : RunSpeed;

            if (_cc.isGrounded)
            {
                _vy = -2f;
                if (JumpInput) _vy = Mathf.Sqrt(2f * JumpHeight * -Gravity);
            }
            JumpInput = false;
            _vy += Gravity * dt;
            _cc.Move((dir * speed + Vector3.up * _vy) * dt);
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
