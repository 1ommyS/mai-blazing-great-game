using IndustrialDemo.Combat;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialDemo.Player
{
    public class ViewModelPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Weapon controller used to drive recoil events.")]
        private WeaponFireController weaponFireController;

        [SerializeField, Tooltip("Main weapon transform under the viewmodel root.")]
        private Transform weaponRoot;

        [Header("Base Pose")]
        [SerializeField]
        private Vector3 rootLocalPosition = new(0.22f, -0.28f, 0.62f);

        [SerializeField]
        private Vector3 rootLocalEuler = new(2f, -4f, 0f);

        [SerializeField]
        private Vector3 weaponLocalPosition = new(0.18f, -0.03f, 0.12f);

        [SerializeField]
        private Vector3 weaponLocalEuler = new(-2f, 180f, 0f);

        [SerializeField]
        private Vector3 weaponLocalScale = new(0.56f, 0.56f, 0.56f);

        [Header("Motion")]
        [SerializeField, Range(0f, 0.08f)]
        private float swayPositionAmount = 0.018f;

        [SerializeField, Range(0f, 8f)]
        private float swayRotationAmount = 3.8f;

        [SerializeField, Range(1f, 20f)]
        private float swaySharpness = 10f;

        [SerializeField, Range(0f, 0.08f)]
        private float moveBobAmount = 0.012f;

        [SerializeField, Range(1f, 20f)]
        private float moveBobFrequency = 7.5f;

        [Header("Recoil")]
        [SerializeField]
        private Vector3 recoilKickPosition = new(0f, 0.012f, -0.08f);

        [SerializeField]
        private Vector3 recoilKickEuler = new(-7f, 1.4f, 1.1f);

        [SerializeField, Range(1f, 40f)]
        private float recoilSharpness = 18f;

        [SerializeField, Range(1f, 40f)]
        private float recoilReturnSharpness = 10f;

        private Vector3 _currentSwayPosition;
        private Vector3 _currentSwayEuler;
        private Vector3 _currentRecoilPosition;
        private Vector3 _targetRecoilPosition;
        private Vector3 _currentRecoilEuler;
        private Vector3 _targetRecoilEuler;
        private float _bobTime;

        private void Awake()
        {
            if (weaponFireController == null)
            {
                weaponFireController = GetComponentInParent<Camera>()?.GetComponent<WeaponFireController>();
            }

            if (weaponRoot == null)
            {
                Transform found = transform.Find("SM_HK_MP5_ViewModel");
                if (found != null)
                {
                    weaponRoot = found;
                }
            }

            ApplyBasePose();
        }

        private void OnEnable()
        {
            if (weaponFireController != null)
            {
                weaponFireController.Fired += HandleWeaponFired;
            }
        }

        private void OnDisable()
        {
            if (weaponFireController != null)
            {
                weaponFireController.Fired -= HandleWeaponFired;
            }
        }

        private void LateUpdate()
        {
            Vector2 lookDelta = ReadLookDelta();
            Vector2 moveInput = ReadMoveInput();

            Vector3 targetSwayPosition = new(
                Mathf.Clamp(-lookDelta.x * swayPositionAmount, -0.05f, 0.05f),
                Mathf.Clamp(-lookDelta.y * swayPositionAmount, -0.05f, 0.05f),
                0f);

            Vector3 targetSwayEuler = new(
                Mathf.Clamp(lookDelta.y * swayRotationAmount, -8f, 8f),
                Mathf.Clamp(-lookDelta.x * swayRotationAmount, -8f, 8f),
                Mathf.Clamp(-lookDelta.x * (swayRotationAmount * 0.35f), -4f, 4f));

            float moveMagnitude = Mathf.Clamp01(moveInput.magnitude);
            _bobTime += Time.deltaTime * (1f + moveMagnitude * moveBobFrequency);
            Vector3 bobOffset = new(
                Mathf.Sin(_bobTime * 0.5f) * moveBobAmount * 0.7f * moveMagnitude,
                Mathf.Abs(Mathf.Cos(_bobTime)) * moveBobAmount * moveMagnitude,
                0f);

            _currentSwayPosition = Vector3.Lerp(_currentSwayPosition, targetSwayPosition + bobOffset, Time.deltaTime * swaySharpness);
            _currentSwayEuler = Vector3.Lerp(_currentSwayEuler, targetSwayEuler, Time.deltaTime * swaySharpness);

            _targetRecoilPosition = Vector3.Lerp(_targetRecoilPosition, Vector3.zero, Time.deltaTime * recoilReturnSharpness);
            _targetRecoilEuler = Vector3.Lerp(_targetRecoilEuler, Vector3.zero, Time.deltaTime * recoilReturnSharpness);
            _currentRecoilPosition = Vector3.Lerp(_currentRecoilPosition, _targetRecoilPosition, Time.deltaTime * recoilSharpness);
            _currentRecoilEuler = Vector3.Lerp(_currentRecoilEuler, _targetRecoilEuler, Time.deltaTime * recoilSharpness);

            transform.localPosition = rootLocalPosition + _currentSwayPosition + _currentRecoilPosition;
            transform.localRotation = Quaternion.Euler(rootLocalEuler + _currentSwayEuler + _currentRecoilEuler);

            if (weaponRoot != null)
            {
                weaponRoot.localPosition = weaponLocalPosition;
                weaponRoot.localRotation = Quaternion.Euler(weaponLocalEuler);
                weaponRoot.localScale = weaponLocalScale;
            }
        }

        private void HandleWeaponFired()
        {
            _targetRecoilPosition += recoilKickPosition;
            _targetRecoilEuler += recoilKickEuler;
        }

        private void ApplyBasePose()
        {
            transform.localPosition = rootLocalPosition;
            transform.localRotation = Quaternion.Euler(rootLocalEuler);

            if (weaponRoot != null)
            {
                weaponRoot.localPosition = weaponLocalPosition;
                weaponRoot.localRotation = Quaternion.Euler(weaponLocalEuler);
                weaponRoot.localScale = weaponLocalScale;
            }
        }

        private static Vector2 ReadLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() * 0.01f : Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed) y += 1f;
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }
    }
}
