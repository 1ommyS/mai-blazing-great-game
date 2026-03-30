using IndustrialDemo.Breaching;
using UnityEngine;

namespace IndustrialDemo.Foam
{
    public class FoamCoverAnchor : MonoBehaviour, IFoamHighlightTarget
    {
        [SerializeField, Tooltip("Suggested local size for a foam cover patch spawned on this marker.")]
        private Vector3 coverSize = new(1.4f, 1.8f, 0.6f);

        [SerializeField]
        private Renderer[] highlightRenderers;

        [SerializeField, ColorUsage(false, true)]
        private Color highlightColor = new(0.85f, 0.95f, 1f, 1f);

        private GameObject[] _outlineObjects;

        public Vector3 CoverSize => coverSize;
        public string FoamActionLabel => "MAKE COVER";
        public string FoamPurposeLabel => "Foam here to create temporary cover.";
        public Color FoamHighlightColor => highlightColor;

        private void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            _outlineObjects = InteractionHighlightUtility.CreateOutlineObjects(highlightRenderers, highlightColor, "FoamCoverOutline");
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
            Gizmos.color = new Color(0.85f, 0.95f, 1f, 0.45f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, coverSize);
            Gizmos.matrix = previous;
        }
    }
}
