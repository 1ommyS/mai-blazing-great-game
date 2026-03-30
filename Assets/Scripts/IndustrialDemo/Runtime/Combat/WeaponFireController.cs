using System;
using System.Collections.Generic;
using IndustrialDemo.Core;
using UnityEngine;

namespace IndustrialDemo.Combat
{
    public class WeaponFireController : MonoBehaviour
    {
        private const float SurfaceExitStep = 0.05f;

        [Header("References")]
        [SerializeField, Tooltip("Optional muzzle transform. If empty, this transform is used.")]
        private Transform muzzlePoint;

        [SerializeField, Tooltip("Optional view camera used to correct shots toward the aimed point.")]
        private Camera aimCamera;

        [Header("Weapon Stats")]
        [SerializeField, Min(0f), Tooltip("Base damage applied by a direct hit.")]
        private float damage = 34f;

        [SerializeField, Min(0.1f), Tooltip("Maximum hitscan range.")]
        private float range = 120f;

        [SerializeField, Min(0f), Tooltip("Total penetration budget available for this shot.")]
        private float penetrationPower = 140f;

        [SerializeField, Tooltip("Whether this weapon can attempt a ricochet.")]
        private bool canRicochet = true;

        [SerializeField, Min(0), Tooltip("Maximum number of ricochets allowed for a single shot.")]
        private int ricochetMaxCount = 1;

        [SerializeField, Min(0f), Tooltip("Noise emitted when firing.")]
        private float impactNoise = 8f;

        [SerializeField, Min(0.01f), Tooltip("Delay between two shots in seconds.")]
        private float fireDelay = 0.14f;

        [SerializeField, Min(1), Tooltip("Number of rounds available before a reload is required.")]
        private int magazineSize = 30;

        [SerializeField, Min(0), Tooltip("Reserve ammo available for reloads. Use a high value for effectively infinite ammo.")]
        private int reserveAmmo = 120;

        [SerializeField, Min(0.1f), Tooltip("Time in seconds needed to reload the weapon.")]
        private float reloadDuration = 1.4f;

        [SerializeField, Tooltip("Layers considered valid for hitscan impacts.")]
        private LayerMask surfaceImpactMask = ~0;

        [Header("Tuning")]
        [SerializeField, Min(1), Tooltip("Safety cap for the number of contacts a single shot can process.")]
        private int maxContactsPerShot = 10;

        [SerializeField, Min(1), Tooltip("Maximum number of times a single shot is allowed to resolve against the same collider.")]
        private int maxHitsPerCollider = 2;

        [SerializeField, Range(0f, 1f), Tooltip("Damage multiplier applied after a ricochet.")]
        private float ricochetDamageMultiplier = 0.65f;

        [SerializeField, Range(0f, 1f), Tooltip("Penetration multiplier applied after a ricochet.")]
        private float ricochetPenetrationMultiplier = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Minimum distance offset used when continuing a shot after an impact.")]
        private float retraceEpsilon = 0.02f;

        [SerializeField, Tooltip("If enabled, debug rays are drawn for each processed shot segment.")]
        private bool drawDebugRays;

        [Header("Tracer")]
        [SerializeField, Tooltip("If enabled, each shot segment spawns a visible tracer line.")]
        private bool spawnTracers = true;

        [SerializeField, ColorUsage(false, true), Tooltip("Tint used by the player weapon tracer.")]
        private Color tracerColor = new(1f, 0.82f, 0.46f, 0.95f);

        [SerializeField, Min(0.001f), Tooltip("Visual width of the tracer line.")]
        private float tracerWidth = 0.022f;

        [SerializeField, Min(0.01f), Tooltip("Lifetime of the tracer visual in seconds.")]
        private float tracerDuration = 0.05f;

        private readonly Dictionary<Collider, int> _colliderVisitCounts = new();
        private readonly HashSet<Collider> _ownerColliders = new();
        private float _nextAllowedShotTime;
        private bool _isReloading;
        private float _reloadFinishTime;
        private int _currentAmmo;

        public event Action Fired;
        public event Action ReloadStarted;
        public event Action ReloadFinished;

        public int CurrentAmmo => _currentAmmo;
        public int MagazineSize => magazineSize;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => _isReloading;

        private void Awake()
        {
            CacheReferences();
            CacheOwnerColliders();
            EnsureAmmoInitialized();
        }

        private void OnEnable()
        {
            CacheReferences();
            CacheOwnerColliders();
            EnsureAmmoInitialized();
        }

        private void Update()
        {
            if (_isReloading && Time.time >= _reloadFinishTime)
            {
                FinishReload();
            }
        }

        public bool TryFire()
        {
            if (_isReloading || Time.time < _nextAllowedShotTime || _currentAmmo <= 0)
            {
                return false;
            }

            _currentAmmo--;
            _nextAllowedShotTime = Time.time + fireDelay;
            Fire();
            Fired?.Invoke();
            return true;
        }

        public bool TryReload()
        {
            if (_isReloading || _currentAmmo >= magazineSize || reserveAmmo <= 0)
            {
                return false;
            }

            _isReloading = true;
            _reloadFinishTime = Time.time + reloadDuration;
            ReloadStarted?.Invoke();
            return true;
        }

        [ContextMenu("Fire Test Shot")]
        public void Fire()
        {
            Transform originTransform = muzzlePoint != null ? muzzlePoint : transform;
            Vector3 origin = originTransform.position;
            Vector3 direction = ResolveInitialDirection(originTransform);

            NoiseSystem.Emit(origin, impactNoise, gameObject, "gunshot");

            _colliderVisitCounts.Clear();

            float remainingDamage = damage;
            float remainingPenetration = penetrationPower;
            int ricochetCount = 0;
            int processedContacts = 0;
            Vector3 currentOrigin = origin;
            Vector3 currentDirection = direction.normalized;
            float remainingRange = range;

            while (processedContacts < maxContactsPerShot && remainingRange > 0.01f)
            {
                if (!TryRaycastWorld(currentOrigin, currentDirection, remainingRange, out RaycastHit hit))
                {
                    SpawnTracer(currentOrigin, currentOrigin + currentDirection * remainingRange);

                    if (drawDebugRays)
                    {
                        Debug.DrawRay(currentOrigin, currentDirection * remainingRange, Color.cyan, 1.5f);
                    }

                    break;
                }

                processedContacts++;
                float travelledDistance = Vector3.Distance(currentOrigin, hit.point);
                remainingRange -= travelledDistance;

                if (drawDebugRays)
                {
                    Debug.DrawLine(currentOrigin, hit.point, Color.yellow, 1.5f);
                }

                SpawnTracer(currentOrigin, hit.point);

                if (_colliderVisitCounts.TryGetValue(hit.collider, out int previousVisits) && previousVisits >= maxHitsPerCollider)
                {
                    break;
                }

                _colliderVisitCounts[hit.collider] = previousVisits + 1;

                SurfaceMaterialDefaults surface = SurfaceMaterial.Resolve(hit.collider);
                var context = new ShotImpactContext(
                    hit,
                    currentDirection,
                    remainingDamage,
                    remainingPenetration,
                    ricochetCount,
                    gameObject);

                IShotDamageReceiver[] damageReceivers = hit.collider.GetComponents<IShotDamageReceiver>();
                for (int i = 0; i < damageReceivers.Length; i++)
                {
                    damageReceivers[i].ReceiveShotDamage(context);
                }

                surface.SurfaceComponent?.SpawnImpactFeedback(hit.point, hit.normal);

                if (TryHandleRicochet(
                        hit,
                        surface,
                        ref currentOrigin,
                        ref currentDirection,
                        ref remainingDamage,
                        ref remainingPenetration,
                        ref remainingRange,
                        ref ricochetCount))
                {
                    continue;
                }

                bool hasPenetrated = TryContinueThroughSurface(
                    hit,
                    surface,
                    ref currentOrigin,
                    currentDirection,
                    ref remainingDamage,
                    ref remainingPenetration,
                    ref remainingRange);

                if (hasPenetrated)
                {
                    continue;
                }

                break;
            }
        }

        private Vector3 ResolveInitialDirection(Transform originTransform)
        {
            if (aimCamera == null)
            {
                return originTransform.forward;
            }

            Ray cameraRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = cameraRay.origin + cameraRay.direction * range;

            if (TryRaycastWorld(cameraRay.origin, cameraRay.direction, range, out RaycastHit cameraHit))
            {
                aimPoint = cameraHit.point;
            }

            Vector3 correctedDirection = (aimPoint - originTransform.position).normalized;
            return correctedDirection.sqrMagnitude > 0f ? correctedDirection : originTransform.forward;
        }

        private bool TryContinueThroughSurface(
            RaycastHit hit,
            SurfaceMaterialDefaults surface,
            ref Vector3 currentOrigin,
            Vector3 currentDirection,
            ref float remainingDamage,
            ref float remainingPenetration,
            ref float remainingRange)
        {
            if (remainingPenetration <= 0f)
            {
                return false;
            }

            remainingPenetration -= surface.PenetrationCost;
            if (remainingPenetration <= 0f)
            {
                return false;
            }

            if (!TryFindExitPoint(hit.collider, hit.point, currentDirection, out Vector3 exitPoint))
            {
                return false;
            }

            surface.SurfaceComponent?.SpawnPenetrationFeedback(exitPoint, -hit.normal);
            currentOrigin = exitPoint + currentDirection * Mathf.Max(retraceEpsilon, SurfaceExitStep);
            remainingDamage *= Mathf.Clamp01(surface.DamageMultiplierThroughSurface);
            remainingRange -= Vector3.Distance(hit.point, exitPoint);
            return true;
        }

        private bool TryHandleRicochet(
            RaycastHit hit,
            SurfaceMaterialDefaults surface,
            ref Vector3 currentOrigin,
            ref Vector3 currentDirection,
            ref float remainingDamage,
            ref float remainingPenetration,
            ref float remainingRange,
            ref int ricochetCount)
        {
            if (!canRicochet || ricochetCount >= ricochetMaxCount)
            {
                return false;
            }

            if (!surface.CanRicochet)
            {
                return false;
            }

            if (remainingDamage <= 4f || remainingPenetration <= 4f)
            {
                return false;
            }

            if (!TryGetRicochet(surface, currentDirection, hit.normal, out Vector3 reflectedDirection))
            {
                return false;
            }

            ricochetCount++;
            remainingDamage *= ricochetDamageMultiplier;
            remainingPenetration *= ricochetPenetrationMultiplier;
            currentDirection = reflectedDirection.normalized;
            currentOrigin = hit.point + hit.normal * Mathf.Max(retraceEpsilon, SurfaceExitStep) + currentDirection * Mathf.Max(retraceEpsilon, SurfaceExitStep);
            remainingRange -= Mathf.Max(SurfaceExitStep, 0.05f);

            NoiseSystem.Emit(hit.point, impactNoise * 0.5f, hit.collider.gameObject, "ricochet");
            surface.SurfaceComponent?.SpawnRicochetFeedback(hit.point, hit.normal);

            if (drawDebugRays)
            {
                Debug.DrawRay(hit.point, currentDirection * 2f, Color.red, 1.5f);
            }

            return true;
        }

        private static bool TryFindExitPoint(Collider targetCollider, Vector3 entryPoint, Vector3 direction, out Vector3 exitPoint)
        {
            float castDistance = Mathf.Max(targetCollider.bounds.extents.magnitude * 2f, 0.5f);
            Vector3 startPoint = entryPoint - direction.normalized * 0.01f;

            Ray reverseRay = new Ray(startPoint + direction.normalized * castDistance, -direction.normalized);
            if (targetCollider.Raycast(reverseRay, out RaycastHit reverseHit, castDistance + 0.02f))
            {
                exitPoint = reverseHit.point;
                return true;
            }

            exitPoint = entryPoint + direction.normalized * SurfaceExitStep;
            return false;
        }

        private static bool TryGetRicochet(
            SurfaceMaterialDefaults surface,
            Vector3 incomingDirection,
            Vector3 hitNormal,
            out Vector3 reflectedDirection)
        {
            reflectedDirection = default;

            float impactAngle = Vector3.Angle(-incomingDirection.normalized, hitNormal.normalized);
            if (impactAngle < surface.RicochetMinAngle)
            {
                return false;
            }

            float glancingFactor = Mathf.InverseLerp(surface.RicochetMinAngle, 89f, impactAngle);
            if (glancingFactor < 1f - surface.RicochetChance)
            {
                return false;
            }

            reflectedDirection = Vector3.Reflect(incomingDirection.normalized, hitNormal.normalized);
            return reflectedDirection.sqrMagnitude > 0f && Vector3.Dot(reflectedDirection, hitNormal) > 0.001f;
        }

        private void CacheReferences()
        {
            if (aimCamera == null)
            {
                aimCamera = GetComponent<Camera>();
            }
        }

        private void CacheOwnerColliders()
        {
            _ownerColliders.Clear();

            Collider[] colliders = transform.root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    _ownerColliders.Add(colliders[i]);
                }
            }
        }

        private void EnsureAmmoInitialized()
        {
            if (_currentAmmo <= 0 && magazineSize > 0)
            {
                _currentAmmo = magazineSize;
            }
        }

        private bool TryRaycastWorld(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit bestHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, surfaceImpactMask, QueryTriggerInteraction.Ignore);
            float bestDistance = float.PositiveInfinity;
            bestHit = default;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidate = hits[i];
                if (IsIgnoredCollider(candidate.collider))
                {
                    continue;
                }

                if (candidate.distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = candidate.distance;
                bestHit = candidate;
            }

            return bestDistance < float.PositiveInfinity;
        }

        private bool IsIgnoredCollider(Collider candidate)
        {
            return candidate == null || _ownerColliders.Contains(candidate);
        }

        private void SpawnTracer(Vector3 start, Vector3 end)
        {
            if (!spawnTracers)
            {
                return;
            }

            BulletTracer.Spawn(start, end, tracerColor, tracerWidth, tracerDuration);
        }

        private void FinishReload()
        {
            _isReloading = false;

            int missingAmmo = magazineSize - _currentAmmo;
            int ammoToLoad = Mathf.Min(missingAmmo, reserveAmmo);
            _currentAmmo += ammoToLoad;
            reserveAmmo -= ammoToLoad;
            ReloadFinished?.Invoke();
        }
    }
}
