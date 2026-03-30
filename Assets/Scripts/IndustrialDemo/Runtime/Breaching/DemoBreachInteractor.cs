using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndustrialDemo.Breaching
{
    public class DemoBreachInteractor : MonoBehaviour
    {
        private readonly struct PromptState
        {
            public PromptState(string title, string primaryAction, string secondaryAction, Vector3 worldPosition, Color accentColor)
            {
                Title = title;
                PrimaryAction = primaryAction;
                SecondaryAction = secondaryAction;
                WorldPosition = worldPosition;
                AccentColor = accentColor;
            }

            public string Title { get; }
            public string PrimaryAction { get; }
            public string SecondaryAction { get; }
            public Vector3 WorldPosition { get; }
            public Color AccentColor { get; }
            public bool HasSecondaryAction => !string.IsNullOrEmpty(SecondaryAction);
            public bool IsValid => !string.IsNullOrEmpty(Title);
        }

        [SerializeField, Tooltip("Camera used for interaction rays. Defaults to the local Camera.")]
        private Camera interactionCamera;

        [SerializeField, Min(0.5f), Tooltip("Maximum interaction distance.")]
        private float interactDistance = 4.5f;

        [SerializeField, Tooltip("Layers used for interaction raycasts.")]
        private LayerMask interactionMask = ~0;

        private IInteractionHighlightTarget _activeHighlightTarget;
        private PromptState _promptState;
        private GUIStyle _bottomBoxStyle;
        private GUIStyle _bottomTitleStyle;
        private GUIStyle _bottomActionStyle;
        private GUIStyle _markerBoxStyle;
        private GUIStyle _markerTitleStyle;
        private GUIStyle _markerActionStyle;

        private void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponent<Camera>();
            }
        }

        private void Update()
        {
            _promptState = default;

            if (interactionCamera == null)
            {
                SetHighlightTarget(null);
                return;
            }

            Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactionMask, QueryTriggerInteraction.Ignore))
            {
                SetHighlightTarget(null);
                return;
            }

            ShotBreachZone shotBreachZone = hit.collider.GetComponent<ShotBreachZone>() ?? hit.collider.GetComponentInParent<ShotBreachZone>();
            if (shotBreachZone != null)
            {
                SetHighlightTarget(shotBreachZone);

                string shotSecondaryAction = string.Empty;
                BreachableEntry linkedEntry = shotBreachZone.LinkedEntry;
                if (linkedEntry != null && linkedEntry.CanForcedBreach())
                {
                    shotSecondaryAction = "[F] FORCE OPEN LOUD";
                    if (ReadForcedPressed())
                    {
                        linkedEntry.TryBeginForcedBreach(this);
                    }
                }

                _promptState = new PromptState(
                    title: shotBreachZone.PromptLabel.ToUpperInvariant(),
                    primaryAction: "[LMB] SHOOT BREACH",
                    secondaryAction: shotSecondaryAction,
                    worldPosition: shotBreachZone.GetInteractionWorldPosition(),
                    accentColor: shotBreachZone.InteractionIndicatorColor);
                return;
            }

            if (hit.collider.TryGetComponent(out PanelBypassConsole panel))
            {
                SetHighlightTarget(panel);
                _promptState = new PromptState(
                    title: panel.PromptLabel.ToUpperInvariant(),
                    primaryAction: "[E] BYPASS QUIET",
                    secondaryAction: string.Empty,
                    worldPosition: panel.GetInteractionWorldPosition(),
                    accentColor: panel.InteractionIndicatorColor);

                if (ReadInteractPressed())
                {
                    panel.TryUse();
                }

                return;
            }

            BreachableEntry entry = hit.collider.GetComponent<BreachableEntry>() ?? hit.collider.GetComponentInParent<BreachableEntry>();
            if (entry == null)
            {
                SetHighlightTarget(null);
                return;
            }

            SetHighlightTarget(entry);

            string title = entry.InteractionLabel.ToUpperInvariant();
            string primaryAction = string.Empty;
            string secondaryAction = string.Empty;

            if (entry.CanManualBreach())
            {
                primaryAction = "[E] OPEN QUIET";
                if (ReadInteractPressed())
                {
                    entry.TryBeginManualBreach(this);
                }
            }

            if (entry.CanForcedBreach())
            {
                secondaryAction = "[F] FORCE OPEN LOUD";

                if (ReadForcedPressed())
                {
                    entry.TryBeginForcedBreach(this);
                }
            }

            if (string.IsNullOrEmpty(primaryAction) && !string.IsNullOrEmpty(secondaryAction))
            {
                primaryAction = secondaryAction;
                secondaryAction = string.Empty;
            }

            if (string.IsNullOrEmpty(primaryAction) && string.IsNullOrEmpty(secondaryAction))
            {
                SetHighlightTarget(null);
                return;
            }

            _promptState = new PromptState(
                title: title,
                primaryAction: primaryAction,
                secondaryAction: secondaryAction,
                worldPosition: entry.GetInteractionWorldPosition(),
                accentColor: entry.InteractionIndicatorColor);
        }

        private void OnGUI()
        {
            if (!_promptState.IsValid)
            {
                return;
            }

            EnsureStyles();
            DrawWorldMarker();
            DrawBottomPrompt();
        }

        private void OnDisable()
        {
            SetHighlightTarget(null);
        }

        private void SetHighlightTarget(IInteractionHighlightTarget nextTarget)
        {
            if (ReferenceEquals(_activeHighlightTarget, nextTarget))
            {
                return;
            }

            _activeHighlightTarget?.SetInteractionHighlight(false);
            _activeHighlightTarget = nextTarget;
            _activeHighlightTarget?.SetInteractionHighlight(true);
        }

        private void DrawWorldMarker()
        {
            if (interactionCamera == null)
            {
                return;
            }

            Vector3 screenPoint = interactionCamera.WorldToScreenPoint(_promptState.WorldPosition);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            float height = _promptState.HasSecondaryAction ? 74f : 54f;
            Rect markerRect = new(
                screenPoint.x - 90f,
                Screen.height - screenPoint.y - height - 18f,
                180f,
                height);

            Color previousColor = GUI.color;
            GUI.color = Color.Lerp(Color.white, _promptState.AccentColor, 0.25f);
            GUI.Box(markerRect, GUIContent.none, _markerBoxStyle);
            GUI.color = previousColor;

            GUI.Label(new Rect(markerRect.x + 10f, markerRect.y + 7f, markerRect.width - 20f, 24f), _promptState.Title, _markerTitleStyle);
            GUI.Label(new Rect(markerRect.x + 10f, markerRect.y + 30f, markerRect.width - 20f, 18f), _promptState.PrimaryAction, _markerActionStyle);
            if (_promptState.HasSecondaryAction)
            {
                GUI.Label(new Rect(markerRect.x + 10f, markerRect.y + 48f, markerRect.width - 20f, 18f), _promptState.SecondaryAction, _markerActionStyle);
            }
        }

        private void DrawBottomPrompt()
        {
            float height = _promptState.HasSecondaryAction ? 86f : 66f;
            Rect rect = new(Screen.width * 0.5f - 250f, Screen.height - 138f, 500f, height);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.1f, 0.12f, 0.92f);
            GUI.Box(rect, GUIContent.none, _bottomBoxStyle);
            GUI.color = previousColor;

            GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width - 36f, 28f), _promptState.Title, _bottomTitleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 40f, rect.width - 36f, 20f), _promptState.PrimaryAction, _bottomActionStyle);
            if (_promptState.HasSecondaryAction)
            {
                GUI.Label(new Rect(rect.x + 18f, rect.y + 60f, rect.width - 36f, 20f), _promptState.SecondaryAction, _bottomActionStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_bottomBoxStyle != null)
            {
                return;
            }

            _bottomBoxStyle = new GUIStyle(GUI.skin.box);
            _bottomBoxStyle.normal.background = Texture2D.whiteTexture;
            _bottomBoxStyle.border = new RectOffset(0, 0, 0, 0);

            _bottomTitleStyle = new GUIStyle(GUI.skin.label);
            _bottomTitleStyle.alignment = TextAnchor.UpperLeft;
            _bottomTitleStyle.fontSize = 18;
            _bottomTitleStyle.fontStyle = FontStyle.Bold;
            _bottomTitleStyle.normal.textColor = Color.white;

            _bottomActionStyle = new GUIStyle(GUI.skin.label);
            _bottomActionStyle.alignment = TextAnchor.UpperLeft;
            _bottomActionStyle.fontSize = 14;
            _bottomActionStyle.fontStyle = FontStyle.Bold;
            _bottomActionStyle.normal.textColor = new Color(0.98f, 0.91f, 0.47f, 1f);

            _markerBoxStyle = new GUIStyle(GUI.skin.box);
            _markerBoxStyle.normal.background = Texture2D.whiteTexture;
            _markerBoxStyle.border = new RectOffset(0, 0, 0, 0);

            _markerTitleStyle = new GUIStyle(GUI.skin.label);
            _markerTitleStyle.alignment = TextAnchor.UpperCenter;
            _markerTitleStyle.fontSize = 13;
            _markerTitleStyle.fontStyle = FontStyle.Bold;
            _markerTitleStyle.normal.textColor = Color.white;

            _markerActionStyle = new GUIStyle(GUI.skin.label);
            _markerActionStyle.alignment = TextAnchor.UpperCenter;
            _markerActionStyle.fontSize = 11;
            _markerActionStyle.fontStyle = FontStyle.Bold;
            _markerActionStyle.normal.textColor = new Color(1f, 0.94f, 0.58f, 1f);
        }

        private static bool ReadInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private static bool ReadForcedPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }
    }
}
