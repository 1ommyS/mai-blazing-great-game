using System.IO;
using IndustrialDemo.Breaching;
using IndustrialDemo.Combat;
using IndustrialDemo.Foam;
using IndustrialDemo.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndustrialDemo.Editor
{
    public static class ChecklistGameplayBuilder
    {
        private const string ScenePath = "Assets/Industrial_Demo.unity/Industrial_Demo.unity";

        [MenuItem("Industrial Demo/Build Checklist Gameplay")]
        public static void BuildChecklistGameplay()
        {
            IndustrialSceneExpansionBuilder.BuildSceneExpansion();
            StableEnemyBuilder.RebuildStableEnemies();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ConfigurePlayer();
            BuildZone02();
            BuildZone03();
            BuildZone04();
            BuildZone05();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Checklist gameplay rebuilt.");
        }

        private static void ConfigurePlayer()
        {
            GameObject playerObject = GameObject.Find("Industrial_PlayerRig");
            GameObject cameraObject = GameObject.Find("Industrial_PlayerRig/Industrial_MainCamera");
            if (cameraObject != null && cameraObject.GetComponent<DemoGameplayCalloutHud>() == null)
            {
                cameraObject.AddComponent<DemoGameplayCalloutHud>();
            }

            if (playerObject != null)
            {
                DemoFirstPersonMotor motor = playerObject.GetComponent<DemoFirstPersonMotor>();
                if (motor != null)
                {
                    SerializedObject motorSerialized = new(motor);
                    motorSerialized.FindProperty("walkSpeed").floatValue = 4.2f;
                    motorSerialized.FindProperty("sprintSpeed").floatValue = 7.4f;
                    motorSerialized.FindProperty("horizontalAcceleration").floatValue = 22f;
                    motorSerialized.FindProperty("gravity").floatValue = 18f;
                    motorSerialized.FindProperty("sprintFovBoost").floatValue = 6f;
                    motorSerialized.FindProperty("sprintFovBlendSpeed").floatValue = 8f;
                    motorSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (cameraObject != null)
            {
                DemoWeaponInput weaponInput = cameraObject.GetComponent<DemoWeaponInput>();
                if (weaponInput != null)
                {
                    SerializedObject weaponInputSerialized = new(weaponInput);
                    weaponInputSerialized.FindProperty("weaponFireController").objectReferenceValue = cameraObject.GetComponent<WeaponFireController>();
                    weaponInputSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                DemoFoamInput foamInput = cameraObject.GetComponent<DemoFoamInput>();
                if (foamInput != null)
                {
                    SerializedObject foamInputSerialized = new(foamInput);
                    foamInputSerialized.FindProperty("foamTool").objectReferenceValue = cameraObject.GetComponent<FoamToolController>();
                    foamInputSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                FoamToolController foamTool = cameraObject.GetComponent<FoamToolController>();
                if (foamTool != null)
                {
                    SerializedObject foamToolSerialized = new(foamTool);
                    foamToolSerialized.FindProperty("aimCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
                    foamToolSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                DemoBreachInteractor breachInteractor = cameraObject.GetComponent<DemoBreachInteractor>();
                if (breachInteractor != null)
                {
                    SerializedObject breachSerialized = new(breachInteractor);
                    breachSerialized.FindProperty("interactionCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
                    breachSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void BuildZone02()
        {
            Transform zoneRoot = GameObject.Find("Industrial_Demo_Root/Geo/Zone_02_CentralHall")?.transform;
            if (zoneRoot == null)
            {
                return;
            }

            Transform root = RebuildChild(zoneRoot, "ChecklistGameplay");
            Material panelMaterial = LoadOrCreateTintedMaterial("Assets/Game/Generated/Materials/M_Checklist_ThinPanel.mat", "Assets/Industrial_Demo_Mat_Concrete.mat", new Color(0.73f, 0.77f, 0.8f));
            Material woodMaterial = LoadOrCreateTintedMaterial("Assets/Game/Generated/Materials/M_Checklist_WoodCover.mat", "Assets/Industrial_Demo_Mat_Concrete.mat", new Color(0.43f, 0.28f, 0.16f));
            Material steelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_Steel.mat");
            Material reinforcedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_Concrete.mat");

            GameObject glassPartition = GameObject.Find("Industrial_Demo_Root/Geo/Zone_02_CentralHall/Z02_GlassPartition");
            ConfigureSurface(glassPartition, SurfaceMaterialType.Glass, 18f, true, 84f, 0.92f, false, 0f);

            GameObject thinPanel = CreateBlock(root, "Z02_ThinPanel", new Vector3(-5.1f, 1.25f, 8.8f), Quaternion.identity, new Vector3(2.1f, 2.5f, 0.16f), panelMaterial);
            ConfigureSurface(thinPanel, SurfaceMaterialType.Drywall, 26f, true, 86f, 0.78f, false, 0f);

            GameObject woodCover = CreateBlock(root, "Z02_WoodCover", new Vector3(7.4f, 0.85f, 12.2f), Quaternion.identity, new Vector3(1.8f, 1.7f, 0.55f), woodMaterial);
            ConfigureSurface(woodCover, SurfaceMaterialType.Wood, 42f, true, 78f, 0.72f, true, 70f);

            GameObject steelPlate = CreateBlock(root, "Z02_RicochetPlate", new Vector3(1.5f, 1.3f, 4.7f), Quaternion.Euler(0f, 36f, 0f), new Vector3(1.05f, 2.6f, 0.18f), steelMaterial);
            ConfigureSurface(steelPlate, SurfaceMaterialType.Steel, 210f, true, 58f, 0.34f, false, 0f);

            GameObject reinforcedBlock = CreateBlock(root, "Z02_ReinforcedBlock", new Vector3(3.9f, 1.15f, 5.4f), Quaternion.identity, new Vector3(1.5f, 2.3f, 1.2f), reinforcedMaterial);
            ConfigureSurface(reinforcedBlock, SurfaceMaterialType.Reinforced, 999f, true, 72f, 0.25f, false, 0f);

            CreateCallout(root, "Callout_Penetration", new Vector3(0f, 1.3f, 3.2f), new Vector3(10f, 2.5f, 10f), "SURFACE TEST LANE", "Glass and drywall mostly let rounds through. Wood weakens them. Steel and reinforced cover reward shallow-angle ricochet shots.", new Color(1f, 0.75f, 0.3f, 1f));
        }

        private static void BuildZone03()
        {
            Transform zoneRoot = GameObject.Find("Industrial_Demo_Root/Geo/Zone_03_Breach")?.transform;
            if (zoneRoot == null)
            {
                return;
            }

            Transform root = RebuildChild(zoneRoot, "ChecklistGameplay");
            CreateCallout(root, "Callout_Breach", new Vector3(0f, 1.3f, 28.4f), new Vector3(12f, 2.5f, 6f), "BREACH CHOICE", "Use the side panel for a quiet angle or force the main door for a loud push. Loud entries alert the room.", new Color(1f, 0.42f, 0.26f, 1f));
        }

        private static void BuildZone04()
        {
            Transform zoneRoot = GameObject.Find("Industrial_Demo_Root/Geo/Zone_04_Steam")?.transform;
            if (zoneRoot == null)
            {
                return;
            }

            Transform root = RebuildChild(zoneRoot, "ChecklistGameplay");
            GameObject leakBarrier = GameObject.Find("Industrial_Demo_Root/Geo/Zone_04_Steam/Z04_LeakBarrier");
            ConfigureSurface(leakBarrier, SurfaceMaterialType.Reinforced, 999f, false, 0f, 0.2f, false, 0f);

            Material foamAnchorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_Foam.mat");
            GameObject coverAnchor = CreateBlock(root, "Z04_FoamCover_Secondary", new Vector3(2.6f, 1.0f, 58.2f), Quaternion.Euler(0f, -12f, 0f), new Vector3(1.6f, 2f, 0.8f), foamAnchorMaterial);
            EnsureComponent<FoamCoverAnchor>(coverAnchor);

            GameObject jumpAnchor = CreateBlock(root, "Z04_FoamJumpAnchor", new Vector3(-2.8f, 0.5f, 60.1f), Quaternion.identity, new Vector3(1.2f, 1f, 1.2f), foamAnchorMaterial);
            FoamCoverAnchor jumpAnchorComponent = EnsureComponent<FoamCoverAnchor>(jumpAnchor);
            ConfigureCoverAnchor(jumpAnchorComponent, new Vector3(1.3f, 0.95f, 1.05f));
            CreateBlock(root, "Z04_FoamJumpLedge", new Vector3(-2.8f, 1.55f, 62.0f), Quaternion.identity, new Vector3(2.2f, 0.35f, 1.8f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_BalconyDeck.mat"));

            CreateCallout(root, "Callout_Seal", new Vector3(0f, 1.3f, 51.6f), new Vector3(8f, 2.5f, 8f), "SEAL THE LEAK", "Spray foam on the highlighted seal point to clear the steam gate. This route is unsafe until sealed.", new Color(0.34f, 0.98f, 1f, 1f));
            CreateCallout(root, "Callout_FoamCover", new Vector3(0f, 1.3f, 58.5f), new Vector3(10f, 2.5f, 8f), "FOAM COVER", "Foam now hardens into real cover. It blocks fire and movement, but it will break if you lean on it too long.", new Color(0.78f, 0.94f, 1f, 1f));
            CreateCallout(root, "Callout_FoamJump", new Vector3(-2.8f, 1.2f, 61.2f), new Vector3(6f, 2.5f, 5f), "FOAM STEP-UP", "Spray this floor anchor to build a climbable step. Jump onto the foam, then vault to the service ledge.", new Color(0.88f, 0.97f, 1f, 1f));
        }

        private static void BuildZone05()
        {
            Transform zoneRoot = GameObject.Find("Industrial_Demo_Root/Geo/Zone_05_Evac")?.transform;
            if (zoneRoot == null)
            {
                return;
            }

            Transform root = RebuildChild(zoneRoot, "ChecklistGameplay");
            Material panelMaterial = LoadOrCreateTintedMaterial("Assets/Game/Generated/Materials/M_Checklist_WeakPanel.mat", "Assets/Industrial_Demo_Mat_Concrete.mat", new Color(0.68f, 0.74f, 0.78f));
            Material woodMaterial = LoadOrCreateTintedMaterial("Assets/Game/Generated/Materials/M_Checklist_LightCover.mat", "Assets/Industrial_Demo_Mat_Concrete.mat", new Color(0.44f, 0.29f, 0.18f));

            GameObject slowMarker = GameObject.Find("Z05_SlowZoneMarker");
            if (slowMarker != null)
            {
                SerializedObject markerSerialized = new(slowMarker.GetComponent<FoamSlowZoneMarker>());
                markerSerialized.FindProperty("zoneSize").vector3Value = new Vector3(4.4f, 0.35f, 4.4f);
                markerSerialized.FindProperty("slowMultiplier").floatValue = 0.4f;
                markerSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject weakPanel = CreateBlock(root, "Z05_WeakPanel", new Vector3(0.9f, 1.25f, 82.2f), Quaternion.identity, new Vector3(2.4f, 2.5f, 0.16f), panelMaterial);
            ConfigureSurface(weakPanel, SurfaceMaterialType.Drywall, 24f, false, 0f, 0.78f, false, 0f);

            GameObject lightCover = CreateBlock(root, "Z05_LightCover", new Vector3(4.2f, 0.85f, 81.8f), Quaternion.identity, new Vector3(1.6f, 1.6f, 0.55f), woodMaterial);
            ConfigureSurface(lightCover, SurfaceMaterialType.Wood, 40f, true, 78f, 0.7f, true, 68f);

            CreatePeekDoor(root, "Z05_SidePressureHatch", new Vector3(-4.7f, 0f, 79.7f), Quaternion.Euler(0f, 90f, 0f), 1.3f, 2.6f);
            CreateBlock(root, "Z05_SprintDivider_A", new Vector3(-2.2f, 0.75f, 76.4f), Quaternion.identity, new Vector3(1.8f, 1.5f, 0.7f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(root, "Z05_SprintDivider_B", new Vector3(2.5f, 0.75f, 79.8f), Quaternion.identity, new Vector3(1.8f, 1.5f, 0.7f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateCallout(root, "Callout_Combined", new Vector3(0f, 1.3f, 74.8f), new Vector3(10f, 2.5f, 10f), "COMBINED ENCOUNTER", "Slow the choke or foam-block the side hatch, then sprint between hard cover to hit the breach and shoot through weak cover inside.", new Color(0.96f, 0.84f, 0.34f, 1f));
            CreateCallout(root, "Callout_Sprint", new Vector3(0f, 1.2f, 77.8f), new Vector3(9f, 2.5f, 7f), "SPRINT WINDOW", "Hold Shift to burst between these dividers before the final crossfire closes. Sprinting now boosts speed and camera feedback.", new Color(1f, 0.92f, 0.45f, 1f));
        }

        private static Transform RebuildChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.SetPositionAndRotation(position, rotation);
            block.transform.localScale = scale;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static void ConfigureSurface(GameObject target, SurfaceMaterialType materialType, float penetration, bool ricochet, float ricochetAngle, float damageMultiplier, bool destructible, float hp)
        {
            if (target == null)
            {
                return;
            }

            SurfaceMaterial surface = EnsureComponent<SurfaceMaterial>(target);
            surface.ConfigureRuntime(materialType, penetration, ricochet, ricochetAngle, damageMultiplier, destructible, hp);
            AssignSurfacePresentation(surface, materialType);
            EditorUtility.SetDirty(surface);
        }

        private static void AssignSurfacePresentation(SurfaceMaterial surface, SurfaceMaterialType materialType)
        {
            GameObject steelImpact = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IndustrialDemo/FX/PFX_SteelImpact.prefab");
            GameObject glassImpact = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IndustrialDemo/FX/PFX_GlassImpact.prefab");
            GameObject fragileBreak = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IndustrialDemo/FX/PFX_FragileBreak.prefab");
            GameObject ricochet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/IndustrialDemo/FX/PFX_Ricochet.prefab");

            switch (materialType)
            {
                case SurfaceMaterialType.Glass:
                    surface.AssignImpactFx(glassImpact, fragileBreak, ricochet, fragileBreak);
                    break;
                case SurfaceMaterialType.Drywall:
                case SurfaceMaterialType.Wood:
                    surface.AssignImpactFx(fragileBreak, fragileBreak, ricochet, fragileBreak);
                    break;
                case SurfaceMaterialType.Steel:
                case SurfaceMaterialType.Reinforced:
                    surface.AssignImpactFx(steelImpact, steelImpact, ricochet, fragileBreak);
                    break;
                default:
                    surface.AssignImpactFx(steelImpact, steelImpact, ricochet, fragileBreak);
                    break;
            }
        }

        private static void ConfigureCoverAnchor(FoamCoverAnchor anchor, Vector3 size)
        {
            if (anchor == null)
            {
                return;
            }

            SerializedObject serialized = new(anchor);
            serialized.FindProperty("coverSize").vector3Value = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCallout(Transform parent, string name, Vector3 position, Vector3 size, string title, string body, Color accentColor)
        {
            GameObject callout = new(name);
            callout.transform.SetParent(parent, false);
            callout.transform.position = position;

            BoxCollider boxCollider = callout.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = size;

            DemoGameplayCalloutZone zone = callout.AddComponent<DemoGameplayCalloutZone>();
            SerializedObject serialized = new(zone);
            serialized.FindProperty("title").stringValue = title;
            serialized.FindProperty("body").stringValue = body;
            serialized.FindProperty("duration").floatValue = 5.5f;
            serialized.FindProperty("triggerOnce").boolValue = true;
            serialized.FindProperty("accentColor").colorValue = accentColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePeekDoor(Transform parent, string name, Vector3 position, Quaternion rotation, float width, float height)
        {
            Material steelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Industrial_Demo_Mat_Steel.mat");

            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            Transform frame = new GameObject("Frame").transform;
            frame.SetParent(root.transform, false);
            CreateBlock(frame, "FrameLeft", position + root.transform.rotation * new Vector3(-width * 0.55f, height * 0.5f, 0f), rotation, new Vector3(0.14f, height + 0.18f, 0.18f), steelMaterial);
            CreateBlock(frame, "FrameRight", position + root.transform.rotation * new Vector3(width * 0.55f, height * 0.5f, 0f), rotation, new Vector3(0.14f, height + 0.18f, 0.18f), steelMaterial);
            CreateBlock(frame, "FrameTop", position + root.transform.rotation * new Vector3(0f, height + 0.09f, 0f), rotation, new Vector3(width + 0.28f, 0.16f, 0.18f), steelMaterial);

            Transform pivot = new GameObject("LeafPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
            pivot.localRotation = Quaternion.Euler(0f, 36f, 0f);

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(pivot, false);
            leaf.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f);
            leaf.transform.localRotation = Quaternion.identity;
            leaf.transform.localScale = new Vector3(width, height, 0.12f);
            leaf.GetComponent<Renderer>().sharedMaterial = steelMaterial;

            BreachableEntry entry = root.AddComponent<BreachableEntry>();
            SerializedObject serialized = new(entry);
            serialized.FindProperty("currentState").enumValueIndex = (int)BreachableEntryState.PeekOpen;
            serialized.FindProperty("entryType").enumValueIndex = (int)BreachableEntryType.SideHatch;
            serialized.FindProperty("supportsManualBreach").boolValue = false;
            serialized.FindProperty("supportsShotBreach").boolValue = false;
            serialized.FindProperty("supportsForcedBreach").boolValue = false;
            serialized.FindProperty("supportsPanelBypass").boolValue = false;
            serialized.FindProperty("supportsFoamBlock").boolValue = true;
            serialized.FindProperty("movingTransform").objectReferenceValue = pivot;
            serialized.FindProperty("motionType").enumValueIndex = (int)BreachMotionType.Rotate;
            serialized.FindProperty("interactionLabel").stringValue = "side pressure hatch";

            SerializedProperty highlightRenderers = serialized.FindProperty("highlightRenderers");
            highlightRenderers.arraySize = 1;
            highlightRenderers.GetArrayElementAtIndex(0).objectReferenceValue = leaf.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static Material LoadOrCreateTintedMaterial(string assetPath, string templatePath, Color tint)
        {
            EnsureFolder("Assets/Game");
            EnsureFolder("Assets/Game/Generated");
            EnsureFolder("Assets/Game/Generated/Materials");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Material template = AssetDatabase.LoadAssetAtPath<Material>(templatePath);
                material = Object.Instantiate(template);
                material.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }
    }
}
