using IndustrialDemo.Breaching;
using UnityEngine;

namespace IndustrialDemo.Foam
{
    public class FoamSealPoint : MonoBehaviour, IFoamHighlightTarget
    {
        [SerializeField, Tooltip("Leak affected by foam sprayed on this point.")]
        private SteamLeak linkedLeak;

        [SerializeField]
        private Renderer[] highlightRenderers;

        [SerializeField, ColorUsage(false, true)]
        private Color highlightColor = new(0.34f, 0.98f, 1f, 1f);

        private GameObject[] _outlineObjects;

        public SteamLeak LinkedLeak => linkedLeak;
        public string FoamActionLabel => "SEAL LEAK";
        public string FoamPurposeLabel => "Foam seals this point and clears the steam gate.";
        public Color FoamHighlightColor => highlightColor;

        private void Awake()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            _outlineObjects = InteractionHighlightUtility.CreateOutlineObjects(highlightRenderers, highlightColor, "FoamSealOutline");
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
            Gizmos.color = new Color(0.3f, 1f, 1f, 0.75f);
            Gizmos.DrawSphere(transform.position, 0.18f);
        }
    }
}
