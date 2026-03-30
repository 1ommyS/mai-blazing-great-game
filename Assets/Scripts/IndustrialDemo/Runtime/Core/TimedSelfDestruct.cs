using UnityEngine;

namespace IndustrialDemo.Core
{
    public class TimedSelfDestruct : MonoBehaviour
    {
        [SerializeField, Min(0.01f), Tooltip("How long this transient object stays alive.")]
        private float lifetime = 1.25f;

        [SerializeField, Tooltip("Optional local scale applied when the effect spawns.")]
        private Vector3 startupScale = new(0.2f, 0.2f, 0.2f);

        private void OnEnable()
        {
            transform.localScale = startupScale;

            Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (Collider activeCollider in colliders)
            {
                activeCollider.enabled = false;
            }

            Destroy(gameObject, lifetime);
        }
    }
}
