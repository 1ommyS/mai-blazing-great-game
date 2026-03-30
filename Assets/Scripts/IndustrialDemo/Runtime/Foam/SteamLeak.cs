using IndustrialDemo.Core;
using UnityEngine;

namespace IndustrialDemo.Foam
{
    public class SteamLeak : MonoBehaviour
    {
        [SerializeField, Tooltip("Whether the leak starts active when play mode begins.")]
        private bool startsActive = true;

        [SerializeField, Tooltip("Optional visual root disabled while the leak is sealed.")]
        private GameObject leakVisualRoot;

        [SerializeField, Tooltip("Optional blocker disabled while the leak is sealed.")]
        private GameObject hazardBlocker;

        [SerializeField, Min(0f), Tooltip("Ambient noise emitted by the leak while active.")]
        private float leakNoise = 3f;

        private bool _isSealed;

        public bool IsActive => startsActive && !_isSealed;

        private void Awake()
        {
            ApplyState();
        }

        public void SetSealed(bool sealedState)
        {
            _isSealed = sealedState;
            ApplyState();

            NoiseSystem.Emit(transform.position, sealedState ? leakNoise * 0.25f : leakNoise, gameObject, sealedState ? "steam_sealed" : "steam_resumed");
        }

        private void ApplyState()
        {
            bool leakActive = IsActive;

            if (leakVisualRoot != null)
            {
                leakVisualRoot.SetActive(leakActive);
            }

            if (hazardBlocker != null)
            {
                hazardBlocker.SetActive(leakActive);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsActive ? new Color(1f, 0.6f, 0.2f, 0.6f) : new Color(0.3f, 1f, 0.8f, 0.6f);
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
