using System.Collections.Generic;
using System.IO;
using IndustrialDemo.Actors;
using IndustrialDemo.Combat;
using IndustrialDemo.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndustrialDemo.Editor
{
    public static class EnemyContentBuilder
    {
        private const string RootFolder = "Assets/Game/Enemies";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string AnimationsFolder = RootFolder + "/Animations";
        private const string WeaponsFolder = RootFolder + "/Weapons";
        private const string VfxFolder = RootFolder + "/VFX";
        private const string DocsFolder = RootFolder + "/Docs";

        private const string PolicePrefabPath = "Assets/Police_officer/Prefab/police_officer.prefab";
        private const string RiflePrefabPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab";
        private const string HandgunPrefabPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_Handgun_03.prefab";
        private const string ScenePath = "Assets/Industrial_Demo.unity/Industrial_Demo.unity";

        [MenuItem("Industrial Demo/Build Enemy Content")]
        public static void Build()
        {
            EnsureFolders();
            EnsureDocs();

            Dictionary<string, Material> materials = CreateMaterials();
            AnimatorController controller = CreateController();
            GameObject impactVfx = CreateImpactVfx();

            string guardA = BuildPrefab("Enemy_Guard_A", "Guard", controller, impactVfx, materials, false, false, false);
            string guardB = BuildPrefab("Enemy_Guard_B", "Guard", controller, impactVfx, materials, false, false, true);
            string breach = BuildPrefab("Enemy_Breach_A", "Breach Trooper", controller, impactVfx, materials, true, false, false);
            string suppressor = BuildPrefab("Enemy_Suppressor_A", "Suppression Specialist", controller, impactVfx, materials, false, true, false);

            ReplaceSceneEnemies(guardA, breach, suppressor);
            WriteReport(new[] { guardA, guardB, breach, suppressor });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Enemy content built.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Game");
            EnsureFolder(RootFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(AnimationsFolder);
            EnsureFolder(WeaponsFolder);
            EnsureFolder(VfxFolder);
            EnsureFolder(DocsFolder);
        }

        private static void EnsureDocs()
        {
            WriteFile(RootFolder + "/agents.md", "Scope: Assets/Game/Enemies.\n- Keep enemy content grounded and rebuildable.\n");
            WriteFile(PrefabsFolder + "/agents.md", "Scope: prefab outputs for enemy archetypes.\n");
            WriteFile(MaterialsFolder + "/agents.md", "Scope: HDRP enemy materials.\n");
            WriteFile(AnimationsFolder + "/agents.md", "Scope: humanoid-compatible enemy animation assets.\n");
            WriteFile(WeaponsFolder + "/agents.md", "Scope: enemy weapon presentation assets.\n");
            WriteFile(VfxFolder + "/agents.md", "Scope: compact enemy hit/readability VFX.\n");
            WriteFile(DocsFolder + "/agents.md", "Scope: generated enemy documentation.\n");
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            Texture2D torsoAlbedo = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficer_torso_AlbedoTransparency.tga");
            Texture2D torsoNormal = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficerl_torso_Normal.tga");
            Texture2D legAlbedo = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficer_leg_AlbedoTransparency.tga");
            Texture2D legNormal = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficerl_leg_Normal.tga");
            Texture2D headAlbedo = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficer_head_AlbedoTransparency.tga");
            Texture2D headNormal = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficer_head_Normal.tga");
            Texture2D capAlbedo = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficer_cap_AlbedoTransparency.tga");
            Texture2D capNormal = Load<Texture2D>("Assets/Police_officer/Texture/T_policeofficerl_cap_Normal.tga");

            return new Dictionary<string, Material>
            {
                ["guard_cloth"] = CreateLit("M_Enemy_Guard_Cloth.mat", new Color(0.22f, 0.26f, 0.30f), legAlbedo, legNormal, 0f, 0.28f),
                ["guard_cloth_alt"] = CreateLit("M_Enemy_Guard_Cloth_Alt.mat", new Color(0.18f, 0.22f, 0.26f), legAlbedo, legNormal, 0f, 0.22f),
                ["guard_armor"] = CreateLit("M_Enemy_Guard_Armor.mat", new Color(0.24f, 0.24f, 0.26f), torsoAlbedo, torsoNormal, 0.22f, 0.42f),
                ["guard_helmet"] = CreateLit("M_Enemy_Guard_Helmet.mat", new Color(0.13f, 0.13f, 0.14f), capAlbedo, capNormal, 0.10f, 0.34f),
                ["guard_accent"] = CreateLit("M_Enemy_Guard_Accent.mat", new Color(0.57f, 0.31f, 0.20f), null, null, 0.02f, 0.30f),
                ["guard_accent_alt"] = CreateLit("M_Enemy_Guard_Accent_Alt.mat", new Color(0.47f, 0.24f, 0.22f), null, null, 0.02f, 0.30f),
                ["breach_cloth"] = CreateLit("M_Enemy_Breach_Cloth.mat", new Color(0.18f, 0.19f, 0.19f), legAlbedo, legNormal, 0f, 0.18f),
                ["breach_armor"] = CreateLit("M_Enemy_Breach_Armor.mat", new Color(0.27f, 0.30f, 0.27f), torsoAlbedo, torsoNormal, 0.26f, 0.44f),
                ["breach_helmet"] = CreateLit("M_Enemy_Breach_Helmet.mat", new Color(0.11f, 0.11f, 0.12f), capAlbedo, capNormal, 0.08f, 0.24f),
                ["breach_accent"] = CreateLit("M_Enemy_Breach_Accent.mat", new Color(0.82f, 0.44f, 0.16f), null, null, 0.03f, 0.34f),
                ["suppressor_cloth"] = CreateLit("M_Enemy_Suppressor_Cloth.mat", new Color(0.24f, 0.25f, 0.28f), legAlbedo, legNormal, 0f, 0.20f),
                ["suppressor_armor"] = CreateLit("M_Enemy_Suppressor_Armor.mat", new Color(0.21f, 0.23f, 0.25f), torsoAlbedo, torsoNormal, 0.18f, 0.38f),
                ["suppressor_helmet"] = CreateLit("M_Enemy_Suppressor_Helmet.mat", new Color(0.10f, 0.12f, 0.16f), capAlbedo, capNormal, 0.10f, 0.34f),
                ["suppressor_accent"] = CreateLit("M_Enemy_Suppressor_Accent.mat", new Color(0.55f, 0.20f, 0.20f), null, null, 0.04f, 0.30f),
                ["weapon_metal"] = CreateLit("M_Enemy_Weapon_Metal.mat", new Color(0.25f, 0.27f, 0.29f), null, null, 0.78f, 0.48f),
                ["weapon_polymer"] = CreateLit("M_Enemy_Weapon_Polymer.mat", new Color(0.10f, 0.10f, 0.11f), null, null, 0.02f, 0.18f),
                ["weapon_accent"] = CreateLit("M_Enemy_Weapon_Accent.mat", new Color(0.30f, 0.29f, 0.24f), null, null, 0.04f, 0.24f),
                ["visor"] = CreateTransparent("M_Enemy_Visor_Glass.mat", new Color(0.42f, 0.53f, 0.57f, 0.28f), headAlbedo, headNormal),
                ["skin"] = CreateLit("M_Enemy_Head_Skin.mat", new Color(0.68f, 0.57f, 0.49f), headAlbedo, headNormal, 0.02f, 0.26f)
            };
        }

        private static AnimatorController CreateController()
        {
            string path = AnimationsFolder + "/AC_Enemy_Humanoid.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            controller.parameters = new[]
            {
                new AnimatorControllerParameter { name = "MoveSpeed", type = AnimatorControllerParameterType.Float },
                new AnimatorControllerParameter { name = "Combat", type = AnimatorControllerParameterType.Bool, defaultBool = true }
            };

            AnimationClip idle = Load<AnimationClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations/Character/_Common/A_FP_PCH_Idle.fbx");
            AnimationClip walk = Load<AnimationClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations/Character/_Common/A_FP_PCH_Walk_F.fbx");
            AnimationClip run = Load<AnimationClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations/Character/_Common/A_FP_PCH_Run_01.fbx");
            AnimationClip aimIdle = Load<AnimationClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations/Character/_Common/A_FP_PCH_Aim_Idle.fbx");
            AnimationClip aimWalk = Load<AnimationClip>("Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations/Character/_Common/A_FP_PCH_Aim_Walk_F.fbx");

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            sm.states = new ChildAnimatorState[0];
            AnimatorState sIdle = sm.AddState("Idle");
            AnimatorState sWalk = sm.AddState("Walk");
            AnimatorState sRun = sm.AddState("Run");
            AnimatorState sAimIdle = sm.AddState("AimIdle");
            AnimatorState sAimWalk = sm.AddState("AimWalk");
            sIdle.motion = idle;
            sWalk.motion = walk;
            sRun.motion = run;
            sAimIdle.motion = aimIdle;
            sAimWalk.motion = aimWalk;
            sm.defaultState = sAimIdle;

            AddTransition(sAimIdle, sAimWalk, AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");
            AddTransition(sAimWalk, sAimIdle, AnimatorConditionMode.Less, 0.1f, "MoveSpeed");
            AddTransition(sAimWalk, sRun, AnimatorConditionMode.Greater, 0.7f, "MoveSpeed");
            AddTransition(sRun, sAimWalk, AnimatorConditionMode.Less, 0.7f, "MoveSpeed");
            AddTransition(sAimIdle, sIdle, AnimatorConditionMode.IfNot, 0f, "Combat");
            AddTransition(sIdle, sAimIdle, AnimatorConditionMode.If, 0f, "Combat");
            AddTransition(sIdle, sWalk, AnimatorConditionMode.Greater, 0.1f, "MoveSpeed");
            AddTransition(sWalk, sIdle, AnimatorConditionMode.Less, 0.1f, "MoveSpeed");

            return controller;
        }

        private static GameObject CreateImpactVfx()
        {
            string path = VfxFolder + "/P_Enemy_Impact.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new("P_Enemy_Impact");
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            root.AddComponent<TimedSelfDestruct>();

            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = 0.22f;
            main.startSpeed = 2.1f;
            main.startSize = 0.07f;
            main.startColor = new Color(1f, 0.48f, 0.22f, 0.9f);
            main.maxParticles = 16;

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static string BuildPrefab(string prefabName, string archetype, AnimatorController controller, GameObject impactVfx, Dictionary<string, Material> m, bool breach, bool suppressor, bool altGuard)
        {
            GameObject basePrefab = Load<GameObject>(PolicePrefabPath);
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = prefabName;
            root.transform.localScale = Vector3.one * 1.08f;

            root.GetComponent<Animator>().runtimeAnimatorController = controller;
            AssignMaterial(root, "Sk_Police_girl_torso", breach ? m["breach_armor"] : suppressor ? m["suppressor_armor"] : m["guard_armor"]);
            AssignMaterial(root, "Sk_Police_girl_leg", breach ? m["breach_cloth"] : suppressor ? m["suppressor_cloth"] : altGuard ? m["guard_cloth_alt"] : m["guard_cloth"]);
            AssignMaterial(root, "Sk_Police_girl_cap", breach ? m["breach_helmet"] : suppressor ? m["suppressor_helmet"] : m["guard_helmet"]);
            AssignMaterial(root, "Sk_Police_girl_hair", breach ? m["breach_helmet"] : suppressor ? m["suppressor_helmet"] : m["guard_helmet"]);
            AssignMaterial(root, "Sk_Police_girl_Head", m["skin"]);
            AssignMaterial(root, "Sk_Police_girl_pistolet", m["weapon_polymer"]);

            AddColliders(root);
            AttachWeapons(root, m, breach, suppressor);
            AddRoleShapes(root, m, breach, suppressor, altGuard);
            AddPresentation(root, archetype, impactVfx, breach, suppressor);

            string path = PrefabsFolder + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        private static void AddColliders(GameObject root)
        {
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = root.AddComponent<CapsuleCollider>();
            }

            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.28f;

            Transform head = root.transform.Find("armature/root/pelvis/spine_01/spine_02/spine_03/neck_01/head");
            if (head != null && head.GetComponent<SphereCollider>() == null)
            {
                SphereCollider sphere = head.gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.12f;
            }
        }

        private static void AttachWeapons(GameObject root, Dictionary<string, Material> m, bool breach, bool suppressor)
        {
            Transform hand = root.transform.Find("armature/root/ik_hand_root/ik_hand_gun");
            Transform holster = root.transform.Find("armature/root/pelvis/thigh_r/u_gun");

            GameObject rifle = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(RiflePrefabPath));
            rifle.name = "PrimaryWeapon";
            rifle.transform.SetParent(hand, false);
            rifle.transform.localPosition = suppressor ? new Vector3(-0.02f, -0.02f, 0.18f) : new Vector3(0f, -0.02f, 0.12f);
            rifle.transform.localRotation = Quaternion.Euler(180f, 92f, 0f);
            rifle.transform.localScale = breach ? Vector3.one * 0.88f : Vector3.one;
            OverrideWeaponMaterials(rifle, m);

            GameObject handgun = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(HandgunPrefabPath));
            handgun.name = "Sidearm";
            handgun.transform.SetParent(holster, false);
            handgun.transform.localPosition = new Vector3(0.04f, 0f, 0.02f);
            handgun.transform.localRotation = Quaternion.Euler(92f, 0f, -90f);
            handgun.transform.localScale = Vector3.one * 0.72f;
            OverrideWeaponMaterials(handgun, m);
        }

        private static void AddRoleShapes(GameObject root, Dictionary<string, Material> m, bool breach, bool suppressor, bool altGuard)
        {
            Material accent = breach ? m["breach_accent"] : suppressor ? m["suppressor_accent"] : altGuard ? m["guard_accent_alt"] : m["guard_accent"];
            Material armor = breach ? m["breach_armor"] : suppressor ? m["suppressor_armor"] : m["guard_armor"];
            Transform spine = root.transform.Find("armature/root/pelvis/spine_01/spine_02/spine_03");
            Transform head = root.transform.Find("armature/root/pelvis/spine_01/spine_02/spine_03/neck_01/head");

            CreateShape(root.transform, "Patch_L", PrimitiveType.Cube, new Vector3(-0.21f, 1.34f, 0.02f), new Vector3(0.05f, 0.12f, 0.02f), accent);
            CreateShape(root.transform, "Patch_R", PrimitiveType.Cube, new Vector3(0.21f, 1.34f, 0.02f), new Vector3(0.05f, 0.12f, 0.02f), accent);

            if (breach)
            {
                CreateShape(spine, "ChestPlate", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.14f), new Vector3(0.38f, 0.28f, 0.12f), armor);
                CreateShape(spine, "ShoulderL", PrimitiveType.Cube, new Vector3(-0.26f, 0.10f, 0.04f), new Vector3(0.16f, 0.14f, 0.16f), armor);
                CreateShape(spine, "ShoulderR", PrimitiveType.Cube, new Vector3(0.26f, 0.10f, 0.04f), new Vector3(0.16f, 0.14f, 0.16f), armor);
                CreateShape(spine, "Utility", PrimitiveType.Cube, new Vector3(0.18f, -0.22f, 0.08f), new Vector3(0.10f, 0.12f, 0.06f), accent);
            }

            if (suppressor)
            {
                CreateShape(spine, "BackAntenna", PrimitiveType.Cube, new Vector3(0f, 0.26f, -0.08f), new Vector3(0.04f, 0.22f, 0.04f), accent);
                CreateShape(head, "Optic", PrimitiveType.Cube, new Vector3(0.1f, 0.1f, 0.12f), new Vector3(0.07f, 0.05f, 0.12f), accent);
                CreateShape(head, "Visor", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.16f), new Vector3(0.18f, 0.06f, 0.03f), m["visor"]);
            }
        }

        private static void AddPresentation(GameObject root, string archetype, GameObject impactVfx, bool breach, bool suppressor)
        {
            SurfaceMaterial surface = root.GetComponent<SurfaceMaterial>() ?? root.AddComponent<SurfaceMaterial>();
            SerializedObject surfaceSO = new(surface);
            surfaceSO.FindProperty("penetrationCost").floatValue = breach ? 110f : 90f;
            surfaceSO.FindProperty("damageMultiplierThroughSurface").floatValue = 0.55f;
            surfaceSO.FindProperty("impactVfxPrefab").objectReferenceValue = impactVfx;
            surfaceSO.ApplyModifiedPropertiesWithoutUndo();

            EnemyPresentationTarget target = root.GetComponent<EnemyPresentationTarget>() ?? root.AddComponent<EnemyPresentationTarget>();
            SerializedObject targetSO = new(target);
            targetSO.FindProperty("archetypeId").stringValue = archetype;
            targetSO.FindProperty("maxHealth").floatValue = breach ? 150f : suppressor ? 110f : 95f;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            SerializedProperty list = targetSO.FindProperty("hitFlashRenderers");
            list.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
            targetSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplaceSceneEnemies(string guardA, string breach, string suppressor)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ReplaceSceneEnemy(scene, "Enemy_Z02_Left", guardA);
            ReplaceSceneEnemy(scene, "Enemy_Z03_Inner", breach);
            ReplaceSceneEnemy(scene, "Enemy_Z05_Final", suppressor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ReplaceSceneEnemy(Scene scene, string sceneName, string prefabPath)
        {
            GameObject existing = GameObject.Find(sceneName);
            if (existing == null)
            {
                return;
            }

            Vector3 position = existing.transform.position;
            Object.DestroyImmediate(existing);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(prefabPath), scene);
            instance.name = sceneName;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localScale = Vector3.one;
        }

        private static void WriteReport(IEnumerable<string> prefabPaths)
        {
            string report =
"# Enemy Build Report\n\n" +
"Used assets:\n" +
"- Assets/Police_officer/Prefab/police_officer.prefab\n" +
"- Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_AR_01.prefab\n" +
"- Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_Handgun_03.prefab\n" +
"- Infima humanoid-compatible clips from Art/Animations/Character\n\n" +
"Built prefabs:\n" +
"- Enemy_Guard_A\n- Enemy_Guard_B\n- Enemy_Breach_A\n- Enemy_Suppressor_A\n\n" +
"Created materials:\n" +
"- guard/breach/suppressor cloth, armor, helmet, accent\n" +
"- weapon metal, weapon polymer, weapon accent, visor glass\n\n" +
"Simplifications:\n" +
"- Missing Human Basic Motions FREE, Human Soldier Animations FREE, Low Poly Soldiers Demo, and City-Themed pack were replaced with available Infima fallback clips and silhouettes.\n" +
"- Breach archetype uses a compact rifle silhouette instead of a dedicated shotgun/SMG because that asset is not imported.\n" +
"- This build replaces billboard enemies with readable humanoid targets and hit feedback, not full combat AI.\n\n" +
"Prefab paths:\n";

            foreach (string prefabPath in prefabPaths)
            {
                report += "- " + prefabPath + "\n";
            }

            WriteFile(DocsFolder + "/EnemyBuildReport.md", report);
        }

        private static Material CreateLit(string fileName, Color color, Texture2D albedo, Texture2D normal, float metallic, float smoothness)
        {
            string path = MaterialsFolder + "/" + fileName;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("HDRP/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BaseColorMap", albedo);
            material.SetTexture("_NormalMap", normal);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTransparent(string fileName, Color color, Texture2D albedo, Texture2D normal)
        {
            Material material = CreateLit(fileName, color, albedo, normal, 0f, 0.85f);
            material.SetFloat("_SurfaceType", 1f);
            material.SetFloat("_TransparentDepthPrepassEnable", 1f);
            material.SetFloat("_TransparentBackfaceEnable", 1f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string parameter)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(mode, threshold, parameter);
        }

        private static void AssignMaterial(GameObject root, string childName, Material material)
        {
            Transform child = root.transform.Find(childName);
            if (child != null && child.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void OverrideWeaponMaterials(GameObject root, Dictionary<string, Material> materials)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] source = renderer.sharedMaterials;
                Material[] target = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    string name = source[i] != null ? source[i].name.ToLowerInvariant() : string.Empty;
                    target[i] = name.Contains("grip") || name.Contains("stock") ? materials["weapon_polymer"] :
                        name.Contains("sight") ? materials["weapon_accent"] : materials["weapon_metal"];
                }
                renderer.sharedMaterials = target;
            }
        }

        private static void CreateShape(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPos;
            shape.transform.localScale = localScale;
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException(path);
            }
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent ?? "Assets", Path.GetFileName(path));
        }

        private static void WriteFile(string path, string contents)
        {
            string normalized = path.Replace("\\", "/");
            if (File.Exists(normalized) && File.ReadAllText(normalized) == contents)
            {
                return;
            }
            File.WriteAllText(normalized, contents);
        }
    }
}
