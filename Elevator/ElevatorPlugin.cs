// Elevator
// a Valheim mod skeleton using Jötunn
// 
// File:    Elevator.cs
// Project: Elevator

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elevator
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ElevatorPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.Elevator";
        public const string PluginName = "Elevator";
        public const string PluginVersion = "0.0.1";
        private AssetBundle embeddedResourceBundle;
        private GameObject elevatorBasePrefab;
        private GameObject kitBashRoot;
        private Harmony harmony;

        private void Awake()
        {
            kitBashRoot = new GameObject("KitBashRoot");
            Object.DontDestroyOnLoad(kitBashRoot);
            kitBashRoot.SetActive(false);

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(Patches));
            // Do all your init stuff here
            // Acceptable value ranges can be defined to allow configuration via a slider in the BepInEx ConfigurationManager: https://github.com/BepInEx/BepInEx.ConfigurationManager
            Config.Bind<int>("Main Section", "Example configuration integer", 1, new ConfigDescription("This is an example config, using a range limitation for ConfigurationManager", new AcceptableValueRange<int>(0, 100)));

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ModStub has landed");

            ItemManager.OnVanillaItemsAvailable += RegisterCustomItems;
            PrefabManager.OnPrefabsRegistered += ApplyKitBash;
        }

        public void OnDestroy()
        {
            harmony?.UnpatchAll(PluginGUID);
        }

        private void ApplyKitBash()
        {
            List<KitBashConfig> kitbashes = new List<KitBashConfig>
            {
                new KitBashConfig
                {
                    name = "_Combined Mesh [high]",
                    targetParentPath = "New",
                    sourcePrefab = "wood_floor",
                    sourcePath = "New/_Combined Mesh [high]",
                    position = new Vector3(0f, -52.55f, 1f),
                    rotation = Quaternion.Euler(0, 0, 0)
                },
                new KitBashConfig
                {
                    name = "wheel_left",
                    targetParentPath = "New/pivot_left",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.06511331f, -1.12f, -0.89f),
                    rotation = Quaternion.Euler(0, 180, 0)
                },
                new KitBashConfig
                {
                    name = "wheel_right",
                    targetParentPath = "New/pivot_right",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(-0.07488668f, -1.12f, 0.85f),
                    rotation = Quaternion.Euler(0, 0, 0)
                },
                 new KitBashConfig
                {
                    name = "support_left",
                    targetParentPath = "New",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(-0.25f, 0.5580001f, 0.9489999f),
                    rotation = Quaternion.Euler(11.676f, -177.394f, 217.222f),
                    scale = Vector3.one  * -1
                },new KitBashConfig
                {
                    name = "support_right",
                    targetParentPath = "New",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.25f, 0.5699999f, 0.9389999f),
                    rotation = Quaternion.Euler(-11.728f, -2.606f, 37.225f)
                },new KitBashConfig
                {
                    name = "seat",
                    targetParentPath = "New",
                    sourcePrefab = "piece_chair",
                    sourcePath = "New",
                    position = new Vector3(0.7f, 0f, 0.656f),
                    scale = new Vector3(0.74871f, 0.61419f, 0.63284f)
                },new KitBashConfig
                {
                    name = "gear",
                    targetParentPath = "New/pivot_left",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.007_ArtisanTable.019",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.3f, -1.2f, 1.17f),
                    rotation = Quaternion.Euler(0f, 90f, 0f),
                    materialRemap = new int[]{ 1, 0 }
                },new KitBashConfig
                {
                    name = "handhold",
                    targetParentPath = "New/pivot_left",
                    sourcePrefab = "piece_stonecutter",
                    sourcePath = "Stonecutterbench_destruction/Stonecutter_destruction.001_Stonecutter_destruction.001_Workbench.001",
                    materialPrefab=  "piece_spinningwheel",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.221f, -0.16f, -0.2f),
                    rotation = Quaternion.Euler(0f, 5.765f, -89.982f),
                    scale = Vector3.one * 0.1f
                },
            };
            KitBash(elevatorBasePrefab, kitbashes);
            //FixCollisionLayers(elevatorBasePrefab);
        }

        public void FixCollisionLayers(GameObject r)
        {
            int layer = (r.layer = LayerMask.NameToLayer("piece"));
            Transform[] componentsInChildren = r.transform.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                
                componentsInChildren[i].gameObject.layer = layer;
            }
        }

        private void RegisterCustomItems()
        {
            embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("elevator", typeof(ElevatorPlugin).Assembly);
            GameObject embeddedPrefab = embeddedResourceBundle.LoadAsset<GameObject>("elevator_base");
            elevatorBasePrefab = Object.Instantiate(embeddedPrefab, kitBashRoot.transform);
            elevatorBasePrefab.name = "piece_elevator";
            Elevator elevatorScript = elevatorBasePrefab.AddComponent<Elevator>();
            elevatorBasePrefab.AddComponent<MoveableBaseRoot>();
            elevatorBasePrefab.AddComponent<MoveableBaseElevatorSync>();
         
            elevatorBasePrefab.transform.Find("wheel_collider").gameObject.AddComponent<ElevatorControlls>();
            PrefabManager.Instance.AddPrefab(elevatorBasePrefab);
            PrefabManager.Instance.RegisterToZNetScene(elevatorBasePrefab);
            //  elevatorBase = new CustomPiece(embeddedResourceBundle, "Assets/PrefabInstance/elevator_base.prefab", "Hammer", true);

            //PieceManager.Instance.AddPiece(elevatorBase);

        }

        private void KitBash(GameObject piecePrefab, List<KitBashConfig> kitbashes)
        {
            foreach (KitBashConfig config in kitbashes)
            {
                GameObject sourcePrefab = PrefabManager.Instance.GetPrefab(config.sourcePrefab);
                if (!sourcePrefab)
                {
                    Jotunn.Logger.LogWarning("No prefab found for " + config);
                    continue;
                }
                GameObject sourceGameObject = sourcePrefab.transform.Find(config.sourcePath).gameObject;

                Transform parentTransform = config.targetParentPath.Length > 0 ? piecePrefab.transform.Find(config.targetParentPath) : piecePrefab.transform;

                GameObject kitBashObject = Object.Instantiate(sourceGameObject, parentTransform);
                kitBashObject.name = config.name;
                kitBashObject.transform.localPosition = config.position;
                kitBashObject.transform.localRotation = config.rotation;
                kitBashObject.transform.localScale = config.scale;


                if (config.materialPath != null)
                {
                    GameObject materialPrefab = config.materialPrefab != null ? PrefabManager.Instance.GetPrefab(config.materialPrefab) : sourcePrefab;
                    GameObject materialSourceObject = materialPrefab.transform.Find(config.materialPath).gameObject;
                    Material[] sourceMaterials;
                    MeshRenderer sourceMeshRenderer = materialSourceObject.GetComponent<MeshRenderer>();
                    SkinnedMeshRenderer sourceSkinnedMeshRenderer = materialSourceObject.GetComponent<SkinnedMeshRenderer>();



                    if (sourceMeshRenderer)
                    {
                        sourceMaterials = sourceMeshRenderer.sharedMaterials;
                    }
                    else if (sourceSkinnedMeshRenderer)
                    {
                        sourceMaterials = sourceSkinnedMeshRenderer.sharedMaterials;
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning("No materials found for " + config);
                        continue;
                    }

                    if (config.materialRemap != null)
                    {
                        Material[] remapped = new Material[sourceMaterials.Length];
                        for (int i = 0; i < sourceMaterials.Length; i++)
                        {
                            remapped[config.materialRemap[i]] = sourceMaterials[i];
                        }
                        sourceMaterials = remapped;
                    }

                    SkinnedMeshRenderer targetSkinnedMeshRenderer = kitBashObject.GetComponent<SkinnedMeshRenderer>();
                    if (targetSkinnedMeshRenderer)
                    {
                        targetSkinnedMeshRenderer.sharedMaterials = sourceMaterials;
                        targetSkinnedMeshRenderer.materials = sourceMaterials;
                    }

                    MeshRenderer targetMeshRenderer = kitBashObject.GetComponent<MeshRenderer>();
                    if (targetMeshRenderer)
                    {
                        targetMeshRenderer.sharedMaterials = sourceMaterials;
                        targetMeshRenderer.materials = sourceMaterials;
                    }

                }

            }
        }

        private class KitBashConfig
        {
            public string name;
            public string targetParentPath;
            public string sourcePrefab;
            public string sourcePath;
            public string materialPrefab;
            public string materialPath;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            internal Vector3 scale = Vector3.one;
            internal int[] materialRemap;
        }
#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            { // Set a breakpoint here to break on F9 key press
                Jotunn.Logger.LogInfo("Right here");
            }
        }
#endif
    }
}