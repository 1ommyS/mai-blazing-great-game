using System.Collections;
using IndustrialDemo.Player;
using UnityEngine;

namespace IndustrialDemo.Actors
{
    [DisallowMultipleComponent]
    public class EnemySentryBehaviour : MonoBehaviour
    {
        [SerializeField, Tooltip("Maximum range at which the enemy can engage the player.")]
        private float detectionRange = 45f;

        [SerializeField, Min(0.05f), Tooltip("Seconds between shots while the player is visible.")]
        private float fireInterval = 0.45f;

        [SerializeField, Min(0f), Tooltip("Damage dealt to the player per shot.")]
        private float shotDamage = 8f;

        [SerializeField, Min(0.5f), Tooltip("Vertical offset used for aiming when no explicit origin is set.")]
        private float aimHeight = 1.55f;

        [SerializeField, Min(0.1f), Tooltip("How quickly the sentry rotates toward the player.")]
        private float turnSpeed = 8f;

        [SerializeField, Tooltip("Optional explicit muzzle/eye origin. If omitted, the enemy root is used.")]
        private Transform shotOrigin;

        [SerializeField, Tooltip("Disable the humanoid animator because the imported rig breaks apart in play mode.")]
        private bool disableAnimatorAtRuntime = true;

        private EnemyPresentationTarget _presentationTarget;
        private Animator _animator;
        private DemoFirstPersonMotor _playerMotor;
        private DemoPlayerHealth _playerHealth;
        private float _nextFireTime;

        private void Awake()
        {
            _presentationTarget = GetComponent<EnemyPresentationTarget>();
            _animator = GetComponent<Animator>();

            if (disableAnimatorAtRuntime && _animator != null)
            {
                _animator.enabled = false;
            }

            DisableLegacyRenderers();
        }

        private void OnEnable()
        {
            DisableLegacyRenderers();
        }

        private void Update()
        {
            if (_presentationTarget != null && _presentationTarget.IsDead)
            {
                return;
            }

            if (!TryResolvePlayer())
            {
                return;
            }

            Vector3 origin = GetShotOrigin();
            Vector3 target = GetPlayerTarget();
            Vector3 toPlayer = target - origin;
            float sqrDistance = toPlayer.sqrMagnitude;
            if (sqrDistance > detectionRange * detectionRange)
            {
                return;
            }

            RotateTowards(target);

            if (!HasLineOfSight(origin, target))
            {
                return;
            }

            if (Time.time < _nextFireTime)
            {
                return;
            }

            _nextFireTime = Time.time + fireInterval;
            _playerHealth.ApplyDamage(shotDamage);
            StartCoroutine(ShotFlashRoutine(origin, target));
        }

        private bool TryResolvePlayer()
        {
            if (_playerMotor == null)
            {
                _playerMotor = FindFirstObjectByType<DemoFirstPersonMotor>();
            }

            if (_playerMotor == null)
            {
                return false;
            }

            if (_playerHealth == null)
            {
                _playerHealth = _playerMotor.GetComponent<DemoPlayerHealth>();
            }

            return _playerHealth != null;
        }

        private Vector3 GetShotOrigin()
        {
            if (shotOrigin != null)
            {
                return shotOrigin.position;
            }

            return transform.position + Vector3.up * aimHeight;
        }

        private Vector3 GetPlayerTarget()
        {
            Transform playerTransform = _playerMotor.transform;
            return playerTransform.position + Vector3.up * 1.45f;
        }

        private void RotateTowards(Vector3 target)
        {
            Vector3 planar = target - transform.position;
            planar.y = 0f;
            if (planar.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion desired = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * turnSpeed);
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 target)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return false;
            }

            if (!Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider.GetComponentInParent<DemoFirstPersonMotor>() != null;
        }

        private IEnumerator ShotFlashRoutine(Vector3 origin, Vector3 target)
        {
            float duration = 0.05f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                Debug.DrawLine(origin, target, new Color(1f, 0.3f, 0.15f), 0f, false);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void DisableLegacyRenderers()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer == null)
                {
                    continue;
                }

                string objectName = renderer.gameObject.name;
                if (objectName.Contains("Proxy"))
                {
                    renderer.enabled = true;
                    continue;
                }

                renderer.enabled = false;
            }
        }
    }
}
