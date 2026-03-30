using UnityEngine;

namespace IndustrialDemo.Breaching
{
    public class PanelBypassConsole : MonoBehaviour, IInteractionHighlightTarget
    {
        [SerializeField, Tooltip("Entry unlocked by this panel.")]
        private BreachableEntry linkedEntry;

        [SerializeField, Tooltip("Prompt label shown by the demo interactor.")]
        private string promptLabel = "security console";

        [SerializeField, Tooltip("If true, this panel can only be used once.")]
        private bool singleUse = true;

        [SerializeField, Tooltip("Renderers highlighted when this panel is targeted.")]
        private Renderer[] highlightRenderers;

        [SerializeField, ColorUsage(false, true), Tooltip("Outline tint applied while the player is targeting this panel.")]
        private Color highlightColor = new(0.36f, 0.92f, 1f, 1f);

        [SerializeField, Tooltip("Offset used for the world marker above this console.")]
        private Vector3 interactionIndicatorOffset = new(0f, 0.35f, 0f);

        private bool _used;
        private GameObject[] _outlineObjects;

        public string PromptLabel => _used ? "Bypass used" : promptLabel;
        public Color InteractionIndicatorColor => highlightColor;

        private void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            _outlineObjects = InteractionHighlightUtility.CreateOutlineObjects(highlightRenderers, highlightColor, "PanelOutline");
        }

        private void OnDisable()
        {
            SetInteractionHighlight(false);
        }

        public bool TryUse()
        {
            if (_used || linkedEntry == null)
            {
                return false;
            }

            bool success = linkedEntry.TryPanelBypass();
            if (success && singleUse)
            {
                _used = true;
            }

            return success;
        }

        public void SetInteractionHighlight(bool isHighlighted)
        {
            if (_outlineObjects == null)
            {
                return;
            }

            for (int i = 0; i < _outlineObjects.Length; i++)
            {
                GameObject outline = _outlineObjects[i];
                if (outline == null)
                {
                    continue;
                }

                outline.SetActive(isHighlighted);
            }
        }

        public Vector3 GetInteractionWorldPosition()
        {
            if (highlightRenderers != null)
            {
                for (int i = 0; i < highlightRenderers.Length; i++)
                {
                    Renderer renderer = highlightRenderers[i];
                    if (renderer != null)
                    {
                        return renderer.bounds.center + interactionIndicatorOffset;
                    }
                }
            }

            Collider collider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            if (collider != null)
            {
                return collider.bounds.center + interactionIndicatorOffset;
            }

            return transform.position + Vector3.up * (1.8f + interactionIndicatorOffset.y);
        }
    }
}
