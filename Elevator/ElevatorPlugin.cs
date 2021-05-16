// Elevator
// a Valheim mod skeleton using Jötunn
// 
// File:    Elevator.cs
// Project: Elevator

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
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
    [BepInDependency(Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ElevatorPlugin : BaseUnityPlugin
    {
        public const int ElevatorLayer = 29;
        public const string PluginGUID = "marcopogo.Elevator";
        public const string PluginName = "Elevator";
        public const string PluginVersion = "0.0.1";
        private AssetBundle embeddedResourceBundle;
        private GameObject elevatorBasePrefab; 
        private GameObject elevatorSupportPrefab;
        private GameObject kitBashRoot;
        private Harmony harmony;

        private void Awake()
        {
            kitBashRoot = new GameObject("KitBashRoot");
            DontDestroyOnLoad(kitBashRoot);
            kitBashRoot.SetActive(false);

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(Patches));
            
            ItemManager.OnVanillaItemsAvailable += RegisterCustomItems;
            PrefabManager.OnPrefabsRegistered += ApplyKitBash;
        }

        public void OnDestroy()
        {
            harmony?.UnpatchAll(PluginGUID);
        }

        private void ApplyKitBash()
        {
            KitBashElevatorBase();
            KitBashElevatorSupport();
            //FixCollisionLayers(elevatorBasePrefab);
        }

        private void KitBashElevatorSupport()
        {
            List<KitBashConfig> elevatorSupportKitBashes = new List<KitBashConfig>
            {
                new KitBashConfig
                {
                    name = "New", 
                    sourcePrefab = "wood_wall_roof_top_45",
                    sourcePath = "New",
                    position = new Vector3(0f, 0f, -1f),
                    rotation = Quaternion.Euler(90, 0, 0)
                },
                new KitBashConfig
                {
                    name = "wheel_left",
                    targetParentPath = "New/pivot_left",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.06511331f, 0.8729141f, -1.120428f),
                    rotation = Quaternion.Euler(269.96f, 180, 0)
                },
                new KitBashConfig
                {
                    name = "wheel_right",
                    targetParentPath = "New/pivot_right",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(-0.07488656f, -0.8700893f, -1.121964f),
                    rotation = Quaternion.Euler(-270.04f, 0, 0)
                },
                 new KitBashConfig
                {
                    name = "support_left",
                    targetParentPath = "New",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(-0.2338867f, 0.1241241f, 0.689f),
                    rotation = Quaternion.Euler(281.91f, -167.542f, 204.497f),
                    scale = Vector3.one  * -1
                }, 
                new KitBashConfig
                {
                    name = "support_right",
                    targetParentPath = "New",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.223f, 0.1341143f, 0.7010067f),
                    rotation = Quaternion.Euler(-281.961f, -12.404f, 24.551f)
                },
            };
            KitBash(elevatorSupportPrefab, elevatorSupportKitBashes); 
            GameObject raft = PrefabManager.Instance.GetPrefab("Raft");
            LineRenderer sourceLineRenderer = raft.transform.Find("ship/visual/ropes/left").GetComponent<LineRenderer>();
            
            foreach(LineRenderer lineRenderer in elevatorSupportPrefab.GetComponentsInChildren<LineRenderer>())
            {
                lineRenderer.materials = sourceLineRenderer.materials;
                lineRenderer.startWidth = sourceLineRenderer.startWidth;
                lineRenderer.endWidth = sourceLineRenderer.endWidth;
                lineRenderer.widthCurve = sourceLineRenderer.widthCurve;
                lineRenderer.textureMode = sourceLineRenderer.textureMode;
                lineRenderer.shadowCastingMode = sourceLineRenderer.shadowCastingMode;
                lineRenderer.alignment = sourceLineRenderer.alignment;
                lineRenderer.numCapVertices = sourceLineRenderer.numCapVertices;
                lineRenderer.numCornerVertices = sourceLineRenderer.numCornerVertices;
                lineRenderer.widthMultiplier = sourceLineRenderer.widthMultiplier;
                lineRenderer.generateLightingData = sourceLineRenderer.generateLightingData;
                lineRenderer.material = sourceLineRenderer.material;
                lineRenderer.rayTracingMode = sourceLineRenderer.rayTracingMode;
                lineRenderer.realtimeLightmapIndex = sourceLineRenderer.realtimeLightmapIndex;
                lineRenderer.realtimeLightmapScaleOffset = sourceLineRenderer.realtimeLightmapScaleOffset;
            }
        }
          
        private void KitBashElevatorBase()
        {
            List<KitBashConfig> elevatorBaseKitBashes = new List<KitBashConfig>
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
                },
                     new KitBashConfig
                {
                    name = "support_left_bar",
                    targetParentPath = "New",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.403f, 0.211f, 0.274f),
                    rotation = Quaternion.Euler(-260.316f, -195.346f, 201.557f),
                    scale = Vector3.one  * 0.6f
                },
                new KitBashConfig
                {
                    name = "support_right",
                    targetParentPath = "New",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.25f, 0.5699999f, 0.9389999f),
                    rotation = Quaternion.Euler(-11.728f, -2.606f, 37.225f)
                },
                new KitBashConfig
                {
                    name = "seat",
                    targetParentPath = "New",
                    sourcePrefab = "piece_chair",
                    sourcePath = "New",
                    position = new Vector3(0.7f, 0f, 0.656f),
                    scale = new Vector3(0.74871f, 0.61419f, 0.63284f)
                },
                new KitBashConfig
                {
                    name = "crank_gear",
                    targetParentPath = "New/crank",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.007_ArtisanTable.019",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.4602f, -1.088331f, 0.7863638f),
                    rotation = Quaternion.Euler(0f, 90f, 8.787001f),
                    scale = new Vector3(0.8f, 0.8f, 1f),
                    materialRemap = new int[]{ 1, 0 }
                },
                new KitBashConfig
                {
                    name = "central_gear",
                    targetParentPath = "New/pivot_right",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.28f, -0.894f, 0.585f),
                    rotation = Quaternion.Euler(0f, 90f, 0f),
                    scale = new Vector3(0.8f, 0.8f, 1f),
                    materialRemap = new int[]{ 1, 0 }
                },
                new KitBashConfig
                {
                    name = "sun_gear",
                    targetParentPath = "New",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.847f, -0.111f, 0.621f),
                    rotation = Quaternion.Euler(0f, 90f, 0f),
                    scale = new Vector3(0.8f, 0.8f, 1.7f),
                    materialRemap = new int[]{ 1, 0 }
                },
                new KitBashConfig
                {
                    name = "planet_gear_1",
                    targetParentPath = "New/pivot_right/planet_1",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.847f,-0.7618001f, 0.752f),
                    rotation = Quaternion.Euler(0f, 90f, -11.669f),
                    scale = new Vector3(0.8f, 0.8f, 1.7f),
                    materialRemap = new int[]{ 1, 0 }
                },
                new KitBashConfig
                {
                    name = "planet_gear_2",
                    targetParentPath = "New/pivot_right/planet_2",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.847f,-0.7618001f, 0.752f),
                    rotation = Quaternion.Euler(0f, 90f, -11.669f),
                    scale = new Vector3(0.8f, 0.8f, 1.7f),
                    materialRemap = new int[]{ 1, 0 }
                },
                                new KitBashConfig
                {
                    name = "planet_gear_3",
                    targetParentPath = "New/pivot_right/planet_3",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.847f,-0.7618001f, 0.752f),
                    rotation = Quaternion.Euler(0f, 90f, -11.669f),
                    scale = new Vector3(0.8f, 0.8f, 1.7f),
                    materialRemap = new int[]{ 1, 0 }
                },                              new KitBashConfig
                {
                    name = "planet_gear_4",
                    targetParentPath = "New/pivot_right/planet_4",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-0.847f,-0.7618001f, 0.752f),
                    rotation = Quaternion.Euler(0f, 90f, -11.669f),
                    scale = new Vector3(0.8f, 0.8f, 1.7f),
                    materialRemap = new int[]{ 1, 0 }
                },
                new KitBashConfig
                {
                    name = "handhold",
                    targetParentPath = "New/crank",
                    sourcePrefab = "piece_stonecutter",
                    sourcePath = "Stonecutterbench_destruction/Stonecutter_destruction.001_Stonecutter_destruction.001_Workbench.001",
                    materialPrefab=  "piece_spinningwheel",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.04099999f, -0.1544f, -0.1712f),
                    rotation = Quaternion.Euler(0f, 5.765f, -89.982f),
                    scale = Vector3.one * 0.1f
                },
            };

            KitBash(elevatorBasePrefab, elevatorBaseKitBashes);
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
            try
            { 
                embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("elevator", typeof(ElevatorPlugin).Assembly);
            
                SetupElevatorBase(); 
                SetupElevatorSupport();
            } finally
            {
                ItemManager.OnVanillaItemsAvailable -= RegisterCustomItems;
            }
    
            //  elevatorBase = new CustomPiece(embeddedResourceBundle, "Assets/PrefabInstance/elevator_base.prefab", "Hammer", true);

            //PieceManager.Instance.AddPiece(elevatorBase);

        }

        private void SetupElevatorSupport()
        {
            GameObject embeddedPrefab = embeddedResourceBundle.LoadAsset<GameObject>("elevator_support");
            elevatorSupportPrefab = Instantiate(embeddedPrefab, kitBashRoot.transform);
            elevatorSupportPrefab.name = "piece_elevator_support";
            PieceManager.Instance.AddPiece(new CustomPiece(elevatorSupportPrefab, new PieceConfig()
            {
                PieceTable = "Hammer"
            }));
            elevatorSupportPrefab.AddComponent<ElevatorSupport>();
        }

        private void SetupElevatorBase()
        {
            GameObject embeddedPrefab = embeddedResourceBundle.LoadAsset<GameObject>("elevator_base");
            elevatorBasePrefab = Instantiate(embeddedPrefab, kitBashRoot.transform);
            elevatorBasePrefab.name = "piece_elevator";
            elevatorBasePrefab.AddComponent<Elevator>(); 
            elevatorBasePrefab.AddComponent<MoveableBaseElevatorSync>();
            elevatorBasePrefab.transform.Find("wheel_collider").gameObject.AddComponent<ElevatorControlls>();
            PrefabManager.Instance.AddPrefab(elevatorBasePrefab);
            PrefabManager.Instance.RegisterToZNetScene(elevatorBasePrefab);
            ElevatorSupport.elevatorPrefab = elevatorBasePrefab;
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

                Transform parentTransform = config.targetParentPath != null ? piecePrefab.transform.Find(config.targetParentPath) : piecePrefab.transform;
                if (parentTransform == null)
                {
                    Jotunn.Logger.LogWarning("");
                    continue;
                }
                GameObject kitBashObject = Instantiate(sourceGameObject, parentTransform);
                kitBashObject.name = config.name;
                kitBashObject.transform.localPosition = config.position;
                kitBashObject.transform.localRotation = config.rotation;
                kitBashObject.transform.localScale = config.scale;


                if (config.materialPath != null)
                {
      
                    Material[] sourceMaterials = GetSourceMaterials(config, sourcePrefab);
                    
                    if(sourceMaterials == null)
                    {
                        Jotunn.Logger.LogWarning("No materials for " + config);
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

                    SkinnedMeshRenderer[] targetSkinnedMeshRenderers = kitBashObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                    foreach(SkinnedMeshRenderer targetSkinnedMeshRenderer in targetSkinnedMeshRenderers)
                    {
                        targetSkinnedMeshRenderer.sharedMaterials = sourceMaterials;
                        targetSkinnedMeshRenderer.materials = sourceMaterials;
                    }

                    MeshRenderer[] targetMeshRenderers = kitBashObject.GetComponentsInChildren<MeshRenderer>();
                    foreach(MeshRenderer targetMeshRenderer in targetMeshRenderers)
                    {
                        targetMeshRenderer.sharedMaterials = sourceMaterials;
                        targetMeshRenderer.materials = sourceMaterials;
                    }

                }

            }
        }

        private Material[] GetSourceMaterials(KitBashConfig config, GameObject sourcePrefab)
        {
            GameObject materialPrefab = config.materialPrefab != null ? PrefabManager.Instance.GetPrefab(config.materialPrefab) : sourcePrefab;
            GameObject materialSourceObject = materialPrefab.transform.Find(config.materialPath).gameObject;
            MeshRenderer[] sourceMeshRenderers = materialSourceObject.GetComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] sourceSkinnedMeshRenderers = materialSourceObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (MeshRenderer meshRenderer in sourceMeshRenderers)
            {
                return meshRenderer.sharedMaterials;
            }
            foreach(SkinnedMeshRenderer skinnedMeshRenderer in sourceSkinnedMeshRenderers)
            {
                return skinnedMeshRenderer.sharedMaterials;
            }
            return null;
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