using System;
using IndustrialDemo.Core;
using UnityEngine;

namespace IndustrialDemo.Combat
{
    public class SurfaceMaterial : MonoBehaviour, IShotDamageReceiver
    {
        [Header("Material")]
        [SerializeField, Tooltip("Gameplay material type used by penetration and ricochet logic.")]
        private SurfaceMaterialType materialType = SurfaceMaterialType.Reinforced;

        [SerializeField, Min(0f), Tooltip("How much penetration power is consumed when a shot passes through this surface.")]
        private float penetrationCost = 100f;

        [SerializeField, Tooltip("Whether shallow-angle hits are allowed to ricochet from this surface.")]
        private bool canRicochet;

        [SerializeField, Range(0f, 89f), Tooltip("Minimum impact angle from the surface normal required before ricochet is allowed. Higher values mean only very shallow hits can bounce.")]
        private float ricochetMinAngle = 72f;

        [SerializeField, Range(0f, 1f), Tooltip("Deterministic ricochet strength used to gate weak glancing hits. Higher values make ricochet more reliable on this material.")]
        private float ricochetChance = 0.5f;

        [SerializeField, Min(0f), Tooltip("Optional multiplier that affects how expensive thick surfaces feel in gameplay.")]
        private float thicknessMultiplier = 1f;

        [SerializeField, Min(0f), Tooltip("Damage multiplier applied to shots that successfully pass through this surface.")]
        private float damageMultiplierThroughSurface = 0.85f;

        [Header("Impact Feedback")]
        [SerializeField, Tooltip("Optional object spawned on every regular impact against this material.")]
        private GameObject impactVfxPrefab;

        [SerializeField, Tooltip("Optional object spawned when a shot fully penetrates this material.")]
        private GameObject penetrationVfxPrefab;

        [SerializeField, Tooltip("Optional object spawned when this material produces a ricochet.")]
        private GameObject ricochetVfxPrefab;

        [Header("Destruction")]
        [SerializeField, Tooltip("If enabled, repeated shot damage can break this piece of cover.")]
        private bool isDestructible;

        [SerializeField, Min(0f), Tooltip("Current intact HP of this cover piece.")]
        private float coverHP = 100f;

        [SerializeField, Tooltip("Optional GameObject enabled after the cover is destroyed.")]
        private GameObject destroyedReplacement;

        [SerializeField, Tooltip("Renderers disabled when the cover breaks.")]
        private Renderer[] intactRenderers = Array.Empty<Renderer>();

        [SerializeField, Tooltip("Colliders disabled when the cover breaks.")]
        private Collider[] intactColliders = Array.Empty<Collider>();

        [SerializeField, Tooltip("Optional object spawned at the impact point when the cover breaks.")]
        private GameObject destructionVfxPrefab;

        [SerializeField, Min(0f), Tooltip("Noise emitted when this cover breaks.")]
        private float destructionNoise = 7f;

        private float _currentCoverHp;
        private bool _isDestroyed;

        public SurfaceMaterialType MaterialType => materialType;
        public float PenetrationCost => penetrationCost * Mathf.Max(0.01f, thicknessMultiplier);
        public bool CanRicochet => canRicochet;
        public float RicochetMinAngle => ricochetMinAngle;
        public float RicochetChance => ricochetChance;
        public float DamageMultiplierThroughSurface => damageMultiplierThroughSurface;
        public bool IsDestructible => isDestructible;
        public bool IsDestroyed => _isDestroyed;

        public static SurfaceMaterialDefaults DefaultReinforced { get; } = new(
            SurfaceMaterialType.Reinforced,
            penetrationCost: 999f,
            canRicochet: true,
            ricochetMinAngle: 76f,
            ricochetChance: 0.3f,
            damageMultiplierThroughSurface: 0.28f);

        private void Awake()
        {
            _currentCoverHp = coverHP;

            if (destroyedReplacement != null)
            {
                destroyedReplacement.SetActive(false);
            }
        }

        public void ReceiveShotDamage(ShotImpactContext context)
        {
            if (!isDestructible || _isDestroyed)
            {
                return;
            }

            _currentCoverHp -= Mathf.Max(0f, context.Damage);
            if (_currentCoverHp > 0f)
            {
                return;
            }

            Break(context.Hit.point);
        }

        public bool TryGetRicochet(Vector3 incomingDirection, Vector3 hitNormal, out Vector3 reflectedDirection)
        {
            reflectedDirection = default;
            if (!canRicochet)
            {
                return false;
            }

            float impactAngle = Vector3.Angle(-incomingDirection.normalized, hitNormal.normalized);
            if (impactAngle < ricochetMinAngle)
            {
                return false;
            }

            float glancingFactor = Mathf.InverseLerp(ricochetMinAngle, 89f, impactAngle);
            if (glancingFactor < 1f - ricochetChance)
            {
                return false;
            }

            reflectedDirection = Vector3.Reflect(incomingDirection.normalized, hitNormal.normalized);
            return reflectedDirection.sqrMagnitude > 0f;
        }

        public static SurfaceMaterialDefaults Resolve(Collider collider)
        {
            if (collider == null)
            {
                return DefaultReinforced;
            }

            SurfaceMaterial surface = collider.GetComponent<SurfaceMaterial>();
            if (surface == null)
            {
                surface = collider.GetComponentInParent<SurfaceMaterial>();
            }

            if (surface != null)
            {
                return new SurfaceMaterialDefaults(
                    surface.MaterialType,
                    surface.PenetrationCost,
                    surface.CanRicochet,
                    surface.RicochetMinAngle,
                    surface.RicochetChance,
                    surface.DamageMultiplierThroughSurface,
                    surface);
            }

            return ResolveFallback(collider);
        }

        public void SpawnImpactFeedback(Vector3 position, Vector3 normal)
        {
            SpawnEffect(impactVfxPrefab, position, normal);
        }

        public void SpawnPenetrationFeedback(Vector3 position, Vector3 normal)
        {
            SpawnEffect(penetrationVfxPrefab != null ? penetrationVfxPrefab : impactVfxPrefab, position, normal);
        }

        public void SpawnRicochetFeedback(Vector3 position, Vector3 normal)
        {
            SpawnEffect(ricochetVfxPrefab != null ? ricochetVfxPrefab : impactVfxPrefab, position, normal);
        }

        public void ConfigureRuntime(
            SurfaceMaterialType runtimeMaterialType,
            float penetration,
            bool allowRicochet,
            float ricochetAngle,
            float damageMultiplier,
            bool destructible,
            float destructibleHp)
        {
            materialType = runtimeMaterialType;
            penetrationCost = Mathf.Max(0f, penetration);
            canRicochet = allowRicochet;
            ricochetMinAngle = Mathf.Clamp(ricochetAngle, 0f, 89f);
            ricochetChance = GetFallbackRicochetChance(runtimeMaterialType, allowRicochet);
            damageMultiplierThroughSurface = Mathf.Max(0f, damageMultiplier);
            isDestructible = destructible;
            coverHP = Mathf.Max(0f, destructibleHp);
            _currentCoverHp = coverHP;
            _isDestroyed = false;
        }

        public void AssignImpactFx(GameObject impactPrefab, GameObject penetrationPrefab, GameObject ricochetPrefab, GameObject destructionPrefab = null)
        {
            impactVfxPrefab = impactPrefab;
            penetrationVfxPrefab = penetrationPrefab;
            ricochetVfxPrefab = ricochetPrefab;
            destructionVfxPrefab = destructionPrefab;
        }

        private void Break(Vector3 hitPoint)
        {
            _isDestroyed = true;

            Renderer[] renderersToDisable = intactRenderers != null && intactRenderers.Length > 0
                ? intactRenderers
                : GetComponentsInChildren<Renderer>(includeInactive: false);

            Collider[] collidersToDisable = intactColliders != null && intactColliders.Length > 0
                ? intactColliders
                : GetComponentsInChildren<Collider>(includeInactive: false);

            foreach (Renderer intactRenderer in renderersToDisable)
            {
                if (intactRenderer != null)
                {
                    intactRenderer.enabled = false;
                }
            }

            foreach (Collider intactCollider in collidersToDisable)
            {
                if (intactCollider != null)
                {
                    intactCollider.enabled = false;
                }
            }

            if (destroyedReplacement != null)
            {
                destroyedReplacement.SetActive(true);
            }

            if (destructionVfxPrefab != null)
            {
                Instantiate(destructionVfxPrefab, hitPoint, Quaternion.identity);
            }

            NoiseSystem.Emit(hitPoint, destructionNoise, gameObject, "cover_break");
        }

        private static void SpawnEffect(GameObject effectPrefab, Vector3 position, Vector3 normal)
        {
            if (effectPrefab == null)
            {
                return;
            }

            Quaternion rotation = normal.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(normal.normalized)
                : Quaternion.identity;

            Instantiate(effectPrefab, position, rotation);
        }

        private static float GetFallbackRicochetChance(SurfaceMaterialType materialType, bool allowRicochet)
        {
            float baseChance = materialType switch
            {
                SurfaceMaterialType.Glass => 0.18f,
                SurfaceMaterialType.Drywall => 0.08f,
                SurfaceMaterialType.Wood => 0.24f,
                SurfaceMaterialType.Steel => 0.92f,
                SurfaceMaterialType.Reinforced => 0.4f,
                _ => 0.16f
            };

            return allowRicochet ? baseChance : Mathf.Min(baseChance, 0.12f);
        }

        private static readonly string[] GlassKeywords = { "glass", "window", "visor", "pane" };
        private static readonly string[] DrywallKeywords = { "drywall", "panel", "sheet", "service", "thin", "weak" };
        private static readonly string[] WoodKeywords = { "wood", "crate", "pallet", "box", "timber" };
        private static readonly string[] SteelKeywords = { "steel", "metal", "container", "shutter", "hatch", "door", "plate", "locker", "cabinet" };
        private static readonly string[] ReinforcedKeywords = { "reinforced", "concrete", "bulkhead", "pillar", "column", "barrier", "wall", "fence" };

        private static SurfaceMaterialDefaults ResolveFallback(Collider collider)
        {
            SurfaceMaterialType inferredType = InferMaterialType(collider);
            return inferredType switch
            {
                SurfaceMaterialType.Glass => new SurfaceMaterialDefaults(inferredType, 20f, true, 83f, 0.18f, 0.42f),
                SurfaceMaterialType.Drywall => new SurfaceMaterialDefaults(inferredType, 26f, true, 86f, 0.08f, 0.76f),
                SurfaceMaterialType.Wood => new SurfaceMaterialDefaults(inferredType, 44f, true, 78f, 0.24f, 0.7f),
                SurfaceMaterialType.Steel => new SurfaceMaterialDefaults(inferredType, 240f, true, 58f, 0.92f, 0.32f),
                SurfaceMaterialType.Reinforced => new SurfaceMaterialDefaults(inferredType, 999f, true, 72f, 0.4f, 0.26f),
                _ => new SurfaceMaterialDefaults(SurfaceMaterialType.DefaultSolid, 90f, true, 80f, 0.16f, 0.55f)
            };
        }

        private static SurfaceMaterialType InferMaterialType(Collider collider)
        {
            string lookup = BuildLookupString(collider);

            if (ContainsKeyword(lookup, GlassKeywords))
            {
                return SurfaceMaterialType.Glass;
            }

            if (ContainsKeyword(lookup, DrywallKeywords))
            {
                return SurfaceMaterialType.Drywall;
            }

            if (ContainsKeyword(lookup, WoodKeywords))
            {
                return SurfaceMaterialType.Wood;
            }

            if (ContainsKeyword(lookup, SteelKeywords))
            {
                return SurfaceMaterialType.Steel;
            }

            if (ContainsKeyword(lookup, ReinforcedKeywords))
            {
                return SurfaceMaterialType.Reinforced;
            }

            return SurfaceMaterialType.Reinforced;
        }

        private static string BuildLookupString(Collider collider)
        {
            if (collider == null)
            {
                return string.Empty;
            }

            string text = collider.name + " " + collider.gameObject.name + " " + collider.tag;

            if (collider.sharedMaterial != null)
            {
                text += " " + collider.sharedMaterial.name;
            }

            Renderer renderer = collider.GetComponent<Renderer>() ?? collider.GetComponentInChildren<Renderer>() ?? collider.GetComponentInParent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                text += " " + renderer.sharedMaterial.name;
            }

            return text.ToLowerInvariant();
        }

        private static bool ContainsKeyword(string source, string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (source.Contains(keywords[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly struct SurfaceMaterialDefaults
    {
        public SurfaceMaterialDefaults(
            SurfaceMaterialType materialType,
            float penetrationCost,
            bool canRicochet,
            float ricochetMinAngle,
            float ricochetChance,
            float damageMultiplierThroughSurface,
            SurfaceMaterial surfaceComponent = null)
        {
            MaterialType = materialType;
            PenetrationCost = penetrationCost;
            CanRicochet = canRicochet;
            RicochetMinAngle = ricochetMinAngle;
            RicochetChance = ricochetChance;
            DamageMultiplierThroughSurface = damageMultiplierThroughSurface;
            SurfaceComponent = surfaceComponent;
        }

        public SurfaceMaterialType MaterialType { get; }
        public float PenetrationCost { get; }
        public bool CanRicochet { get; }
        public float RicochetMinAngle { get; }
        public float RicochetChance { get; }
        public float DamageMultiplierThroughSurface { get; }
        public SurfaceMaterial SurfaceComponent { get; }

        public static implicit operator SurfaceMaterialDefaults(SurfaceMaterial surface)
        {
            return new SurfaceMaterialDefaults(
                surface.MaterialType,
                surface.PenetrationCost,
                surface.CanRicochet,
                surface.RicochetMinAngle,
                surface.RicochetChance,
                surface.DamageMultiplierThroughSurface,
                surface);
        }
    }
}
