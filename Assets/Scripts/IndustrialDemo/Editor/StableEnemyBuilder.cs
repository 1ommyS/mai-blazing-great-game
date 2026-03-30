using System.IO;
using IndustrialDemo.Actors;
using IndustrialDemo.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndustrialDemo.Editor
{
    public static class StableEnemyBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs/IndustrialDemo/Actors";
        private const string PrefabPath = PrefabFolder + "/PFB_IndustrialEnemy.prefab";
        private const string ScenePath = "Assets/Industrial_Demo.unity/Industrial_Demo.unity";

        private readonly struct EnemySpec
        {
            public EnemySpec(string sceneName, Vector3 fallbackPosition, float yaw, DemoEnemyActor.EncounterRole role, DemoEnemyActor.CombatArchetype archetype, Vector3[] tacticalOffsets)
            {
                SceneName = sceneName;
                FallbackPosition = fallbackPosition;
                Yaw = yaw;
                Role = role;
                Archetype = archetype;
                TacticalOffsets = tacticalOffsets;
            }

            public string SceneName { get; }
            public Vector3 FallbackPosition { get; }
            public float Yaw { get; }
            public DemoEnemyActor.EncounterRole Role { get; }
            public DemoEnemyActor.CombatArchetype Archetype { get; }
            public Vector3[] TacticalOffsets { get; }
        }

        private static readonly EnemySpec[] EnemySpecs =
        {
            new("Enemy_Z02_Left", new Vector3(-5.3f, 0f, 8.9f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Central_Balcony", new Vector3(-1.8f, 0f, 6.6f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Central_Flank", new Vector3(7.6f, 0f, 12.4f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Rifleman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Breach_Guard", new Vector3(0.1f, 0f, 34.7f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Z03_Inner", new Vector3(1.8f, 0f, 35.2f), 180f, DemoEnemyActor.EncounterRole.Pusher, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Pusher)),
            new("Enemy_Breach_Reactor", new Vector3(3.6f, 0f, 39.2f), 180f, DemoEnemyActor.EncounterRole.Pusher, DemoEnemyActor.CombatArchetype.Enforcer, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Pusher)),
            new("Enemy_Steam_Choke", new Vector3(0.8f, 0f, 58.7f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Steam_FoamTrap", new Vector3(4.8f, 0f, 55.5f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Final_A", new Vector3(-4.8f, 0f, 80.2f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Z05_Final", new Vector3(0.8f, 0f, 83.0f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Final_B", new Vector3(4.2f, 0f, 82.0f), 180f, DemoEnemyActor.EncounterRole.Pusher, DemoEnemyActor.CombatArchetype.Enforcer, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Pusher)),
            new("Enemy_TankYard_Gate", new Vector3(-5.5f, 0f, 110f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Rifleman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_TankYard_LeftTank", new Vector3(-10.5f, 0f, 123f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_TankYard_RightTank", new Vector3(10.2f, 0f, 128f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_TankYard_ContainerPush", new Vector3(4.4f, 0f, 136f), 180f, DemoEnemyActor.EncounterRole.Pusher, DemoEnemyActor.CombatArchetype.Enforcer, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Pusher)),
            new("Enemy_Hangar_Left", new Vector3(-9f, 0f, 170f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Rifleman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Hangar_Right", new Vector3(9.8f, 0f, 171f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Hangar_Center", new Vector3(0.6f, 0f, 182f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Rifleman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Hangar_Push", new Vector3(3.2f, 0f, 193f), 180f, DemoEnemyActor.EncounterRole.Pusher, DemoEnemyActor.CombatArchetype.Enforcer, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Pusher)),
            new("Enemy_Control_Left", new Vector3(-6f, 0f, 232f), 180f, DemoEnemyActor.EncounterRole.Anchor, DemoEnemyActor.CombatArchetype.Marksman, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Anchor)),
            new("Enemy_Control_Right", new Vector3(6f, 0f, 232f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Control_BayFlank", new Vector3(8f, 0f, 247f), 180f, DemoEnemyActor.EncounterRole.Flanker, DemoEnemyActor.CombatArchetype.Assault, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Flanker)),
            new("Enemy_Control_FinalPush", new Vector3(0f, 0f, 255f), 180f, DemoEnemyActor.EncounterRole.Pusher, DemoEnemyActor.CombatArchetype.Enforcer, BuildRoleOffsets(DemoEnemyActor.EncounterRole.Pusher))
        };

        [MenuItem("Industrial Demo/Rebuild Stable Enemies")]
        public static void RebuildStableEnemies()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/IndustrialDemo");
            EnsureFolder(PrefabFolder);

            GameObject prefab = BuildPrefab();
            ReplaceSceneEnemies(prefab);
            EnsurePlayerHealth();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Stable enemies rebuilt.");
        }

        private static GameObject BuildPrefab()
        {
            GameObject root = new("PFB_IndustrialEnemy");
            root.layer = 0;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.32f;

            GameObject visualRoot = new("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Enemies/Materials/M_Enemy_Guard_Armor.mat");
            Material limbMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Enemies/Materials/M_Enemy_Guard_Cloth.mat");
            Material weaponMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Enemies/Materials/M_Enemy_Weapon_Metal.mat");
            Material accentMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Enemies/Materials/M_Enemy_Guard_Helmet.mat");
            Material visorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Enemies/Materials/M_Enemy_Visor_Glass.mat");

            Renderer torso = CreatePart(visualRoot.transform, "Torso", PrimitiveType.Cube, new Vector3(0f, 1.18f, 0f), new Vector3(0.52f, 0.56f, 0.28f), bodyMaterial);
            Renderer pelvis = CreatePart(visualRoot.transform, "Pelvis", PrimitiveType.Cube, new Vector3(0f, 0.82f, 0f), new Vector3(0.42f, 0.20f, 0.22f), limbMaterial);
            Renderer head = CreatePart(visualRoot.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.66f, 0f), new Vector3(0.26f, 0.28f, 0.26f), accentMaterial);
            Renderer leftArm = CreatePart(visualRoot.transform, "Arm_L", PrimitiveType.Cube, new Vector3(-0.36f, 1.14f, 0f), new Vector3(0.16f, 0.52f, 0.16f), limbMaterial);
            Renderer rightArm = CreatePart(visualRoot.transform, "Arm_R", PrimitiveType.Cube, new Vector3(0.36f, 1.14f, 0f), new Vector3(0.16f, 0.52f, 0.16f), limbMaterial);
            Renderer leftLeg = CreatePart(visualRoot.transform, "Leg_L", PrimitiveType.Cube, new Vector3(-0.14f, 0.36f, 0f), new Vector3(0.18f, 0.70f, 0.18f), limbMaterial);
            Renderer rightLeg = CreatePart(visualRoot.transform, "Leg_R", PrimitiveType.Cube, new Vector3(0.14f, 0.36f, 0f), new Vector3(0.18f, 0.70f, 0.18f), limbMaterial);
            Renderer visor = CreatePart(visualRoot.transform, "Visor", PrimitiveType.Cube, new Vector3(0f, 1.66f, 0.14f), new Vector3(0.18f, 0.05f, 0.03f), visorMaterial);
            Renderer rifle = CreatePart(visualRoot.transform, "Rifle", PrimitiveType.Cube, new Vector3(0.26f, 1.04f, 0.20f), new Vector3(0.12f, 0.16f, 0.64f), weaponMaterial);
            Renderer stock = CreatePart(visualRoot.transform, "Stock", PrimitiveType.Cube, new Vector3(0.18f, 1.10f, -0.02f), new Vector3(0.08f, 0.14f, 0.18f), weaponMaterial);

            GameObject muzzle = new("MuzzlePoint");
            muzzle.transform.SetParent(visualRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0.26f, 1.10f, 0.56f);

            DemoEnemyActor actor = root.AddComponent<DemoEnemyActor>();
            AssignActorReferences(actor, visualRoot.transform, muzzle.transform, new[]
            {
                torso, pelvis, head, leftArm, rightArm, leftLeg, rightLeg, visor, rifle, stock
            }, new[] { visor });

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void ReplaceSceneEnemies(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            for (int i = 0; i < EnemySpecs.Length; i++)
            {
                ReplaceSceneEnemy(scene, prefab, EnemySpecs[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ReplaceSceneEnemy(Scene scene, GameObject prefab, EnemySpec spec)
        {
            GameObject existing = GameObject.Find(spec.SceneName);
            bool isExistingActor = existing != null && existing.GetComponent<DemoEnemyActor>() != null;
            Vector3 position = existing != null && !isExistingActor
                ? ResolveGroundedPosition(GetReplacementPosition(existing))
                : ResolveGroundedPosition(spec.FallbackPosition);
            Quaternion rotation = isExistingActor
                ? Quaternion.Euler(0f, spec.Yaw, 0f)
                : existing != null
                    ? existing.transform.rotation
                    : Quaternion.Euler(0f, spec.Yaw, 0f);

            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = spec.SceneName;
            instance.transform.SetPositionAndRotation(position, rotation);
            ApplyProfile(instance, spec.Role, spec.Archetype);
            ApplyVisualProfile(instance, spec.Archetype);
            ApplyTacticalPoints(instance, spec.TacticalOffsets);
        }

        private static void EnsurePlayerHealth()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            GameObject player = GameObject.Find("Industrial_PlayerRig");
            if (player != null && player.GetComponent<DemoPlayerHealth>() == null)
            {
                player.AddComponent<DemoPlayerHealth>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static Renderer CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void AssignActorReferences(DemoEnemyActor actor, Transform visualRoot, Transform muzzlePoint, Renderer[] renderers, Renderer[] emissiveRenderers)
        {
            SerializedObject serialized = new(actor);
            serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serialized.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint;
            SetRendererArray(serialized.FindProperty("renderers"), renderers);
            SetRendererArray(serialized.FindProperty("emissiveRenderers"), emissiveRenderers);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRendererArray(SerializedProperty property, Renderer[] renderers)
        {
            property.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
        }

        private static void ApplyProfile(GameObject instance, DemoEnemyActor.EncounterRole role, DemoEnemyActor.CombatArchetype archetype)
        {
            DemoEnemyActor actor = instance.GetComponent<DemoEnemyActor>();
            if (actor == null)
            {
                return;
            }

            SerializedObject serialized = new(actor);
            serialized.FindProperty("encounterRole").enumValueIndex = (int)role;
            serialized.FindProperty("combatArchetype").enumValueIndex = (int)archetype;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyVisualProfile(GameObject instance, DemoEnemyActor.CombatArchetype archetype)
        {
            Transform visualRoot = instance.transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                return;
            }

            Transform torso = visualRoot.Find("Torso");
            Transform head = visualRoot.Find("Head");
            Transform leftArm = visualRoot.Find("Arm_L");
            Transform rightArm = visualRoot.Find("Arm_R");
            Transform rifle = visualRoot.Find("Rifle");
            Transform stock = visualRoot.Find("Stock");
            Transform muzzlePoint = visualRoot.Find("MuzzlePoint");

            visualRoot.localScale = Vector3.one;
            SetLocalTransform(torso, new Vector3(0f, 1.18f, 0f), new Vector3(0.52f, 0.56f, 0.28f));
            SetLocalTransform(head, new Vector3(0f, 1.66f, 0f), new Vector3(0.26f, 0.28f, 0.26f));
            SetLocalTransform(leftArm, new Vector3(-0.36f, 1.14f, 0f), new Vector3(0.16f, 0.52f, 0.16f));
            SetLocalTransform(rightArm, new Vector3(0.36f, 1.14f, 0f), new Vector3(0.16f, 0.52f, 0.16f));
            SetLocalTransform(rifle, new Vector3(0.26f, 1.04f, 0.20f), new Vector3(0.12f, 0.16f, 0.64f));
            SetLocalTransform(stock, new Vector3(0.18f, 1.10f, -0.02f), new Vector3(0.08f, 0.14f, 0.18f));
            SetLocalPosition(muzzlePoint, new Vector3(0.26f, 1.10f, 0.56f));

            switch (archetype)
            {
                case DemoEnemyActor.CombatArchetype.Assault:
                    visualRoot.localScale = new Vector3(0.98f, 0.98f, 1.02f);
                    SetLocalTransform(rifle, new Vector3(0.28f, 1.02f, 0.24f), new Vector3(0.12f, 0.15f, 0.74f));
                    SetLocalPosition(muzzlePoint, new Vector3(0.28f, 1.08f, 0.64f));
                    break;
                case DemoEnemyActor.CombatArchetype.Enforcer:
                    visualRoot.localScale = new Vector3(1.08f, 1.03f, 1.08f);
                    SetLocalTransform(torso, new Vector3(0f, 1.18f, 0f), new Vector3(0.62f, 0.66f, 0.34f));
                    SetLocalTransform(head, new Vector3(0f, 1.67f, 0f), new Vector3(0.30f, 0.31f, 0.30f));
                    SetLocalTransform(leftArm, new Vector3(-0.41f, 1.12f, 0f), new Vector3(0.19f, 0.58f, 0.19f));
                    SetLocalTransform(rightArm, new Vector3(0.41f, 1.12f, 0f), new Vector3(0.19f, 0.58f, 0.19f));
                    SetLocalTransform(rifle, new Vector3(0.31f, 1.00f, 0.22f), new Vector3(0.16f, 0.20f, 0.72f));
                    SetLocalTransform(stock, new Vector3(0.20f, 1.08f, -0.04f), new Vector3(0.11f, 0.18f, 0.22f));
                    SetLocalPosition(muzzlePoint, new Vector3(0.31f, 1.08f, 0.62f));
                    break;
                case DemoEnemyActor.CombatArchetype.Marksman:
                    visualRoot.localScale = new Vector3(0.95f, 1.04f, 0.95f);
                    SetLocalTransform(head, new Vector3(0f, 1.70f, 0f), new Vector3(0.24f, 0.27f, 0.24f));
                    SetLocalTransform(rifle, new Vector3(0.24f, 1.06f, 0.24f), new Vector3(0.10f, 0.12f, 0.86f));
                    SetLocalTransform(stock, new Vector3(0.15f, 1.12f, -0.08f), new Vector3(0.07f, 0.11f, 0.24f));
                    SetLocalPosition(muzzlePoint, new Vector3(0.24f, 1.12f, 0.74f));
                    break;
            }
        }

        private static void ApplyTacticalPoints(GameObject instance, Vector3[] tacticalOffsets)
        {
            DemoEnemyActor actor = instance.GetComponent<DemoEnemyActor>();
            if (actor == null)
            {
                return;
            }

            Transform tacticalRoot = instance.transform.Find("TacticalPoints");
            if (tacticalRoot != null)
            {
                Object.DestroyImmediate(tacticalRoot.gameObject);
            }

            tacticalRoot = new GameObject("TacticalPoints").transform;
            tacticalRoot.SetParent(instance.transform, false);

            SerializedObject serialized = new(actor);
            SerializedProperty tacticalPointsProperty = serialized.FindProperty("tacticalPoints");
            tacticalPointsProperty.arraySize = tacticalOffsets.Length;

            for (int i = 0; i < tacticalOffsets.Length; i++)
            {
                Transform point = new GameObject($"Tac_{i}").transform;
                point.SetParent(tacticalRoot, false);
                point.localPosition = tacticalOffsets[i];
                point.localRotation = Quaternion.identity;
                tacticalPointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = point;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 ResolveGroundedPosition(Vector3 basePosition)
        {
            Vector3 localOrigin = basePosition + Vector3.up * 1.5f;
            if (Physics.Raycast(localOrigin, Vector3.down, out RaycastHit localHit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return localHit.point;
            }

            Vector3 origin = basePosition + Vector3.up * 12f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 48f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return basePosition;
        }

        private static Vector3 GetReplacementPosition(GameObject existing)
        {
            Vector3 position = existing.transform.position;
            if (existing.GetComponent<DemoEnemyActor>() != null)
            {
                return position;
            }

            CapsuleCollider capsule = existing.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                position.y -= capsule.center.y;
            }

            return position;
        }

        private static Vector3[] BuildRoleOffsets(DemoEnemyActor.EncounterRole role)
        {
            switch (role)
            {
                case DemoEnemyActor.EncounterRole.Flanker:
                    return new[]
                    {
                        new Vector3(-2.2f, 0f, 0.9f),
                        new Vector3(1.7f, 0f, -1.2f),
                        new Vector3(0.6f, 0f, 2.2f)
                    };
                case DemoEnemyActor.EncounterRole.Pusher:
                    return new[]
                    {
                        new Vector3(0f, 0f, 2.0f),
                        new Vector3(-1.1f, 0f, 1.1f),
                        new Vector3(1.2f, 0f, 0.3f)
                    };
                default:
                    return new[]
                    {
                        new Vector3(0f, 0f, 0f),
                        new Vector3(-1.5f, 0f, -0.8f),
                        new Vector3(1.5f, 0f, -0.8f)
                    };
            }
        }

        private static void SetLocalTransform(Transform target, Vector3 localPosition, Vector3 localScale)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = localPosition;
            target.localScale = localScale;
        }

        private static void SetLocalPosition(Transform target, Vector3 localPosition)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = localPosition;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
