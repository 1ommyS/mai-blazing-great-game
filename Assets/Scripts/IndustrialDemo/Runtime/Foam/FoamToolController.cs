using System.Collections.Generic;
using IndustrialDemo.Breaching;
using IndustrialDemo.Core;
using UnityEngine;

namespace IndustrialDemo.Foam
{
    public class FoamToolController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Optional camera used for centered foam raycasts.")]
        private Camera aimCamera;

        [SerializeField, Tooltip("Optional transform used as the spawn origin for foam patches.")]
        private Transform foamSpawnPoint;

        [SerializeField, Tooltip("Optional shared material used by runtime foam patches.")]
        private Material foamMaterial;

        [SerializeField, Tooltip("Optional effect spawned when foam successfully lands on a valid target.")]
        private GameObject foamSplashVfxPrefab;

        [SerializeField, Tooltip("Optional effect spawned when foam seals a leak or blocks an entry.")]
        private GameObject foamResolveVfxPrefab;

        [SerializeField, Tooltip("Authored visual prefab used for cover foam patches.")]
        private GameObject coverVisualPrefab;

        [SerializeField, Tooltip("Authored visual prefab used for seal foam patches.")]
        private GameObject sealVisualPrefab;

        [SerializeField, Tooltip("Authored visual prefab used for block foam patches.")]
        private GameObject blockVisualPrefab;

        [SerializeField, Tooltip("Authored visual prefab used for slow-zone foam patches.")]
        private GameObject slowZoneVisualPrefab;

        [Header("Tool Stats")]
        [SerializeField, Min(0), Tooltip("Starting number of available foam charges.")]
        private int ammo = 8;

        [SerializeField, Min(0.5f), Tooltip("Maximum spray distance.")]
        private float maxDistance = 16f;

        [SerializeField, Min(0.5f), Tooltip("Default lifetime of newly spawned foam patches.")]
        private float patchLifetime = 18f;

        [SerializeField, Min(1f), Tooltip("Default HP assigned to newly spawned foam patches.")]
        private float patchHP = 65f;

        [SerializeField, Tooltip("Default size used for foam cover patches.")]
        private Vector3 patchScale = new(1.4f, 1.8f, 0.5f);

        [SerializeField, Min(0.05f), Tooltip("Delay between two foam shots.")]
        private float cooldown = 0.3f;

        [SerializeField, Tooltip("Valid surfaces for foam usage.")]
        private LayerMask validSurfaceMask = ~0;

        [SerializeField, Min(0.01f), Tooltip("Small offset used to pull spawned foam away from the hit surface.")]
        private float foamSpawnOffset = 0.08f;

        [SerializeField, Min(1), Tooltip("Maximum number of active foam patches at the same time.")]
        private int maxActivePatches = 4;

        [SerializeField, Range(0.1f, 1f), Tooltip("Movement multiplier used by slow-zone patches.")]
        private float slowZoneMultiplier = 0.5f;

        [SerializeField, Min(0f), Tooltip("Noise emitted when spraying foam.")]
        private float foamNoise = 1.25f;

        private readonly Queue<FoamPatch> _activePatches = new();
        private float _nextUseTime;
        private FoamMode _lastSpawnedMode = FoamMode.Cover;
        private FoamMode _previewMode = FoamMode.Cover;
        private bool _hasValidPreview;
        private string _previewActionLabel = "NO TARGET";
        private string _previewPurposeLabel = "Aim at a seal point, entry, floor marker, or cover anchor.";
        private IFoamHighlightTarget _activeHighlightTarget;

        public int CurrentAmmo => ammo;
        public FoamMode LastSpawnedMode => _lastSpawnedMode;
        public FoamMode PreviewMode => _previewMode;
        public bool HasValidPreview => _hasValidPreview;
        public string PreviewActionLabel => _previewActionLabel;
        public string PreviewPurposeLabel => _previewPurposeLabel;

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = GetComponent<Camera>();
            }
        }

        private void Update()
        {
            _hasValidPreview = TryGetTarget(out _, out FoamTargetInfo targetInfo);
            _previewMode = targetInfo.Mode;
            _previewActionLabel = targetInfo.ActionLabel;
            _previewPurposeLabel = targetInfo.PurposeLabel;
            SetHighlightTarget(targetInfo.HighlightTarget);
        }

        private void OnDisable()
        {
            SetHighlightTarget(null);
        }

        public bool TryUseTool()
        {
            if (Time.time < _nextUseTime || ammo <= 0)
            {
                return false;
            }

            if (!TryGetTarget(out RaycastHit hit, out FoamTargetInfo targetInfo))
            {
                return false;
            }

            _nextUseTime = Time.time + cooldown;
            ammo--;

            TrimPatchQueue();
            SpawnPatch(hit, targetInfo);
            SpawnEffect(foamSplashVfxPrefab, hit.point, hit.normal);
            if (targetInfo.Mode == FoamMode.Seal || targetInfo.Mode == FoamMode.Block)
            {
                SpawnEffect(foamResolveVfxPrefab, hit.point, hit.normal);
            }

            NoiseSystem.Emit(hit.point, foamNoise, gameObject, "foam_spray");
            return true;
        }

        private bool TryGetTarget(out RaycastHit hit, out FoamTargetInfo targetInfo)
        {
            hit = default;
            targetInfo = FoamTargetInfo.DefaultCover(patchScale, slowZoneMultiplier);

            if (aimCamera == null)
            {
                return false;
            }

            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out hit, maxDistance, validSurfaceMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            FoamSealPoint sealPoint = hit.collider.GetComponent<FoamSealPoint>() ?? hit.collider.GetComponentInParent<FoamSealPoint>();
            if (sealPoint != null && sealPoint.LinkedLeak != null)
            {
                targetInfo = new FoamTargetInfo(
                    FoamMode.Seal,
                    new Vector3(0.8f, 0.8f, 0.28f),
                    slowZoneMultiplier,
                    null,
                    sealPoint.LinkedLeak,
                    "SEAL LEAK",
                    "Foam this point to shut down the steam barrier.",
                    sealPoint);
                return true;
            }

            BreachableEntry entry = hit.collider.GetComponent<BreachableEntry>() ?? hit.collider.GetComponentInParent<BreachableEntry>();
            if (entry != null && entry.CanFoamBlock())
            {
                Vector3 blockSize = new(
                    Mathf.Max(1.2f, hit.collider.bounds.size.x),
                    Mathf.Max(2f, hit.collider.bounds.size.y),
                    0.45f);

                targetInfo = new FoamTargetInfo(
                    FoamMode.Block,
                    blockSize,
                    slowZoneMultiplier,
                    entry,
                    null,
                    "BLOCK ENTRY",
                    "Foam jams this route and cuts pressure for a short time.",
                    entry);
                return true;
            }

            FoamSlowZoneMarker slowMarker = hit.collider.GetComponent<FoamSlowZoneMarker>() ?? hit.collider.GetComponentInParent<FoamSlowZoneMarker>();
            if (slowMarker != null)
            {
                targetInfo = new FoamTargetInfo(
                    FoamMode.SlowZone,
                    slowMarker.ZoneSize,
                    slowMarker.SlowMultiplier,
                    null,
                    null,
                    "SLOW CHOKE",
                    "Foam the floor to slow anything crossing this lane.",
                    slowMarker);
                return true;
            }

            FoamCoverAnchor coverAnchor = hit.collider.GetComponent<FoamCoverAnchor>() ?? hit.collider.GetComponentInParent<FoamCoverAnchor>();
            if (coverAnchor != null)
            {
                targetInfo = new FoamTargetInfo(
                    FoamMode.Cover,
                    coverAnchor.CoverSize,
                    slowZoneMultiplier,
                    null,
                    null,
                    "MAKE COVER",
                    "Foam hardens into temporary cover on this anchor.",
                    coverAnchor);
                return true;
            }

            if (hit.normal.y > 0.7f)
            {
                targetInfo = new FoamTargetInfo(
                    FoamMode.SlowZone,
                    new Vector3(2.8f, 0.25f, 2.8f),
                    slowZoneMultiplier,
                    null,
                    null,
                    "COAT FLOOR",
                    "Spread foam on the floor to deny the choke.",
                    null);
            }

            return true;
        }

        private void SpawnPatch(RaycastHit hit, FoamTargetInfo targetInfo)
        {
            _lastSpawnedMode = targetInfo.Mode;

            Vector3 normal = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : Vector3.up;
            Vector3 spawnPosition = CalculateSpawnPosition(hit.point, normal, targetInfo);
            Vector3 forwardHint = aimCamera != null ? aimCamera.transform.forward : transform.forward;
            if (foamSpawnPoint != null)
            {
                spawnPosition = Vector3.Lerp(foamSpawnPoint.position, spawnPosition, 0.9f);
            }

            GameObject patchObject = new($"FoamPatch_{targetInfo.Mode}");
            patchObject.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

            FoamPatch patch = patchObject.AddComponent<FoamPatch>();
            patch.Initialize(
                targetInfo.Mode,
                gameObject,
                patchHP,
                patchLifetime,
                targetInfo.Size,
                normal,
                forwardHint,
                foamMaterial,
                ResolveVisualPrefab(targetInfo.Mode),
                hit.collider != null ? hit.collider.transform : null,
                targetInfo.LinkedEntry,
                targetInfo.LinkedLeak,
                targetInfo.SlowMultiplier,
                foamResolveVfxPrefab);

            _activePatches.Enqueue(patch);
        }

        private Vector3 CalculateSpawnPosition(Vector3 hitPoint, Vector3 normal, FoamTargetInfo targetInfo)
        {
            if (targetInfo.Mode == FoamMode.SlowZone || normal.y > 0.7f)
            {
                return hitPoint + normal * foamSpawnOffset;
            }

            float outwardDepth = foamSpawnOffset + Mathf.Max(0.08f, targetInfo.Size.z * 0.5f);
            return hitPoint + normal * outwardDepth;
        }

        private static void SpawnEffect(GameObject prefab, Vector3 position, Vector3 normal)
        {
            if (prefab == null)
            {
                return;
            }

            Quaternion rotation = normal.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(normal.normalized)
                : Quaternion.identity;

            Object.Instantiate(prefab, position, rotation);
        }

        private void TrimPatchQueue()
        {
            while (_activePatches.Count >= maxActivePatches)
            {
                FoamPatch oldest = _activePatches.Dequeue();
                if (oldest == null)
                {
                    continue;
                }

                oldest.ForceExpire();
                break;
            }
        }

        private GameObject ResolveVisualPrefab(FoamMode mode)
        {
            return mode switch
            {
                FoamMode.Cover => coverVisualPrefab,
                FoamMode.Seal => sealVisualPrefab,
                FoamMode.Block => blockVisualPrefab,
                FoamMode.SlowZone => slowZoneVisualPrefab,
                _ => null
            };
        }

        private void SetHighlightTarget(IFoamHighlightTarget nextTarget)
        {
            if (ReferenceEquals(_activeHighlightTarget, nextTarget))
            {
                return;
            }

            _activeHighlightTarget?.SetFoamHighlight(false);
            _activeHighlightTarget = nextTarget;
            _activeHighlightTarget?.SetFoamHighlight(true);
        }

        private readonly struct FoamTargetInfo
        {
            public FoamTargetInfo(
                FoamMode mode,
                Vector3 size,
                float slowMultiplier,
                BreachableEntry linkedEntry,
                SteamLeak linkedLeak,
                string actionLabel,
                string purposeLabel,
                IFoamHighlightTarget highlightTarget)
            {
                Mode = mode;
                Size = size;
                SlowMultiplier = slowMultiplier;
                LinkedEntry = linkedEntry;
                LinkedLeak = linkedLeak;
                ActionLabel = actionLabel;
                PurposeLabel = purposeLabel;
                HighlightTarget = highlightTarget;
            }

            public FoamMode Mode { get; }
            public Vector3 Size { get; }
            public float SlowMultiplier { get; }
            public BreachableEntry LinkedEntry { get; }
            public SteamLeak LinkedLeak { get; }
            public string ActionLabel { get; }
            public string PurposeLabel { get; }
            public IFoamHighlightTarget HighlightTarget { get; }

            public static FoamTargetInfo DefaultCover(Vector3 size, float slowMultiplier)
            {
                return new FoamTargetInfo(
                    FoamMode.Cover,
                    size,
                    slowMultiplier,
                    null,
                    null,
                    "NO TARGET",
                    "Aim at a seal point, entry, floor marker, or cover anchor.",
                    null);
            }
        }
    }
}
