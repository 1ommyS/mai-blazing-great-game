using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialDemo.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class DemoFirstPersonMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Optional camera transform used for pitch. If empty, this transform is used.")]
        private Transform lookTarget;

        [Header("Movement")]
        [SerializeField, Min(0.1f), Tooltip("Regular ground movement speed.")]
        private float walkSpeed = 4.2f;

        [SerializeField, Min(0.1f), Tooltip("Sprint movement speed while the sprint key is held.")]
        private float sprintSpeed = 7.4f;

        [SerializeField, Min(0.1f), Tooltip("Acceleration applied while the player changes horizontal speed.")]
        private float horizontalAcceleration = 22f;

        [SerializeField, Min(0.1f), Tooltip("Initial jump velocity applied while grounded.")]
        private float jumpVelocity = 5.8f;

        [SerializeField, Min(0f), Tooltip("Downward gravity used by the demo controller.")]
        private float gravity = 18f;

        [SerializeField, Range(0f, 25f), Tooltip("Extra camera FOV applied while sprinting to make the speed boost readable.")]
        private float sprintFovBoost = 6f;

        [SerializeField, Min(0.1f), Tooltip("How quickly the sprint FOV blends in and out.")]
        private float sprintFovBlendSpeed = 8f;

        [Header("Look")]
        [SerializeField, Min(0.01f), Tooltip("Horizontal mouse look sensitivity.")]
        private float lookSensitivityX = 0.22f;

        [SerializeField, Min(0.01f), Tooltip("Vertical mouse look sensitivity.")]
        private float lookSensitivityY = 0.2f;

        [SerializeField, Range(20f, 89f), Tooltip("Maximum vertical look angle in either direction.")]
        private float pitchClamp = 80f;

        [SerializeField, Tooltip("Locks and hides the cursor while play mode is active.")]
        private bool lockCursorOnStart = true;

        private readonly Dictionary<Object, float> _slowSources = new();
        private CharacterController _characterController;
        private Camera _playerCamera;
        private Vector3 _horizontalVelocity;
        private float _baseFieldOfView;
        private float _pitch;
        private float _verticalVelocity;

        public bool IsSprinting { get; private set; }
        public float CurrentHorizontalSpeed => new Vector2(_horizontalVelocity.x, _horizontalVelocity.z).magnitude;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (lookTarget == null)
            {
                lookTarget = transform;
            }

            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera != null)
            {
                _baseFieldOfView = _playerCamera.fieldOfView;
            }

            Vector3 euler = lookTarget.rotation.eulerAngles;
            _pitch = NormalizePitch(euler.x);
        }

        private void Start()
        {
            if (!lockCursorOnStart)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            UpdateLook();
            UpdateMove();
            UpdateSprintPresentation();
        }

        public void SetMovementSlow(Object source, float multiplier)
        {
            if (source == null)
            {
                return;
            }

            _slowSources[source] = Mathf.Clamp(multiplier, 0.1f, 1f);
        }

        public void ClearMovementSlow(Object source)
        {
            if (source == null)
            {
                return;
            }

            _slowSources.Remove(source);
        }

        private void UpdateLook()
        {
            Vector2 look = ReadLook();
            _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivityY, -pitchClamp, pitchClamp);
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + look.x * lookSensitivityX, 0f);
            lookTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateMove()
        {
            Vector2 moveInput = ReadMove();
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            float slowMultiplier = GetSlowMultiplier();
            IsSprinting = ReadSprintHeld() && moveInput.y > 0.1f && slowMultiplier > 0.75f;
            float speed = (IsSprinting ? sprintSpeed : walkSpeed) * slowMultiplier;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }

            if (_characterController.isGrounded && ReadJumpPressed())
            {
                _verticalVelocity = jumpVelocity;
            }

            _verticalVelocity -= gravity * Time.deltaTime;

            Vector3 targetHorizontalVelocity = move * speed;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetHorizontalVelocity, horizontalAcceleration * Time.deltaTime);

            Vector3 velocity = _horizontalVelocity;
            velocity.y = _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateSprintPresentation()
        {
            if (_playerCamera == null)
            {
                return;
            }

            float targetFov = _baseFieldOfView + (IsSprinting ? sprintFovBoost : 0f);
            _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, targetFov, sprintFovBlendSpeed * Time.deltaTime);
        }

        private float GetSlowMultiplier()
        {
            float multiplier = 1f;
            foreach (float sourceMultiplier in _slowSources.Values)
            {
                multiplier = Mathf.Min(multiplier, sourceMultiplier);
            }

            return multiplier;
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private static Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;
            if (keyboard.aKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed) vertical += 1f;
            return new Vector2(horizontal, vertical);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        private static Vector2 ReadLook()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        private static bool ReadSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        private static bool ReadJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private void OnGUI()
        {
            Rect rect = new(18f, Screen.height - 126f, 220f, 26f);
            string sprintState = IsSprinting ? "SPRINTING" : "SPRINT READY";
            GUI.Box(rect, $"[Shift] {sprintState}  SPD {CurrentHorizontalSpeed:0.0}");
        }
    }
}
