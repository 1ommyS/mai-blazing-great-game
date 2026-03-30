using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialDemo.Foam
{
    public class DemoFoamInput : MonoBehaviour
    {
        [SerializeField, Tooltip("Foam tool used by the demo player.")]
        private FoamToolController foamTool;

        [SerializeField, Tooltip("If enabled, right mouse button continuously sprays foam while held.")]
        private bool holdToSpray = true;

        private void Awake()
        {
            if (foamTool == null)
            {
                foamTool = GetComponent<FoamToolController>();
            }
        }

        private void Update()
        {
            if (foamTool == null)
            {
                return;
            }

            bool wantsSpray = holdToSpray ? ReadSprayHeld() : ReadSprayPressed();
            if (wantsSpray)
            {
                foamTool.TryUseTool();
            }
        }

        private void OnGUI()
        {
            if (foamTool == null)
            {
                return;
            }

            Rect rect = new(18f, Screen.height - 92f, 360f, 72f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f), $"[RMB] FOAM  {foamTool.CurrentAmmo}");
            GUI.Label(new Rect(rect.x + 10f, rect.y + 28f, rect.width - 20f, 18f), foamTool.PreviewActionLabel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 46f, rect.width - 20f, 18f), foamTool.PreviewPurposeLabel);
        }

        private static bool ReadSprayHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private static bool ReadSprayPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }
    }
}
