using System.Collections.Generic;
using System.IO;
using IndustrialDemo.Breaching;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndustrialDemo.Editor
{
    public static class IndustrialSceneExpansionBuilder
    {
        private const string ScenePath = "Assets/Industrial_Demo.unity/Industrial_Demo.unity";

        private static readonly Dictionary<string, Material> MaterialCache = new();

        [MenuItem("Industrial Demo/Build Scene Expansion")]
        public static void BuildSceneExpansion()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform geoRoot = EnsureChild(EnsureChild(EnsureRoot("Industrial_Demo_Root"), "Geo"), "Expansion");
            Transform zone02Root = GameObject.Find("Industrial_Demo_Root/Geo/Zone_02_CentralHall")?.transform;

            if (zone02Root != null)
            {
                RebuildZone(zone02Root, "InteriorRetrofit", BuildZone02Retrofit);
            }

            RebuildZone(geoRoot, "Connector_05_06", BuildConnector0506);
            RebuildZone(geoRoot, "Zone_06_TankYard", BuildTankYard);
            RebuildZone(geoRoot, "Connector_06_07", BuildConnector0607);
            RebuildZone(geoRoot, "Zone_07_LoadingHangar", BuildLoadingHangar);
            RebuildZone(geoRoot, "Connector_07_08", BuildConnector0708);
            RebuildZone(geoRoot, "Zone_08_ControlEvac", BuildControlEvac);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Industrial scene expansion rebuilt.");
        }

        private static void BuildZone02Retrofit(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");

            CreateBlock(shell, "Ceiling", new Vector3(0f, 4.2f, 6f), new Vector3(20f, 0.35f, 24f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "Wall_L", new Vector3(-9.6f, 2f, 6f), new Vector3(0.45f, 4f, 24f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Wall_R", new Vector3(9.6f, 2f, 6f), new Vector3(0.45f, 4f, 24f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "RearCap_L", new Vector3(-6.2f, 2f, -5.4f), new Vector3(6f, 4f, 0.35f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "RearCap_R", new Vector3(6.2f, 2f, -5.4f), new Vector3(6f, 4f, 0.35f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "ForwardCap_L", new Vector3(-6.3f, 2f, 17.4f), new Vector3(6f, 4f, 0.35f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "ForwardCap_R", new Vector3(6.3f, 2f, 17.4f), new Vector3(6f, 4f, 0.35f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
        }

        private static void BuildConnector0506(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");
            Transform props = EnsureChild(zoneRoot, "Props");
            Transform doors = EnsureChild(zoneRoot, "Doors");

            CreateBlock(shell, "Floor", new Vector3(0f, -0.15f, 98f), new Vector3(8f, 0.3f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Asphalt.mat"));
            CreateBlock(shell, "Wall_L", new Vector3(-4.1f, 2f, 98f), new Vector3(0.4f, 4f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Wall_R", new Vector3(4.1f, 2f, 98f), new Vector3(0.4f, 4f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Ceiling", new Vector3(0f, 4.05f, 98f), new Vector3(8.2f, 0.3f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "HazardStripe", new Vector3(0f, 0.01f, 98f), new Vector3(1.5f, 0.02f, 17f), LoadMaterial("Assets/Industrial_Demo_Mat_Hazard.mat"));
            CreateBlock(shell, "DoorFrame", new Vector3(0f, 2f, 106.3f), new Vector3(4.8f, 4f, 0.3f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));

            InstantiateAsset(props, "Generator_A", "Assets/RPG_FPS_game_assets_industrial/Other_props/Generators/Generator_v1/Generator_v1.prefab", new Vector3(-2.6f, 0f, 94f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "ElectricBox_A", "Assets/RPG_FPS_game_assets_industrial/Other_props/Electric_box/Electric_box_v2/Electric_box_v2.prefab", new Vector3(2.9f, 0f, 95.6f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
            InstantiateAsset(props, "Cone_A", "Assets/Industrial Props Pack/models/road_cone/road_cone_dirty.FBX", new Vector3(-1f, 0f, 101.4f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "Cone_B", "Assets/Industrial Props Pack/models/road_cone/road_cone_clear.FBX", new Vector3(1.2f, 0f, 101.8f), Quaternion.Euler(0f, 20f, 0f), Vector3.one);
            CreateSlidingDoor(doors, "Door_05_06_Bulkhead", new Vector3(0f, 0f, 106.15f), Quaternion.identity, 3.5f, 3.4f, 0.28f, "bulkhead shutter", true, true, false);
        }

        private static void BuildTankYard(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");
            Transform architecture = EnsureChild(zoneRoot, "Architecture");
            Transform props = EnsureChild(zoneRoot, "Props");
            Transform fx = EnsureChild(zoneRoot, "FX");
            Transform doors = EnsureChild(zoneRoot, "Doors");

            CreateBlock(shell, "Floor", new Vector3(0f, -0.15f, 123f), new Vector3(34f, 0.3f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Asphalt.mat"));
            CreateBlock(shell, "BackWall", new Vector3(0f, 3f, 139.8f), new Vector3(34f, 6f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "LeftWallBase", new Vector3(-16.8f, 1.4f, 123f), new Vector3(0.6f, 2.8f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "RightWallBase", new Vector3(16.8f, 1.4f, 123f), new Vector3(0.6f, 2.8f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Ceiling", new Vector3(0f, 6.2f, 123f), new Vector3(34f, 0.4f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "FrontWall_L", new Vector3(-12.5f, 3f, 106.2f), new Vector3(9f, 6f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "FrontWall_R", new Vector3(12.5f, 3f, 106.2f), new Vector3(9f, 6f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "UpperLeftWall", new Vector3(-16.8f, 4.7f, 123f), new Vector3(0.6f, 3f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "UpperRightWall", new Vector3(16.8f, 4.7f, 123f), new Vector3(0.6f, 3f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "CenterLane", new Vector3(0f, 0.015f, 123f), new Vector3(2.2f, 0.03f, 30f), LoadMaterial("Assets/Industrial_Demo_Mat_Hazard.mat"));
            CreateBlock(shell, "FrontThreshold", new Vector3(0f, 0.1f, 106.5f), new Vector3(10f, 0.2f, 1.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));

            InstantiateAsset(architecture, "Gate", "Assets/RPG_FPS_game_assets_industrial/Fences/Concrete_fences/Concrete_fence_v2/Concrete_fence_v2_Gate.prefab", new Vector3(0f, 0f, 109.5f), Quaternion.identity, Vector3.one);
            InstantiateAsset(architecture, "OilTank_Left", "Assets/RPG_FPS_game_assets_industrial/Oil_tanks/Oil_tank_v1/Oil_tank_v1.prefab", new Vector3(-9.5f, 0f, 121.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.05f);
            InstantiateAsset(architecture, "OilTank_Right", "Assets/RPG_FPS_game_assets_industrial/Oil_tanks/Oil_tank_v2/Oil_tank_v2.prefab", new Vector3(9.5f, 0f, 127.5f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
            InstantiateAsset(architecture, "Container_A", "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Cargo_container_v1_LD1through.prefab", new Vector3(-11.2f, 0f, 134.2f), Quaternion.identity, Vector3.one);
            InstantiateAsset(architecture, "Container_B", "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Cargo_container_v1_LD1close.prefab", new Vector3(11.5f, 0f, 114.4f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateAsset(architecture, "Fence_L", "Assets/RPG_FPS_game_assets_industrial/Fences/Concrete_fences/Concrete_fence_v2/Concrete_fence_v2_L.prefab", new Vector3(-15.8f, 0f, 122.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(architecture, "Fence_R", "Assets/RPG_FPS_game_assets_industrial/Fences/Concrete_fences/Concrete_fence_v2/Concrete_fence_v2_L.prefab", new Vector3(15.8f, 0f, 122.5f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);

            InstantiateAsset(props, "PalletStack", "Assets/RPG_FPS_game_assets_industrial/Other_props/Palets/Palet_v1/Palet_v1_set.prefab", new Vector3(-4.8f, 0f, 132.8f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "BagsPallet", "Assets/RPG_FPS_game_assets_industrial/Other_props/Palets/Bags_on_pallet_v1/Bags_on_pallet_v1_1.prefab", new Vector3(4.9f, 0f, 118.6f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateAsset(props, "BarrelCluster", "Assets/RPG_FPS_game_assets_industrial/Barrels/Barrel_v2/Barrel_v2_quadro.prefab", new Vector3(5.6f, 0f, 134.8f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "Dumpster", "Assets/RPG_FPS_game_assets_industrial/Dumpsters/Dumpsters_v1/Dumpsters_v1_empty.prefab", new Vector3(-13.4f, 0f, 114.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "RoadBlock", "Assets/RPG_FPS_game_assets_industrial/Fences/Road_blocks/Road_block_v1/Road_block_v1.prefab", new Vector3(0f, 0f, 131.5f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "TrafficSign", "Assets/Industrial Props Pack/models/traffic_sign/traffic_sign.FBX", new Vector3(13.6f, 0f, 110.8f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);

            InstantiateAsset(fx, "Smoke_Left", "Assets/RPG_FPS_game_assets_industrial/Particles/Smoke/Smoke_v1/Smoke_v1.prefab", new Vector3(-9.8f, 0f, 121.8f), Quaternion.identity, Vector3.one);
            InstantiateAsset(fx, "Smoke_Right", "Assets/RPG_FPS_game_assets_industrial/Particles/Smoke/Smoke_v1/Smoke_v1.prefab", new Vector3(9.2f, 0f, 127.6f), Quaternion.identity, Vector3.one);

            GameObject tankDoorLeft = CreateSwingDoor(doors, "Tank_ServiceDoor_L", new Vector3(-16.52f, 0f, 131.6f), Quaternion.Euler(0f, 90f, 0f), 1.45f, 2.7f, 0.14f, "tank service door", false, false, true);
            CreateWallPanelConsole(doors, "Tank_ServicePanel_L", new Vector3(-14.9f, 1.15f, 130.2f), Quaternion.Euler(0f, 90f, 0f), "Unlock tank door", tankDoorLeft.GetComponent<BreachableEntry>());
            CreateSwingDoor(doors, "Tank_ServiceDoor_R", new Vector3(16.52f, 0f, 116.7f), Quaternion.Euler(0f, 270f, 0f), 1.45f, 2.7f, 0.14f, "service hatch", true, true, false);
        }

        private static void BuildConnector0607(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");
            Transform props = EnsureChild(zoneRoot, "Props");
            Transform doors = EnsureChild(zoneRoot, "Doors");

            CreateBlock(shell, "Floor", new Vector3(0f, -0.15f, 149f), new Vector3(10f, 0.3f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Wall_L", new Vector3(-5.1f, 2.2f, 149f), new Vector3(0.4f, 4.4f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Wall_R", new Vector3(5.1f, 2.2f, 149f), new Vector3(0.4f, 4.4f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Ceiling", new Vector3(0f, 4.35f, 149f), new Vector3(10.2f, 0.3f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "Stripe", new Vector3(0f, 0.01f, 149f), new Vector3(1.2f, 0.02f, 16f), LoadMaterial("Assets/Industrial_Demo_Mat_Hazard.mat"));

            InstantiateAsset(props, "PipeSet", "Assets/RPG_FPS_game_assets_industrial/Other_props/Pipes/Pipe_sets/Pipes_set_v1/Pipes_set_v1_H_set_v2.prefab", new Vector3(-3.5f, 0f, 146.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "ElectricBox", "Assets/RPG_FPS_game_assets_industrial/Other_props/Electric_box/Electric_box_v1/Electric_box_v1.prefab", new Vector3(3.4f, 0f, 152.5f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
            InstantiateAsset(props, "Ladder", "Assets/Industrial Props Pack/models/ladders/ladder.FBX", new Vector3(4.2f, 0f, 145.5f), Quaternion.identity, Vector3.one);
            CreateSlidingDoor(doors, "Door_06_07_Shutter", new Vector3(0f, 0f, 156.15f), Quaternion.identity, 3.6f, 3.3f, 0.28f, "security shutter", true, true, false);
        }

        private static void BuildLoadingHangar(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");
            Transform architecture = EnsureChild(zoneRoot, "Architecture");
            Transform props = EnsureChild(zoneRoot, "Props");
            Transform doors = EnsureChild(zoneRoot, "Doors");

            CreateBlock(shell, "Floor", new Vector3(0f, -0.15f, 179f), new Vector3(36f, 0.3f, 46f), LoadMaterial("Assets/Industrial_Demo_Mat_Asphalt.mat"));
            CreateBlock(shell, "Ceiling", new Vector3(0f, 8.2f, 179f), new Vector3(36f, 0.4f, 46f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "SideWall_L", new Vector3(-18.0f, 4f, 179f), new Vector3(0.5f, 8f, 46f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "SideWall_R", new Vector3(18.0f, 4f, 179f), new Vector3(0.5f, 8f, 46f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "FrontWall_L", new Vector3(-12.2f, 4f, 156.2f), new Vector3(12f, 8f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "FrontWall_R", new Vector3(12.2f, 4f, 156.2f), new Vector3(12f, 8f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "RearWall_L", new Vector3(-12.2f, 4f, 201.8f), new Vector3(12f, 8f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "RearWall_R", new Vector3(12.2f, 4f, 201.8f), new Vector3(12f, 8f, 0.5f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "HazardLane_A", new Vector3(-5f, 0.01f, 179f), new Vector3(2f, 0.02f, 36f), LoadMaterial("Assets/Industrial_Demo_Mat_Hazard.mat"));
            CreateBlock(shell, "HazardLane_B", new Vector3(5f, 0.01f, 179f), new Vector3(2f, 0.02f, 36f), LoadMaterial("Assets/Industrial_Demo_Mat_Hazard.mat"));
            CreateBlock(shell, "RearSeal", new Vector3(0f, 2.2f, 201.8f), new Vector3(16f, 4.4f, 0.4f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));

            InstantiateAsset(architecture, "HangarMain", "Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Hangar_v2_basic.prefab", new Vector3(0f, 0f, 179f), Quaternion.identity, Vector3.one);
            InstantiateAsset(architecture, "Outbuilding_L", "Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Hangar_v2_outbuilding.prefab", new Vector3(-15.4f, 0f, 186f), Quaternion.identity, Vector3.one);
            InstantiateAsset(architecture, "Outbuilding_R", "Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Hangar_v2_outbuilding2.prefab", new Vector3(15.2f, 0f, 171f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateAsset(architecture, "Container_A", "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Cargo_container_v1_LD1open.prefab", new Vector3(-8.5f, 0f, 185.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(architecture, "Container_B", "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Cargo_container_v1_LD1through.prefab", new Vector3(8.6f, 0f, 173.8f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
            InstantiateAsset(architecture, "Container_C", "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Cargo_container_v1_LD1close.prefab", new Vector3(0.2f, 0f, 191.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);

            InstantiateAsset(props, "Generator", "Assets/RPG_FPS_game_assets_industrial/Other_props/Generators/Generator_v1/Generator_v1.prefab", new Vector3(-12.8f, 0f, 170.6f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "PipeSet", "Assets/RPG_FPS_game_assets_industrial/Other_props/Pipes/Pipe_sets/Pipes_set_v1/Pipes_set_v1_U_set_v2.prefab", new Vector3(12.2f, 0f, 188f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "Pallet_A", "Assets/Industrial Props Pack/models/pallet/pallet_full_01.FBX", new Vector3(-4.4f, 0f, 170.8f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "Pallet_B", "Assets/Industrial Props Pack/models/pallet/pallet_full_02.FBX", new Vector3(4.7f, 0f, 188.7f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateAsset(props, "Ladder", "Assets/Industrial Props Pack/models/ladders/ladder.FBX", new Vector3(-14f, 0f, 178.6f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "BoxCluster", "Assets/RPG_FPS_game_assets_industrial/Boxes/Wooden_box_v1/Wooden_box_v1_LD1square.prefab", new Vector3(11.8f, 0f, 168.2f), Quaternion.Euler(0f, 30f, 0f), Vector3.one);
            InstantiateAsset(props, "Cone", "Assets/Industrial Props Pack/models/road_cone/road_cone_clear.FBX", new Vector3(1.2f, 0f, 164.2f), Quaternion.identity, Vector3.one);

            CreateSwingDoor(doors, "Hangar_SideDoor_L", new Vector3(-17.72f, 0f, 186.3f), Quaternion.Euler(0f, 90f, 0f), 1.45f, 2.8f, 0.14f, "hangar side door", true, false, false);
            CreateSwingDoor(doors, "Hangar_SideDoor_R", new Vector3(17.72f, 0f, 170.9f), Quaternion.Euler(0f, 270f, 0f), 1.45f, 2.8f, 0.14f, "control access door", true, true, false);
            GameObject hangarRearDoor = CreateSlidingDoor(doors, "Hangar_RearShutter", new Vector3(0f, 0f, 201.45f), Quaternion.identity, 3.8f, 3.5f, 0.28f, "hangar rear shutter", false, false, true);
            CreateWallPanelConsole(doors, "Hangar_RearPanel", new Vector3(4.8f, 1.2f, 200.9f), Quaternion.identity, "Open rear shutter", hangarRearDoor.GetComponent<BreachableEntry>());
        }

        private static void BuildConnector0708(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");
            Transform props = EnsureChild(zoneRoot, "Props");
            Transform doors = EnsureChild(zoneRoot, "Doors");

            CreateBlock(shell, "Floor", new Vector3(0f, -0.15f, 214f), new Vector3(8f, 0.3f, 16f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "Wall_L", new Vector3(-4.1f, 2.15f, 214f), new Vector3(0.4f, 4.3f, 16f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Wall_R", new Vector3(4.1f, 2.15f, 214f), new Vector3(0.4f, 4.3f, 16f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Ceiling", new Vector3(0f, 4.2f, 214f), new Vector3(8.2f, 0.3f, 16f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "Window", new Vector3(0f, 2f, 220.7f), new Vector3(3.5f, 1.6f, 0.08f), LoadMaterial("Assets/Industrial_Demo_Mat_GlassPartition.mat"));

            InstantiateAsset(props, "ElectricBox", "Assets/RPG_FPS_game_assets_industrial/Other_props/Electric_box/Electric_box_v3/Electric_box_v3.prefab", new Vector3(-2.6f, 0f, 211.2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "PaperBox", "Assets/Industrial Props Pack/models/paper_box/paper_box.FBX", new Vector3(2.7f, 0f, 216.8f), Quaternion.Euler(0f, 15f, 0f), Vector3.one);
            CreateSwingDoor(doors, "Connector_ServiceDoor", new Vector3(-3.72f, 0f, 216.4f), Quaternion.Euler(0f, 90f, 0f), 1.3f, 2.6f, 0.14f, "service access door", true, false, false);
        }

        private static void BuildControlEvac(Transform zoneRoot)
        {
            Transform shell = EnsureChild(zoneRoot, "Shell");
            Transform architecture = EnsureChild(zoneRoot, "Architecture");
            Transform props = EnsureChild(zoneRoot, "Props");
            Transform doors = EnsureChild(zoneRoot, "Doors");

            CreateBlock(shell, "ControlFloor", new Vector3(0f, -0.15f, 236f), new Vector3(24f, 0.3f, 16f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "BayFloor", new Vector3(0f, -0.15f, 252f), new Vector3(28f, 0.3f, 18f), LoadMaterial("Assets/Industrial_Demo_Mat_Asphalt.mat"));
            CreateBlock(shell, "Ceiling", new Vector3(0f, 7.4f, 245f), new Vector3(28f, 0.4f, 34f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateBlock(shell, "HazardLane", new Vector3(0f, 0.01f, 252f), new Vector3(2.2f, 0.02f, 15f), LoadMaterial("Assets/Industrial_Demo_Mat_Hazard.mat"));
            CreateBlock(shell, "Wall_L", new Vector3(-12.2f, 2.5f, 242f), new Vector3(0.4f, 5f, 32f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "Wall_R", new Vector3(12.2f, 2.5f, 242f), new Vector3(0.4f, 5f, 32f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "ControlRearWall", new Vector3(0f, 2.5f, 228.2f), new Vector3(24f, 5f, 0.4f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "BayRearCap_L", new Vector3(-9f, 2.5f, 260.8f), new Vector3(8f, 5f, 0.4f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "BayRearCap_R", new Vector3(9f, 2.5f, 260.8f), new Vector3(8f, 5f, 0.4f), LoadMaterial("Assets/Industrial_Demo_Mat_Concrete.mat"));
            CreateBlock(shell, "WindowLeft", new Vector3(-4.2f, 2.1f, 243.2f), new Vector3(6f, 1.8f, 0.08f), LoadMaterial("Assets/Industrial_Demo_Mat_Glass.mat"));
            CreateBlock(shell, "WindowRight", new Vector3(4.2f, 2.1f, 243.2f), new Vector3(6f, 1.8f, 0.08f), LoadMaterial("Assets/Industrial_Demo_Mat_Glass.mat"));
            CreateBlock(shell, "ExitDoor", new Vector3(0f, 2.1f, 260.8f), new Vector3(4f, 4.2f, 0.3f), LoadMaterial("Assets/Industrial_Demo_Mat_Evac.mat"));

            InstantiateAsset(architecture, "HangarBackdrop", "Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v4/Hangar_v4.prefab", new Vector3(0f, 0f, 252f), Quaternion.identity, Vector3.one);
            InstantiateAsset(architecture, "ConcreteWallSet", "Assets/RPG_FPS_game_assets_industrial/Fences/Concrete_fences/Concrete_fence_v1/Concrete_fence_v1_wall_set_v1.prefab", new Vector3(-9.6f, 0f, 233.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(architecture, "ConcreteWallSet_B", "Assets/RPG_FPS_game_assets_industrial/Fences/Concrete_fences/Concrete_fence_v1/Concrete_fence_v1_wall_set_v2.prefab", new Vector3(9.6f, 0f, 233.5f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);

            InstantiateAsset(props, "Generator", "Assets/RPG_FPS_game_assets_industrial/Other_props/Generators/Generator_v1/Generator_v1.prefab", new Vector3(-7.6f, 0f, 233.4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "ElectricBox", "Assets/RPG_FPS_game_assets_industrial/Other_props/Electric_box/Electric_box_v2/Electric_box_v2.prefab", new Vector3(7.8f, 0f, 233.1f), Quaternion.Euler(0f, 270f, 0f), Vector3.one);
            InstantiateAsset(props, "Container", "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Cargo_container_v1_LD1open.prefab", new Vector3(8.2f, 0f, 249.6f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "Dumpster", "Assets/RPG_FPS_game_assets_industrial/Dumpsters/Dumpsters_v1/Dumpsters_v1_garbadge.prefab", new Vector3(-8.8f, 0f, 250.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            InstantiateAsset(props, "Conditioner", "Assets/RPG_FPS_game_assets_industrial/Other_props/Conditioners/Conditioner_v1/Conditioner_v1.prefab", new Vector3(0f, 0f, 229.6f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiateAsset(props, "Barrel", "Assets/Industrial Props Pack/models/barrel/barrel.FBX", new Vector3(2.6f, 0f, 247.1f), Quaternion.identity, Vector3.one);
            InstantiateAsset(props, "TrashCan", "Assets/Industrial Props Pack/models/trash_can/trash can.FBX", new Vector3(-2.7f, 0f, 246.4f), Quaternion.identity, Vector3.one);

            CreateSwingDoor(doors, "Control_ServiceDoor_L", new Vector3(-11.92f, 0f, 235.1f), Quaternion.Euler(0f, 90f, 0f), 1.35f, 2.7f, 0.14f, "control room door", true, true, false);
            CreateSlidingDoor(doors, "Final_ExitDoor", new Vector3(0f, 0f, 260.55f), Quaternion.identity, 3.9f, 3.9f, 0.28f, "final exit shutter", true, true, false);
        }

        private static void RebuildZone(Transform parent, string name, System.Action<Transform> build)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform zoneRoot = new GameObject(name).transform;
            zoneRoot.SetParent(parent, false);
            build(zoneRoot);
        }

        private static Transform EnsureRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root != null)
            {
                return root.transform;
            }

            return new GameObject(name).transform;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static GameObject InstantiateAsset(Transform parent, string name, string assetPath, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"Missing asset for scene expansion: {assetPath}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            if (instance == null)
            {
                return null;
            }

            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            ApplyCompatibilityMaterial(assetPath, instance);
            return instance;
        }

        private static GameObject CreateSwingDoor(Transform parent, string name, Vector3 position, Quaternion rotation, float width, float height, float thickness, string label, bool supportsManual, bool supportsForced, bool supportsPanelBypass)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            Transform frame = EnsureChild(root.transform, "Frame");
            Transform leafPivot = new GameObject("LeafPivot").transform;
            leafPivot.SetParent(root.transform, false);
            leafPivot.localPosition = new Vector3(-width * 0.5f, 0f, 0f);

            CreateLocalBlock(frame, "FrameLeft", new Vector3(-width * 0.5f - 0.08f, height * 0.5f, 0f), new Vector3(0.16f, height + 0.18f, thickness + 0.12f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateLocalBlock(frame, "FrameRight", new Vector3(width * 0.5f + 0.08f, height * 0.5f, 0f), new Vector3(0.16f, height + 0.18f, thickness + 0.12f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateLocalBlock(frame, "FrameTop", new Vector3(0f, height + 0.09f, 0f), new Vector3(width + 0.32f, 0.18f, thickness + 0.12f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));

            GameObject leaf = CreateLocalBlock(leafPivot, "Leaf", new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(width, height, thickness), LoadMaterial("Assets/Industrial_Demo_Mat_Evac.mat"));
            Renderer leafRenderer = leaf.GetComponent<Renderer>();

            BreachableEntry entry = root.AddComponent<BreachableEntry>();
            ConfigureBreachableEntry(entry, leafPivot, label, BreachableEntryType.StandardDoor, BreachMotionType.Rotate, supportsManual, supportsForced, supportsPanelBypass, 105f, 2.2f, 1.0f, 0.45f, leafRenderer);
            return root;
        }

        private static GameObject CreateSlidingDoor(Transform parent, string name, Vector3 position, Quaternion rotation, float width, float height, float thickness, string label, bool supportsManual, bool supportsForced, bool supportsPanelBypass)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            Transform frame = EnsureChild(root.transform, "Frame");
            CreateLocalBlock(frame, "FrameLeft", new Vector3(-width * 0.5f - 0.1f, height * 0.5f, 0f), new Vector3(0.2f, height + 0.22f, thickness + 0.14f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateLocalBlock(frame, "FrameRight", new Vector3(width * 0.5f + 0.1f, height * 0.5f, 0f), new Vector3(0.2f, height + 0.22f, thickness + 0.14f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            CreateLocalBlock(frame, "FrameTop", new Vector3(0f, height + 0.11f, 0f), new Vector3(width + 0.4f, 0.22f, thickness + 0.14f), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));

            GameObject leaf = CreateLocalBlock(root.transform, "Leaf", new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, thickness), LoadMaterial("Assets/Industrial_Demo_Mat_Steel.mat"));
            Renderer leafRenderer = leaf.GetComponent<Renderer>();

            BreachableEntry entry = root.AddComponent<BreachableEntry>();
            ConfigureBreachableEntry(entry, leaf.transform, label, BreachableEntryType.MetalShutter, BreachMotionType.Slide, supportsManual, supportsForced, supportsPanelBypass, 0f, height * 0.95f, 1.05f, 0.4f, leafRenderer);
            return root;
        }

        private static GameObject CreateWallPanelConsole(Transform parent, string name, Vector3 position, Quaternion rotation, string promptLabel, BreachableEntry linkedEntry)
        {
            GameObject panel = CreatePrimitiveChild(parent, name, PrimitiveType.Cube);
            panel.transform.SetPositionAndRotation(position, rotation);
            panel.transform.localScale = new Vector3(0.42f, 0.95f, 0.16f);

            Renderer renderer = panel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = LoadMaterial("Assets/Industrial_Demo_Mat_ElectricTex.mat");
            }

            PanelBypassConsole console = panel.AddComponent<PanelBypassConsole>();
            ConfigurePanelConsole(console, linkedEntry, promptLabel, renderer);
            return panel;
        }

        private static void ConfigureBreachableEntry(BreachableEntry entry, Transform movingTransform, string label, BreachableEntryType entryType, BreachMotionType motionType, bool supportsManual, bool supportsForced, bool supportsPanelBypass, float openAngle, float slideDistance, float breachTimeManual, float breachTimeForced, Renderer highlightRenderer)
        {
            SerializedObject serializedObject = new(entry);
            serializedObject.FindProperty("currentState").enumValueIndex = (int)BreachableEntryState.Closed;
            serializedObject.FindProperty("entryType").enumValueIndex = (int)entryType;
            serializedObject.FindProperty("supportsManualBreach").boolValue = supportsManual;
            serializedObject.FindProperty("supportsShotBreach").boolValue = false;
            serializedObject.FindProperty("supportsForcedBreach").boolValue = supportsForced;
            serializedObject.FindProperty("supportsPanelBypass").boolValue = supportsPanelBypass;
            serializedObject.FindProperty("supportsFoamBlock").boolValue = true;
            serializedObject.FindProperty("breachTimeManual").floatValue = breachTimeManual;
            serializedObject.FindProperty("breachTimeForced").floatValue = breachTimeForced;
            serializedObject.FindProperty("noiseManual").floatValue = 1.1f;
            serializedObject.FindProperty("noiseForced").floatValue = 8f;
            serializedObject.FindProperty("noiseShot").floatValue = 6f;
            serializedObject.FindProperty("motionType").enumValueIndex = (int)motionType;
            serializedObject.FindProperty("movingTransform").objectReferenceValue = movingTransform;
            serializedObject.FindProperty("openAngle").floatValue = openAngle;
            serializedObject.FindProperty("slideDistance").floatValue = slideDistance;
            serializedObject.FindProperty("openDuration").floatValue = 0.42f;
            serializedObject.FindProperty("interactionLabel").stringValue = label;

            SerializedProperty highlightRenderers = serializedObject.FindProperty("highlightRenderers");
            highlightRenderers.arraySize = highlightRenderer != null ? 1 : 0;
            if (highlightRenderer != null)
            {
                highlightRenderers.GetArrayElementAtIndex(0).objectReferenceValue = highlightRenderer;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePanelConsole(PanelBypassConsole console, BreachableEntry linkedEntry, string promptLabel, Renderer highlightRenderer)
        {
            SerializedObject serializedObject = new(console);
            serializedObject.FindProperty("linkedEntry").objectReferenceValue = linkedEntry;
            serializedObject.FindProperty("promptLabel").stringValue = promptLabel;
            serializedObject.FindProperty("singleUse").boolValue = true;

            SerializedProperty highlightRenderers = serializedObject.FindProperty("highlightRenderers");
            highlightRenderers.arraySize = highlightRenderer != null ? 1 : 0;
            if (highlightRenderer != null)
            {
                highlightRenderers.GetArrayElementAtIndex(0).objectReferenceValue = highlightRenderer;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateLocalBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject block = CreatePrimitiveChild(parent, name, PrimitiveType.Cube);
            block.transform.localPosition = localPosition;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = localScale;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType primitiveType)
        {
            GameObject child = GameObject.CreatePrimitive(primitiveType);
            child.name = name;
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ApplyCompatibilityMaterial(string assetPath, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                {
                    continue;
                }

                Material[] convertedMaterials = new Material[sourceMaterials.Length];
                bool changed = false;

                for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                {
                    Material sourceMaterial = sourceMaterials[materialIndex];
                    Material compatibleMaterial = ResolveCompatibilityMaterial(assetPath, sourceMaterial);
                    convertedMaterials[materialIndex] = compatibleMaterial != null ? compatibleMaterial : sourceMaterial;
                    changed |= convertedMaterials[materialIndex] != sourceMaterial;
                }

                if (changed)
                {
                    renderer.sharedMaterials = convertedMaterials;
                }
            }
        }

        private static Material ResolveCompatibilityMaterial(string assetPath, Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            string sourceMaterialPath = AssetDatabase.GetAssetPath(sourceMaterial);
            if (string.IsNullOrEmpty(sourceMaterialPath))
            {
                return null;
            }

            if (sourceMaterialPath.StartsWith("Assets/Game/Generated/Materials/"))
            {
                return sourceMaterial;
            }

            if (!NeedsCompatibilityMaterial(assetPath, sourceMaterialPath, sourceMaterial))
            {
                return null;
            }

            string outputPath = $"Assets/Game/Generated/Materials/Compatibility/{MakeSafeAssetName(sourceMaterialPath)}_HDRP.mat";
            string templatePath = ChooseCompatibilityTemplate(sourceMaterialPath);
            return EnsureHdrpTexturedMaterial(outputPath, templatePath, sourceMaterialPath);
        }

        private static bool NeedsCompatibilityMaterial(string assetPath, string sourceMaterialPath, Material sourceMaterial)
        {
            return assetPath.StartsWith("Assets/RPG_FPS_game_assets_industrial/") ||
                   assetPath.StartsWith("Assets/Industrial Props Pack/") ||
                   sourceMaterialPath.StartsWith("Assets/RPG_FPS_game_assets_industrial/") ||
                   sourceMaterialPath.StartsWith("Assets/Industrial Props Pack/");
        }

        private static string ChooseCompatibilityTemplate(string sourceMaterialPath)
        {
            string path = sourceMaterialPath.ToLowerInvariant();
            if (path.Contains("electric"))
            {
                return "Assets/Industrial_Demo_Mat_ElectricTex.mat";
            }

            if (path.Contains("hazard") || path.Contains("cone") || path.Contains("traffic"))
            {
                return "Assets/Industrial_Demo_Mat_Hazard.mat";
            }

            if (path.Contains("wood") || path.Contains("pallet") || path.Contains("palet") || path.Contains("bag") || path.Contains("paper") || path.Contains("box"))
            {
                return "Assets/Industrial_Demo_Mat_Concrete.mat";
            }

            return "Assets/Industrial_Demo_Mat_Steel.mat";
        }

        private static Material EnsureHdrpTexturedMaterial(string outputPath, string templatePath, string sourceMaterialPath)
        {
            EnsureFolder("Assets/Game");
            EnsureFolder("Assets/Game/Generated");
            EnsureFolder("Assets/Game/Generated/Materials");
            EnsureFolder("Assets/Game/Generated/Materials/Compatibility");

            Material generatedMaterial = AssetDatabase.LoadAssetAtPath<Material>(outputPath);
            if (generatedMaterial == null)
            {
                Material template = AssetDatabase.LoadAssetAtPath<Material>(templatePath);
                if (template == null)
                {
                    return null;
                }

                generatedMaterial = Object.Instantiate(template);
                generatedMaterial.name = Path.GetFileNameWithoutExtension(outputPath);
                AssetDatabase.CreateAsset(generatedMaterial, outputPath);
            }

            Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourceMaterialPath);
            if (sourceMaterial != null)
            {
                Texture texture = null;
                if (sourceMaterial.HasProperty("_BaseColorMap"))
                {
                    texture = sourceMaterial.GetTexture("_BaseColorMap");
                }
                else if (sourceMaterial.HasProperty("_MainTex"))
                {
                    texture = sourceMaterial.GetTexture("_MainTex");
                }

                if (texture != null)
                {
                    if (generatedMaterial.HasProperty("_BaseColorMap"))
                    {
                        generatedMaterial.SetTexture("_BaseColorMap", texture);
                    }
                    else if (generatedMaterial.HasProperty("_MainTex"))
                    {
                        generatedMaterial.SetTexture("_MainTex", texture);
                    }
                }

                if (generatedMaterial.HasProperty("_BaseColor"))
                {
                    Color sourceColor = sourceMaterial.HasProperty("_BaseColor")
                        ? sourceMaterial.GetColor("_BaseColor")
                        : (sourceMaterial.HasProperty("_Color") ? sourceMaterial.GetColor("_Color") : Color.white);
                    generatedMaterial.SetColor("_BaseColor", sourceColor);
                }
                else if (generatedMaterial.HasProperty("_Color"))
                {
                    Color sourceColor = sourceMaterial.HasProperty("_Color")
                        ? sourceMaterial.GetColor("_Color")
                        : (sourceMaterial.HasProperty("_BaseColor") ? sourceMaterial.GetColor("_BaseColor") : Color.white);
                    generatedMaterial.SetColor("_Color", sourceColor);
                }
            }

            EditorUtility.SetDirty(generatedMaterial);
            return generatedMaterial;
        }

        private static string MakeSafeAssetName(string assetPath)
        {
            string withoutExtension = Path.ChangeExtension(assetPath, null)?.Replace("Assets/", string.Empty) ?? "Generated";
            char[] chars = withoutExtension.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!char.IsLetterOrDigit(c))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
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

        private static Material LoadMaterial(string assetPath)
        {
            if (MaterialCache.TryGetValue(assetPath, out Material cached))
            {
                return cached;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            MaterialCache[assetPath] = material;
            return material;
        }
    }
}
