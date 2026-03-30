using IndustrialDemo.Breaching;
using IndustrialDemo.Combat;
using IndustrialDemo.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IndustrialDemo.Foam
{
    public class FoamPatch : MonoBehaviour, IShotDamageReceiver
    {
        [Header("State")]
        [SerializeField, Tooltip("Active mode used by this patch.")]
        private FoamMode foamMode = FoamMode.Cover;

        [SerializeField, Tooltip("Owner that created this patch.")]
        private GameObject owner;

        [SerializeField, Min(1f), Tooltip("Current hit points remaining on this patch.")]
        private float patchHP = 50f;

        [SerializeField, Min(0.1f), Tooltip("Total lifetime before the patch expires.")]
        private float lifetime = 12f;

        [SerializeField, Tooltip("World-space footprint used by this patch.")]
        private Vector3 footprintSize = Vector3.one;

        [SerializeField, Tooltip("Surface normal used when the patch was spawned.")]
        private Vector3 surfaceNormal = Vector3.up;

        [SerializeField, Tooltip("Forward hint captured when the patch was spawned so floor-placed foam can face the lane cleanly.")]
        private Vector3 forwardHint = Vector3.forward;

        [SerializeField, Tooltip("Optional anchor transform followed by the patch.")]
        private Transform anchorTransform;

        [SerializeField, Tooltip("Optional linked breaching entry blocked by this patch.")]
        private BreachableEntry linkedEntry;

        [SerializeField, Tooltip("Optional linked leak sealed by this patch.")]
        private SteamLeak linkedLeak;

        [SerializeField, Range(0.1f, 1f), Tooltip("Movement multiplier applied by slow-zone patches.")]
        private float slowMultiplier = 0.5f;

        [SerializeField, Tooltip("Optional effect spawned when the patch expires or is destroyed.")]
        private GameObject breakVfxPrefab;

        private Vector3 _anchorLocalPosition;
        private Quaternion _anchorLocalRotation;
        private float _remainingLifetime;
        private bool _expired;
        private readonly HashSet<DemoFirstPersonMotor> _slowedMotors = new();
        private readonly HashSet<Actors.DemoEnemyActor> _slowedEnemies = new();

        public void Initialize(
            FoamMode mode,
            GameObject patchOwner,
            float hp,
            float patchLifetime,
            Vector3 size,
            Vector3 normal,
            Vector3 spawnForwardHint,
            Material foamMaterial,
            GameObject visualPrefab,
            Transform anchor,
            BreachableEntry entry,
            SteamLeak leak,
            float zoneSlowMultiplier,
            GameObject breakEffectPrefab)
        {
            foamMode = mode;
            owner = patchOwner;
            patchHP = Mathf.Max(1f, hp);
            lifetime = Mathf.Max(0.1f, patchLifetime);
            footprintSize = size;
            surfaceNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            forwardHint = spawnForwardHint.sqrMagnitude > 0.001f ? spawnForwardHint.normalized : Vector3.forward;
            anchorTransform = anchor;
            linkedEntry = entry;
            linkedLeak = leak;
            slowMultiplier = Mathf.Clamp(zoneSlowMultiplier, 0.1f, 1f);
            breakVfxPrefab = breakEffectPrefab;
            _remainingLifetime = lifetime;

            transform.rotation = GetAlignedRotation();

            if (anchorTransform != null)
            {
                _anchorLocalPosition = anchorTransform.InverseTransformPoint(transform.position);
                _anchorLocalRotation = Quaternion.Inverse(anchorTransform.rotation) * transform.rotation;
            }

            ConfigureShape(foamMaterial, visualPrefab);
            ApplyModeSetup();
        }

        private void Update()
        {
            if (_expired)
            {
                return;
            }

            if (anchorTransform == null && (foamMode == FoamMode.Block || foamMode == FoamMode.Seal))
            {
                ForceExpire();
                return;
            }

            if (anchorTransform != null)
            {
                transform.SetPositionAndRotation(
                    anchorTransform.TransformPoint(_anchorLocalPosition),
                    anchorTransform.rotation * _anchorLocalRotation);
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                ForceExpire();
            }
        }

        public void ReceiveShotDamage(ShotImpactContext context)
        {
            if (_expired)
            {
                return;
            }

            patchHP -= Mathf.Max(0f, context.Damage);
            if (patchHP <= 0f)
            {
                ForceExpire();
            }
        }

        public void ForceExpire()
        {
            if (_expired)
            {
                return;
            }

            _expired = true;

            if (linkedEntry != null)
            {
                linkedEntry.SetFoamBlocked(false);
            }

            if (linkedLeak != null)
            {
                linkedLeak.SetSealed(false);
            }

            if (breakVfxPrefab != null)
            {
                Instantiate(breakVfxPrefab, transform.position + transform.up * Mathf.Max(0.08f, footprintSize.y * 0.2f), Quaternion.LookRotation(surfaceNormal));
            }

            Destroy(gameObject);
        }

        private void ApplyModeSetup()
        {
            if (linkedEntry != null && foamMode == FoamMode.Block)
            {
                linkedEntry.SetFoamBlocked(true);
            }

            if (linkedLeak != null && foamMode == FoamMode.Seal)
            {
                linkedLeak.SetSealed(true);
            }
        }

        private void ConfigureShape(Material foamMaterial, GameObject visualPrefab)
        {
            bool hasAuthoredVisual = TryCreateAuthoredVisual(visualPrefab);

            switch (foamMode)
            {
                case FoamMode.Cover:
                    ConfigureCover(foamMaterial, !hasAuthoredVisual);
                    break;
                case FoamMode.Seal:
                    ConfigureSeal(foamMaterial, !hasAuthoredVisual);
                    break;
                case FoamMode.Block:
                    ConfigureBlock(foamMaterial, !hasAuthoredVisual);
                    break;
                case FoamMode.SlowZone:
                    ConfigureSlowZone(foamMaterial, !hasAuthoredVisual);
                    break;
            }
        }

        private bool TryCreateAuthoredVisual(GameObject visualPrefab)
        {
            if (visualPrefab == null)
            {
                return false;
            }

            GameObject visualInstance = Instantiate(visualPrefab, transform);
            visualInstance.name = "Visual";
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = GetAuthoredVisualScale();
            return true;
        }

        private void ConfigureCover(Material foamMaterial, bool addProceduralVisuals)
        {
            Vector3 size = new(
                Mathf.Max(0.9f, footprintSize.x),
                Mathf.Max(1.15f, footprintSize.y),
                Mathf.Max(0.72f, footprintSize.z));

            if (addProceduralVisuals)
            {
                Material decalMaterial = CreateDecalMaterial(foamMaterial, 0.28f, 2.1f);
                Material topMaterial = CreateTopMaterial(foamMaterial);
                CreateFoamDecal("CoverDecal", new Vector3(0f, -size.y * 0.08f, -size.z * 0.55f), new Vector3(size.x * 1.08f, size.y * 0.92f, 1f), decalMaterial);
                CreateFoamSlab("CoverCore", Vector3.zero, size, foamMaterial);
                CreateFoamSlab("CoverTopCap", new Vector3(0f, size.y * 0.34f, size.z * 0.04f), new Vector3(size.x * 0.88f, size.y * 0.14f, size.z * 0.78f), topMaterial != null ? topMaterial : foamMaterial);
                CreateFoamLump("CoverShoulderL", new Vector3(-size.x * 0.26f, size.y * 0.06f, size.z * 0.08f), new Vector3(size.x * 0.44f, size.y * 0.32f, size.z * 0.78f), foamMaterial);
                CreateFoamLump("CoverShoulderR", new Vector3(size.x * 0.24f, -size.y * 0.02f, -size.z * 0.04f), new Vector3(size.x * 0.36f, size.y * 0.26f, size.z * 0.72f), foamMaterial);
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, 0f, size.z * 0.04f);

            SurfaceMaterial surface = gameObject.AddComponent<SurfaceMaterial>();
            surface.ConfigureRuntime(SurfaceMaterialType.Drywall, 45f, false, 0f, 0.75f, true, patchHP);
        }

        private void ConfigureSeal(Material foamMaterial, bool addProceduralVisuals)
        {
            Vector3 size = new(
                Mathf.Max(0.55f, footprintSize.x),
                Mathf.Max(0.55f, footprintSize.y),
                Mathf.Max(0.42f, footprintSize.z));

            if (addProceduralVisuals)
            {
                Material decalMaterial = CreateDecalMaterial(foamMaterial, 0.34f, 1.7f);
                Material topMaterial = CreateTopMaterial(foamMaterial);
                CreateFoamDecal("SealDecal", new Vector3(0f, 0f, -size.z * 0.45f), new Vector3(size.x * 1.05f, size.y * 1.05f, 1f), decalMaterial);
                CreateFoamSlab("SealCore", Vector3.zero, size, foamMaterial);
                CreateFoamSlab("SealCap", new Vector3(0f, 0f, size.z * 0.12f), new Vector3(size.x * 0.74f, size.y * 0.74f, size.z * 0.52f), topMaterial != null ? topMaterial : foamMaterial);
                CreateFoamLump("SealBurst", new Vector3(0f, 0f, size.z * 0.18f), new Vector3(size.x * 0.58f, size.y * 0.5f, size.z * 0.42f), foamMaterial);
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, 0f, size.z * 0.04f);

            SurfaceMaterial surface = gameObject.AddComponent<SurfaceMaterial>();
            surface.ConfigureRuntime(SurfaceMaterialType.Drywall, 60f, false, 0f, 0.58f, true, patchHP * 0.85f);
        }

        private void ConfigureBlock(Material foamMaterial, bool addProceduralVisuals)
        {
            Vector3 size = new(
                Mathf.Max(1.1f, footprintSize.x),
                Mathf.Max(2f, footprintSize.y),
                Mathf.Max(0.92f, footprintSize.z));

            if (addProceduralVisuals)
            {
                Material decalMaterial = CreateDecalMaterial(foamMaterial, 0.4f, 2.6f);
                Material topMaterial = CreateTopMaterial(foamMaterial);
                CreateFoamDecal("BlockDecal", new Vector3(0f, 0f, -size.z * 0.58f), new Vector3(size.x * 1.04f, size.y * 1.02f, 1f), decalMaterial);
                CreateFoamSlab("BlockLower", new Vector3(0f, -size.y * 0.16f, 0f), new Vector3(size.x, size.y * 0.44f, size.z), foamMaterial);
                CreateFoamSlab("BlockUpper", new Vector3(0f, size.y * 0.18f, size.z * 0.04f), new Vector3(size.x * 0.92f, size.y * 0.34f, size.z * 0.92f), topMaterial != null ? topMaterial : foamMaterial);
                CreateFoamLump("BlockBulgeL", new Vector3(-size.x * 0.22f, -size.y * 0.02f, size.z * 0.1f), new Vector3(size.x * 0.34f, size.y * 0.28f, size.z * 0.66f), foamMaterial);
                CreateFoamLump("BlockBulgeR", new Vector3(size.x * 0.2f, size.y * 0.08f, -size.z * 0.04f), new Vector3(size.x * 0.3f, size.y * 0.24f, size.z * 0.6f), foamMaterial);
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, 0f, size.z * 0.04f);

            SurfaceMaterial surface = gameObject.AddComponent<SurfaceMaterial>();
            surface.ConfigureRuntime(SurfaceMaterialType.Drywall, 70f, false, 0f, 0.6f, true, patchHP);
        }

        private void ConfigureSlowZone(Material foamMaterial, bool addProceduralVisuals)
        {
            Vector3 size = new(
                Mathf.Max(1.2f, footprintSize.x),
                Mathf.Max(0.1f, footprintSize.y),
                Mathf.Max(1.2f, footprintSize.z));

            if (addProceduralVisuals)
            {
                Material decalMaterial = CreateDecalMaterial(foamMaterial, 0.26f, 1.8f);
                CreateFoamDecal("SlowZoneDecalOuter", new Vector3(0f, 0f, 0f), new Vector3(size.x * 1.02f, size.z * 1.02f, 1f), decalMaterial);
                CreateFoamDecal("SlowZoneDecalInner", new Vector3(0f, 0.002f, 0f), new Vector3(size.x * 0.66f, size.z * 0.66f, 1f), decalMaterial);
                CreateFoamDisc("SlowZoneDisc", Vector3.zero, new Vector3(size.x, size.y, size.z), foamMaterial);
                CreateFoamDisc("SlowZoneDiscInner", new Vector3(0f, 0.01f, 0f), new Vector3(size.x * 0.72f, size.y * 0.8f, size.z * 0.72f), foamMaterial);
            }

            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
        }

        private GameObject CreateFoamLump(string name, Vector3 localPosition, Vector3 localScale, Material foamMaterial)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = name;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            Destroy(visual.GetComponent<Collider>());
            ApplyFoamMaterial(visual, foamMaterial);
            return visual;
        }

        private GameObject CreateFoamDisc(string name, Vector3 localPosition, Vector3 localScale, Material foamMaterial)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = name;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(localScale.x, localScale.z * 0.5f, Mathf.Max(0.02f, localScale.y));
            Destroy(visual.GetComponent<Collider>());
            ApplyFoamMaterial(visual, foamMaterial);
            return visual;
        }

        private GameObject CreateFoamSlab(string name, Vector3 localPosition, Vector3 localScale, Material foamMaterial)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            Destroy(visual.GetComponent<Collider>());
            ApplyFoamMaterial(visual, foamMaterial);
            return visual;
        }

        private static void ApplyFoamMaterial(GameObject visual, Material foamMaterial)
        {
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null && foamMaterial != null)
            {
                renderer.sharedMaterial = foamMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private GameObject CreateFoamDecal(string name, Vector3 localPosition, Vector3 localScale, Material decalMaterial)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = name;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            Destroy(visual.GetComponent<Collider>());

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null && decalMaterial != null)
            {
                renderer.sharedMaterial = decalMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return visual;
        }

        private static Material CreateDecalMaterial(Material sourceMaterial, float alpha, float emissiveIntensity)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            Material decalMaterial = new(sourceMaterial);

            if (decalMaterial.HasProperty("_SurfaceType"))
            {
                decalMaterial.SetFloat("_SurfaceType", 1f);
            }

            if (decalMaterial.HasProperty("_BlendMode"))
            {
                decalMaterial.SetFloat("_BlendMode", 0f);
            }

            if (decalMaterial.HasProperty("_ZWrite"))
            {
                decalMaterial.SetFloat("_ZWrite", 0f);
            }

            if (decalMaterial.HasProperty("_TransparentZWrite"))
            {
                decalMaterial.SetFloat("_TransparentZWrite", 0f);
            }

            if (decalMaterial.HasProperty("_CullMode"))
            {
                decalMaterial.SetFloat("_CullMode", 0f);
            }

            if (decalMaterial.HasProperty("_TransparentCullMode"))
            {
                decalMaterial.SetFloat("_TransparentCullMode", 0f);
            }

            if (decalMaterial.HasProperty("_BaseColor"))
            {
                Color baseColor = decalMaterial.GetColor("_BaseColor");
                baseColor.a = alpha;
                decalMaterial.SetColor("_BaseColor", baseColor);
            }

            if (decalMaterial.HasProperty("_Color"))
            {
                Color legacyColor = decalMaterial.GetColor("_Color");
                legacyColor.a = alpha;
                decalMaterial.SetColor("_Color", legacyColor);
            }

            if (decalMaterial.HasProperty("_UseEmissiveIntensity"))
            {
                decalMaterial.SetFloat("_UseEmissiveIntensity", 1f);
            }

            if (decalMaterial.HasProperty("_EmissiveColor"))
            {
                Color emissive = decalMaterial.GetColor("_EmissiveColor");
                if (emissive.maxColorComponent < 0.01f)
                {
                    emissive = new Color(0.12f, 0.2f, 0.24f, 1f);
                }

                decalMaterial.SetColor("_EmissiveColor", emissive);
            }

            if (decalMaterial.HasProperty("_EmissiveIntensity"))
            {
                decalMaterial.SetFloat("_EmissiveIntensity", emissiveIntensity);
            }

            decalMaterial.renderQueue = 3000;
            return decalMaterial;
        }

        private static Material CreateTopMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            Material topMaterial = new(sourceMaterial);
            if (topMaterial.HasProperty("_BaseColor"))
            {
                Color color = topMaterial.GetColor("_BaseColor");
                topMaterial.SetColor("_BaseColor", Color.Lerp(color, Color.white, 0.18f));
            }
            else if (topMaterial.HasProperty("_Color"))
            {
                Color color = topMaterial.GetColor("_Color");
                topMaterial.SetColor("_Color", Color.Lerp(color, Color.white, 0.18f));
            }

            return topMaterial;
        }

        private Vector3 GetAuthoredVisualScale()
        {
            return foamMode switch
            {
                FoamMode.Cover => new Vector3(
                    Mathf.Max(0.9f, footprintSize.x),
                    Mathf.Max(1.15f, footprintSize.y),
                    Mathf.Max(0.72f, footprintSize.z)),
                FoamMode.Seal => new Vector3(
                    Mathf.Max(0.55f, footprintSize.x),
                    Mathf.Max(0.55f, footprintSize.y),
                    Mathf.Max(0.42f, footprintSize.z)),
                FoamMode.Block => new Vector3(
                    Mathf.Max(1.1f, footprintSize.x),
                    Mathf.Max(2f, footprintSize.y),
                    Mathf.Max(0.92f, footprintSize.z)),
                FoamMode.SlowZone => new Vector3(
                    Mathf.Max(1.2f, footprintSize.x),
                    Mathf.Max(0.1f, footprintSize.y),
                    Mathf.Max(1.2f, footprintSize.z)),
                _ => Vector3.one
            };
        }

        private Quaternion GetAlignedRotation()
        {
            if (foamMode == FoamMode.SlowZone)
            {
                return Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            }

            if (surfaceNormal.y > 0.65f)
            {
                Vector3 planarForward = Vector3.ProjectOnPlane(forwardHint, Vector3.up);
                if (planarForward.sqrMagnitude < 0.001f)
                {
                    planarForward = Vector3.forward;
                }

                return Quaternion.LookRotation(planarForward.normalized, Vector3.up);
            }

            Vector3 forward = surfaceNormal.sqrMagnitude > 0.001f ? -surfaceNormal : transform.forward;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(forward, up);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (foamMode != FoamMode.SlowZone)
            {
                return;
            }

            ApplySlow(other, true);
        }

        private void OnTriggerStay(Collider other)
        {
            if (foamMode != FoamMode.SlowZone)
            {
                return;
            }

            ApplySlow(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (foamMode != FoamMode.SlowZone)
            {
                return;
            }

            ApplySlow(other, false);
        }

        private void ApplySlow(Collider other, bool apply)
        {
            DemoFirstPersonMotor motor = other.GetComponent<DemoFirstPersonMotor>() ?? other.GetComponentInParent<DemoFirstPersonMotor>();
            if (motor != null)
            {
                if (apply)
                {
                    motor.SetMovementSlow(this, slowMultiplier);
                    _slowedMotors.Add(motor);
                }
                else
                {
                    motor.ClearMovementSlow(this);
                    _slowedMotors.Remove(motor);
                }
            }

            Actors.DemoEnemyActor enemyActor = other.GetComponent<Actors.DemoEnemyActor>() ?? other.GetComponentInParent<Actors.DemoEnemyActor>();
            if (enemyActor == null)
            {
                return;
            }

            if (apply)
            {
                enemyActor.SetMovementSlow(this, slowMultiplier);
                _slowedEnemies.Add(enemyActor);
            }
            else
            {
                enemyActor.ClearMovementSlow(this);
                _slowedEnemies.Remove(enemyActor);
            }
        }

        private void OnDestroy()
        {
            foreach (DemoFirstPersonMotor motor in _slowedMotors)
            {
                if (motor != null)
                {
                    motor.ClearMovementSlow(this);
                }
            }

            _slowedMotors.Clear();

            foreach (Actors.DemoEnemyActor enemyActor in _slowedEnemies)
            {
                if (enemyActor != null)
                {
                    enemyActor.ClearMovementSlow(this);
                }
            }

            _slowedEnemies.Clear();
        }
    }
}
