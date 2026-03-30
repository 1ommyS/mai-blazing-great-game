using IndustrialDemo.Combat;
using UnityEngine;

namespace IndustrialDemo.Breaching
{
    public class ShotBreachZone : MonoBehaviour, IShotDamageReceiver, IInteractionHighlightTarget
    {
        [SerializeField, Tooltip("Entry affected by this vulnerable zone.")]
        private BreachableEntry linkedEntry;

        [SerializeField, Tooltip("Type of vulnerable zone.")]
        private ShotBreachZoneType zoneType = ShotBreachZoneType.Lock;

        [SerializeField, Min(0f), Tooltip("Minimum incoming damage required to trigger this zone.")]
        private float minimumDamage = 1f;

        [SerializeField, Tooltip("Optional impact effect used when the zone is successfully hit.")]
        private GameObject successVfxPrefab;

        [SerializeField]
        private Renderer[] highlightRenderers;

        [SerializeField, ColorUsage(false, true)]
        private Color highlightColor = new(1f, 0.45f, 0.22f, 1f);

        [SerializeField]
        private Vector3 interactionIndicatorOffset = new(0f, 0.18f, 0f);

        private GameObject[] _outlineObjects;

        public string PromptLabel => zoneType == ShotBreachZoneType.Hinge ? "shoot hinge" : "shoot lock";
        public BreachableEntry LinkedEntry => linkedEntry;
        public Color InteractionIndicatorColor => highlightColor;

        private void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            _outlineObjects = InteractionHighlightUtility.CreateOutlineObjects(highlightRenderers, highlightColor, "ShotZoneOutline");
        }

        private void OnDisable()
        {
            SetInteractionHighlight(false);
        }

        public void ReceiveShotDamage(ShotImpactContext context)
        {
            if (linkedEntry == null || context.Damage < minimumDamage)
            {
                return;
            }

            bool success = linkedEntry.TryShotBreach(zoneType);
            if (!success || successVfxPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(context.Hit.normal.normalized);
            Instantiate(successVfxPrefab, context.Hit.point, rotation);
        }

        public void SetInteractionHighlight(bool isHighlighted)
        {
            if (_outlineObjects == null)
            {
                return;
            }

            for (int i = 0; i < _outlineObjects.Length; i++)
            {
                if (_outlineObjects[i] != null)
                {
                    _outlineObjects[i].SetActive(isHighlighted);
                }
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

            return transform.position + Vector3.up * (0.8f + interactionIndicatorOffset.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = zoneType == ShotBreachZoneType.Hinge
                ? new Color(1f, 0.7f, 0.2f, 0.5f)
                : new Color(1f, 0.2f, 0.2f, 0.5f);

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = previous;
        }
    }
}
