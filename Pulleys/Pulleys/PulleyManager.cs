using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PlanBuild.KitBash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pulleys
{
    class PulleyManager
    {
        public const string MoveableBaseSyncName = "moveable_base_sync";
        public const string MoveableBaseRootName = "moveable_base_root";
        public const string PulleyBaseName = "piece_pulley_base";
        public const string PulleySupportName = "piece_pulley_support";
        private static PulleyManager _instance;
        private static Piece m_lastRayPiece;

        public static PulleyManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PulleyManager();
                return _instance;
            }
        }

        public void Init()
        {
            ItemManager.OnVanillaItemsAvailable += RegisterCustomItems;
            On.Hud.UpdateShipHud += UpdateShipHud;
            On.ShipControlls.Interact += PulleyControlls.ShipControllsInteract;
            On.Piece.Awake += Piece_Awake;
            On.Player.SetShipControl += Player_SetShipControl;
            On.Player.PieceRayTest += Player_PieceRayTest;
            On.Player.CheckCanRemovePiece += Player_CheckCanRemovePiece;
            IL.Player.PlacePiece += Player_PlacePiece;
        }

        private bool Player_CheckCanRemovePiece(On.Player.orig_CheckCanRemovePiece orig, Player self, Piece piece)
        {
            if (piece.TryGetComponent(out Pulley pulley))
            {
                if (!pulley.CanBeRemoved())
                {
                    self.Message(MessageHud.MessageType.Center, "$msg_pulley_is_supporting");
                    return false;
                }
            }
            if (piece.TryGetComponent(out PulleySupport pulleySupport))
            {
                if (!pulleySupport.CanBeRemoved())
                {
                    self.Message(MessageHud.MessageType.Center, "$msg_pulley_is_supporting");
                    return false;
                }
            }
            return orig(self, piece);
        }

        private bool Player_PieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            int layerMask = self.m_placeRayMask;
            if (water)
            {
                layerMask = self.m_placeWaterRayMask;
            }
            if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, layerMask)
                && hitInfo.collider
                && (!hitInfo.collider.attachedRigidbody || hitInfo.collider.attachedRigidbody.GetComponent<MoveableBaseRoot>() != null)
                && Vector3.Distance(self.m_eye.position, hitInfo.point) < self.m_maxPlaceDistance)
            {
                point = hitInfo.point;
                normal = hitInfo.normal;
                piece = hitInfo.collider.GetComponentInParent<Piece>();
                heightmap = hitInfo.collider.GetComponent<Heightmap>();
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    waterSurface = hitInfo.collider;
                }
                else
                {
                    waterSurface = null;
                }
                m_lastRayPiece = piece; 
                return true;
            }
            point = Vector3.zero;
            normal = Vector3.zero;
            piece = null;
            heightmap = null;
            waterSurface = null;
            m_lastRayPiece = piece;
            return false;

        }

        private void Player_SetShipControl(On.Player.orig_SetShipControl orig, Player self, ref Vector3 moveDir)
        {
            MoveableBaseRoot moveableBaseRoot = self.GetControlledShip() as MoveableBaseRoot;
            if (moveableBaseRoot)
            {
                moveableBaseRoot.ApplyMovementControlls(moveDir);
                return;
            }
            orig(self, ref moveDir);

        }

        private void Piece_Awake(On.Piece.orig_Awake orig, Piece self)
        {
            orig(self);
            if (self.m_nview && self.m_nview.IsValid())
            {
                MoveableBaseSync.InitPiece(self);
            }
        }

        private void Player_PlacePiece(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int resultLoc = 0;
            c.GotoNext(MoveType.After,
                zz => zz.MatchCallOrCallvirt<Object>("Instantiate"),                //Find the Object.Instantiate function
                zz => zz.MatchStloc(out resultLoc)                                  //Get the location of the returned instantiated Piece
            );
            c.Emit(OpCodes.Ldloc, resultLoc);                                       //Load the instantiated object for ...
            c.Emit(OpCodes.Call, typeof(PulleyManager).GetMethod("PlacedPiece"));   //my hook :D
        }

        public static void PlacedPiece(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Piece piece))
            {
                if (m_lastRayPiece)
                {
                    MoveableBaseRoot moveableBaseRoot = m_lastRayPiece.GetComponentInParent<MoveableBaseRoot>();
                    if (moveableBaseRoot)
                    {
                        moveableBaseRoot.m_baseSync.AddNewPiece(piece);
                    }
                }
            }
        }

        private void UpdateShipHud(On.Hud.orig_UpdateShipHud orig, Hud self, Player player, float dt)
        {
            MoveableBaseRoot controlledMoveableBaseRoot = player.GetControlledShip() as MoveableBaseRoot;
            if (controlledMoveableBaseRoot)
            {
                Ship.Speed speedSetting = controlledMoveableBaseRoot.GetSpeedSetting();
                self.m_shipHudRoot.SetActive(value: true);
                self.m_rudderSlow.SetActive(speedSetting == Ship.Speed.Slow);
                self.m_rudderForward.SetActive(speedSetting == Ship.Speed.Half);
                self.m_rudderFastForward.SetActive(speedSetting == Ship.Speed.Full);
                self.m_rudderBackward.SetActive(speedSetting == Ship.Speed.Back);
                self.m_rudderLeft.SetActive(value: false);
                self.m_rudderRight.SetActive(value: false);
                self.m_fullSail.SetActive(false);
                self.m_halfSail.SetActive(false);
                self.m_shipWindIconRoot.gameObject.SetActive(false);
                self.m_shipWindIndicatorRoot.gameObject.SetActive(false);
                GameObject rudder2 = self.m_rudder;
                rudder2.SetActive(false);
                self.m_shipRudderIndicator.gameObject.SetActive(value: false);

                Camera mainCamera = Utils.GetMainCamera();
                if (!(mainCamera == null))
                {
                    self.m_shipControlsRoot.transform.position = mainCamera.WorldToScreenPoint(controlledMoveableBaseRoot.m_controlGuiPos.position);
                }
                return;
            }
            orig(self, player, dt);
        }

        public void RegisterCustomItems()
        {
            AssetBundle embeddedResourceBundle = null;
            try
            {
                embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("pulleys", typeof(PulleyPlugin).Assembly);

                SetupMoveableBaseRoot(embeddedResourceBundle);
                SetupPulleySupport(embeddedResourceBundle);
                SetupPulleyBase(embeddedResourceBundle);
            }
            finally
            {
                embeddedResourceBundle?.Unload(false);
                ItemManager.OnVanillaItemsAvailable -= RegisterCustomItems;
            }
        }

        private void SetupMoveableBaseRoot(AssetBundle embeddedResourceBundle)
        {
            GameObject baseSyncPrefab = embeddedResourceBundle.LoadAsset<GameObject>(MoveableBaseSyncName);
            PrefabManager.Instance.AddPrefab(baseSyncPrefab);
            baseSyncPrefab.AddComponent<MoveableBaseSync>();

            GameObject baseRootPrefab = embeddedResourceBundle.LoadAsset<GameObject>(MoveableBaseRootName);
            PrefabManager.Instance.AddPrefab(baseRootPrefab);
            baseRootPrefab.AddComponent<MoveableBaseRoot>();
        }

        private void SetupPulleySupport(AssetBundle embeddedResourceBundle)
        {
            GameObject embeddedPrefab = embeddedResourceBundle.LoadAsset<GameObject>(PulleySupportName);
            KitBashObject pulleySupportKitBash = KitBashManager.Instance.KitBash(embeddedPrefab, new KitBashConfig
            {
                KitBashSources = new List<KitBashSourceConfig>
            {
                new KitBashSourceConfig
                {
                    name = "New",
                    sourcePrefab = "wood_wall_roof_top_45",
                    sourcePath = "New",
                    position = new Vector3(0f, 0f, -1f),
                    rotation = Quaternion.Euler(90, 0, 0)
                },
                new KitBashSourceConfig
                {
                    name = "pivot_gear",
                    targetParentPath = "New/pivot",
                    sourcePrefab = "piece_artisanstation",
                    sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.007_ArtisanTable.019",
                    materialPath = "New/high/ArtisanTable.004",
                    position = new Vector3(-1.904f, 1.365f, -0.2620001f),
                    rotation = Quaternion.Euler(180f, 180f, 98.78699f),
                    scale = new Vector3(1.4f, 1.4f, 1f),
                    materialRemap = new int[]{ 1, 0 }
                },
                new KitBashSourceConfig
                {
                    name = "support_left",
                    targetParentPath = "New/pivot",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(-0.2338867f, -0.913f, 0.72f),
                    rotation = Quaternion.Euler(283.043f, -168.623f, 205.604f),
                    scale = Vector3.one  * -1
                },
                new KitBashSourceConfig
                {
                    name = "support_right",
                    targetParentPath = "New/pivot",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.223f, -0.913f, 0.72f),
                    rotation = Quaternion.Euler(-283.095f, -11.332f, 25.65f)
                },
                new KitBashSourceConfig
                {
                    name = "wheel_left",
                    targetParentPath = "New/pivot/pivot_left",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(0.06511331f, 0.8729141f, -1.120428f),
                    rotation = Quaternion.Euler(269.96f, 180, 0)
                },
                new KitBashSourceConfig
                {
                    name = "wheel_right",
                    targetParentPath = "New/pivot/pivot_right",
                    sourcePrefab = "piece_spinningwheel",
                    sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                    materialPath = "New/High/SpinningWheel",
                    position = new Vector3(-0.07488656f, -0.8700893f, -1.121964f),
                    rotation = Quaternion.Euler(-270.04f, 0, 0)
                },

            }
            });
            pulleySupportKitBash.KitBashApplied += () =>
            {
                GameObject raft = PrefabManager.Instance.GetPrefab("Raft");
                LineRenderer sourceLineRenderer = raft.transform.Find("ship/visual/ropes/left").GetComponent<LineRenderer>();

                foreach (LineRenderer lineRenderer in pulleySupportKitBash.Prefab.GetComponentsInChildren<LineRenderer>())
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
            };
            PieceManager.Instance.AddPiece(new CustomPiece(pulleySupportKitBash.Prefab, new PieceConfig()
            {
                PieceTable = "Hammer"
            }));
            pulleySupportKitBash.Prefab.AddComponent<PulleySupport>();
        }

        private void SetupPulleyBase(AssetBundle embeddedResourceBundle)
        {
            GameObject embeddedPrefab = embeddedResourceBundle.LoadAsset<GameObject>(PulleyBaseName);
            KitBashObject pulleyBaseKitBash = KitBashManager.Instance.KitBash(embeddedPrefab, new KitBashConfig
            {
                KitBashSources = new List<KitBashSourceConfig> { new KitBashSourceConfig
                    {
                        name = "_Combined Mesh [high]",
                        targetParentPath = "New",
                        sourcePrefab = "wood_floor",
                        sourcePath = "New/_Combined Mesh [high]",
                        position = new Vector3(0f, -52.55f, 1f),
                        rotation = Quaternion.Euler(0, 0, 0)
                    },
                    new KitBashSourceConfig
                    {
                        name = "wheel_left",
                        targetParentPath = "New/pivot_left",
                        sourcePrefab = "piece_spinningwheel",
                        sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                        materialPath = "New/High/SpinningWheel",
                        position = new Vector3(0.06511331f, -1.13f, -0.86f),
                        rotation = Quaternion.Euler(0, 180, 0)
                    },
                    new KitBashSourceConfig
                    {
                        name = "wheel_right",
                        targetParentPath = "New/pivot_right",
                        sourcePrefab = "piece_spinningwheel",
                        sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.002_SpinningWheel_Broken.018",
                        materialPath = "New/High/SpinningWheel",
                        position = new Vector3(-0.07488668f, -1.12f, 0.86f),
                        rotation = Quaternion.Euler(0, 0, 0)
                    },
                    new KitBashSourceConfig
                    {
                        name = "support_left",
                        targetParentPath = "New",
                        sourcePrefab = "piece_spinningwheel",
                        sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                        materialPath = "New/High/SpinningWheel",
                        position = new Vector3(-0.25f, 0.5580001f, 0.9489999f),
                        rotation = Quaternion.Euler(11.676f, -177.394f, 217.222f),
                        scale = Vector3.one * -1
                    },
                    new KitBashSourceConfig
                    {
                        name = "support_left_bar",
                        targetParentPath = "New",
                        sourcePrefab = "piece_spinningwheel",
                        sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                        materialPath = "New/High/SpinningWheel",
                        position = new Vector3(0.403f, 0.211f, 0.274f),
                        rotation = Quaternion.Euler(-260.316f, -195.346f, 201.557f),
                        scale = Vector3.one * 0.6f
                    },
                    new KitBashSourceConfig
                    {
                        name = "support_right",
                        targetParentPath = "New",
                        sourcePrefab = "piece_spinningwheel",
                        sourcePath = "SpinningWheel_Destruction/SpinningWheel_Destruction.011_SpinningWheel_Broken.027",
                        materialPath = "New/High/SpinningWheel",
                        position = new Vector3(0.25f, 0.5699999f, 0.9389999f),
                        rotation = Quaternion.Euler(-11.728f, -2.606f, 37.225f)
                    },
                    new KitBashSourceConfig
                    {
                        name = "seat",
                        targetParentPath = "New",
                        sourcePrefab = "piece_chair",
                        sourcePath = "New",
                        position = new Vector3(0.7f, 0f, 0.656f),
                        scale = new Vector3(0.74871f, 0.61419f, 0.63284f)
                    },
                    new KitBashSourceConfig
                    {
                        name = "crank_gear",
                        targetParentPath = "New/crank",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.007_ArtisanTable.019",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.4602f, -1.088331f, 0.7863638f),
                        rotation = Quaternion.Euler(0f, 90f, 8.787001f),
                        scale = new Vector3(0.8f, 0.8f, 1f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "central_gear",
                        targetParentPath = "New/pivot_right",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.28f, -0.894f, 0.585f),
                        rotation = Quaternion.Euler(0f, 90f, 0f),
                        scale = new Vector3(0.8f, 0.8f, 1f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "sun_gear",
                        targetParentPath = "New",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.847f, -0.111f, 0.621f),
                        rotation = Quaternion.Euler(0f, 90f, 0f),
                        scale = new Vector3(0.8f, 0.8f, 1.7f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "planet_gear_1",
                        targetParentPath = "New/pivot_right/planet_1",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.847f, -0.7618001f, 0.752f),
                        rotation = Quaternion.Euler(0f, 90f, -11.669f),
                        scale = new Vector3(0.8f, 0.8f, 1.7f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "planet_gear_2",
                        targetParentPath = "New/pivot_right/planet_2",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.847f, -0.7618001f, 0.752f),
                        rotation = Quaternion.Euler(0f, 90f, -11.669f),
                        scale = new Vector3(0.8f, 0.8f, 1.7f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "planet_gear_3",
                        targetParentPath = "New/pivot_right/planet_3",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.847f, -0.7618001f, 0.752f),
                        rotation = Quaternion.Euler(0f, 90f, -11.669f),
                        scale = new Vector3(0.8f, 0.8f, 1.7f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "planet_gear_4",
                        targetParentPath = "New/pivot_right/planet_4",
                        sourcePrefab = "piece_artisanstation",
                        sourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.006_ArtisanTable.018",
                        materialPath = "New/high/ArtisanTable.004",
                        position = new Vector3(-0.847f, -0.7618001f, 0.752f),
                        rotation = Quaternion.Euler(0f, 90f, -11.669f),
                        scale = new Vector3(0.8f, 0.8f, 1.7f),
                        materialRemap = new int[] { 1, 0 }
                    },
                    new KitBashSourceConfig
                    {
                        name = "handhold",
                        targetParentPath = "New/crank",
                        sourcePrefab = "piece_stonecutter",
                        sourcePath = "Stonecutterbench_destruction/Stonecutter_destruction.001_Stonecutter_destruction.001_Workbench.001",
                        materialPrefab = "piece_spinningwheel",
                        materialPath = "New/High/SpinningWheel",
                        position = new Vector3(0.04099999f, -0.1544f, -0.1712f),
                        rotation = Quaternion.Euler(0f, 5.765f, -89.982f),
                        scale = Vector3.one * 0.1f
                    }
                }
            });
            PieceManager.Instance.AddPiece(new CustomPiece(pulleyBaseKitBash.Prefab, new PieceConfig()
            {
                PieceTable = "Hammer"
            }));
            pulleyBaseKitBash.Prefab.AddComponent<Pulley>();
        }

    }
}
