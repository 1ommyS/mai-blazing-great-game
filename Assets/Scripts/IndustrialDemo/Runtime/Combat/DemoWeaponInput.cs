using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialDemo.Combat
{
    public class DemoWeaponInput : MonoBehaviour
    {
        [SerializeField, Tooltip("Weapon fire controller triggered by this input bridge.")]
        private WeaponFireController weaponFireController;

        [SerializeField, Tooltip("If enabled, holding Fire1 repeatedly shoots using the weapon fire delay.")]
        private bool holdToFire = true;

        private void Reset()
        {
            weaponFireController = GetComponent<WeaponFireController>();
        }

        private void Awake()
        {
            if (weaponFireController == null)
            {
                weaponFireController = GetComponent<WeaponFireController>();
            }
        }

        private void Update()
        {
            if (weaponFireController == null)
            {
                return;
            }

            if (ReadReloadPressed())
            {
                weaponFireController.TryReload();
            }

            bool wantsToFire = ReadFireInput();
            if (!wantsToFire)
            {
                return;
            }

            weaponFireController.TryFire();
        }

        private void OnGUI()
        {
            if (weaponFireController == null)
            {
                return;
            }

            string state = weaponFireController.IsReloading
                ? "RELOADING"
                : $"{weaponFireController.CurrentAmmo}/{weaponFireController.MagazineSize}  RES {weaponFireController.ReserveAmmo}";

            Rect rect = new(Screen.width - 250f, Screen.height - 42f, 232f, 24f);
            GUI.Box(rect, $"[LMB] Fire  [R] Reload  {state}");
        }

        private bool ReadFireInput()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            return holdToFire ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
#else
            return holdToFire ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
#endif
        }

        private static bool ReadReloadPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }
}
