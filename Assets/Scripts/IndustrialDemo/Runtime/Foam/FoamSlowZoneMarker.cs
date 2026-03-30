using IndustrialDemo.Breaching;
using UnityEngine;

namespace IndustrialDemo.Foam
{
    public class FoamSlowZoneMarker : MonoBehaviour, IFoamHighlightTarget
    {
        [SerializeField, Tooltip("Size used for the foam slow zone volume spawned on this marker.")]
        private Vector3 zoneSize = new(2.8f, 0.4f, 2.8f);

        [SerializeField, Range(0.1f, 1f), Tooltip("Movement multiplier applied while inside the spawned foam slow zone.")]
        private float slowMultiplier = 0.5f;

        [SerializeField]
        private Renderer[] highlightRenderers;

        [SerializeField, ColorUsage(false, true)]
        private Color highlightColor = new(0.42f, 0.84f, 1f, 1f);

        private GameObject[] _outlineObjects;

        public Vector3 ZoneSize => zoneSize;
        public float SlowMultiplier => slowMultiplier;
        public string FoamActionLabel => "SLOW CHOKE";
        public string FoamPurposeLabel => "Foam this marker to slow anything crossing the lane.";
        public Color FoamHighlightColor => highlightColor;

        private void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            _outlineObjects = InteractionHighlightUtility.CreateOutlineObjects(highlightRenderers, highlightColor, "FoamSlowOutline");
        }

        private void OnDisable()
        {
            SetFoamHighlight(false);
        }

        public void SetFoamHighlight(bool isHighlighted)
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.45f, 0.8f, 1f, 0.4f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, zoneSize);
            Gizmos.matrix = previous;
        }
    }
}
