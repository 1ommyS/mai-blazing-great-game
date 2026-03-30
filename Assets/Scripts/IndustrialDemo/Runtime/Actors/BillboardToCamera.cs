using UnityEngine;

namespace IndustrialDemo.Actors
{
    public class BillboardToCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional camera used as the facing target. Falls back to Camera.main.")]
        private Camera targetCamera;

        [SerializeField, Tooltip("Locks rotation to the world Y axis so sprites stay upright.")]
        private bool yawOnly = true;

        private void LateUpdate()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            Vector3 forward = cameraToUse.transform.position - transform.position;
            if (yawOnly)
            {
                forward.y = 0f;
            }

            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(-forward.normalized, Vector3.up);
        }
    }
}
