using Jotunn.Configs;
using Jotunn.Managers;
using PlanBuild.ModCompat;
using PlanBuild.PlanBuild;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class BlueprintManager
    {
        private static BlueprintManager _instance;
        public static BlueprintManager Instance
        {
            get
            {
                if (_instance == null) _instance = new BlueprintManager();
                return _instance;
            }
        }

        internal static ButtonConfig PlanSwitchButton;
        internal static ButtonConfig GUIToggleButton;

        internal static BlueprintDictionary LocalBlueprints;
        internal static BlueprintDictionary ServerBlueprints;

        internal bool ShowSelectionCircle = true;
        private GameObject SelectionSegment;
        private CircleProjector SelectionCircle;
        private float SelectionRadius = 10.0f;

        private Vector3 PlacementOffset = Vector3.zero;
        private float OriginalPlaceDistance;

        private float CameraOffset = 5.0f;

        internal const float HighlightTimeout = 0.5f;
        private float LastHightlightTime = 0f;

        private Piece LastHoveredPiece;

        internal void Init()
        {
            Jotunn.Logger.LogInfo("Initializing BlueprintManager");

            try
            {
                // Init lists
                LocalBlueprints = new BlueprintDictionary(BlueprintLocation.Local);
                ServerBlueprints = new BlueprintDictionary(BlueprintLocation.Server);

                // Init config
                BlueprintConfig.Init();

                // Init sync
                BlueprintSync.Init();

                // Init Commands
                BlueprintCommands.Init();

                // Init GUI
                BlueprintGUI.Init();

                // Create KeyHints if and when PixelFix is created
                GUIManager.OnPixelFixCreated += CreateCustomKeyHints;

                // Create blueprint prefabs when all pieces were registered
                // Some may still fail, these will be retried every time the blueprint rune is opened
                PieceManager.OnPiecesRegistered += RegisterKnownBlueprints;

                // Hooks
                On.Player.OnSpawned += OnOnSpawned;
                On.Player.PieceRayTest += OnPieceRayTest;
                On.Player.UpdateWearNTearHover += OnUpdateWearNTearHover;
                On.Player.SetupPlacementGhost += OnSetupPlacementGhost;
                On.Player.UpdatePlacement += OnUpdatePlacement;
                On.Player.UpdatePlacementGhost += OnUpdatePlacementGhost;
                On.Player.PlacePiece += OnPlacePiece;
                On.GameCamera.UpdateCamera += OnUpdateCamera;
                On.Humanoid.EquipItem += OnEquipItem;
                On.Humanoid.UnequipItem += OnUnequipItem;
                On.ZNet.OnDestroy += ResetServerBlueprints; 
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"{ex.StackTrace}");
            }
        }

        /// <summary>
        ///     Determine if a piece can be captured in a blueprint
        /// </summary>
        /// <param name="piece">Piece instance to be tested</param>
        /// <param name="onlyPlanned">When true, only pieces with the PlanPiece component return true</param>
        /// <returns></returns>
        public bool CanCapture(Piece piece, bool onlyPlanned = false)
        {
            if (piece.name.StartsWith(BlueprintRunePrefab.BlueprintSnapPointName) || piece.name.StartsWith(BlueprintRunePrefab.BlueprintCenterPointName))
            {
                return true;
            }
            return piece.GetComponent<PlanPiece>() != null || (!onlyPlanned && PlanManager.CanCreatePlan(piece));
        }

        /// <summary>
        ///     Get all pieces on a given position in a given radius, optionally only planned ones
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="onlyPlanned"></param>
        /// <returns></returns>
        public List<Piece> GetPiecesInRadius(Vector3 position, float radius, bool onlyPlanned = false)
        {
            
            List<Piece> result = new List<Piece>();
            foreach (var piece in Piece.m_allPieces)
            {
                Vector3 piecePos = piece.transform.position;
                if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(piecePos.x, piecePos.z)) <= radius
                    && CanCapture(piece, onlyPlanned))
                {
                    result.Add(piece);
                }
            }
            return result;
        }

        /// <summary>
        ///     "Highlights" pieces in a given radius with a given color.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="radius"></param>
        /// <param name="color"></param>
        /// <param name="onlyPlanned"></param>
        public void HighlightPiecesInRadius(Vector3 startPosition, float radius, Color color, bool onlyPlanned = false)
        {
            if (Time.time < LastHightlightTime + HighlightTimeout)
            {
                return;
            }
            foreach (var piece in GetPiecesInRadius(startPosition, radius, onlyPlanned))
            {
                if (piece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.Highlight(color);
                }
            }
            LastHightlightTime = Time.time;
        }

        /// <summary>
        ///     "Highlights" the last hovered planned piece with a given color.
        /// </summary>
        /// <param name="color"></param>
        public void HighlightHoveredPiece(Color color)
        {
            if (Time.time > LastHightlightTime + HighlightTimeout)
            {
                if (LastHoveredPiece != null && LastHoveredPiece.TryGetComponent(out PlanPiece hoveredPlanPiece))
                {
                    hoveredPlanPiece.m_wearNTear.Highlight(color);
                }
                LastHightlightTime = Time.time;
            }
        }

        /// <summary>
        ///     "Highlights" all pieces belonging to the last hovered Blueprint with a given color.
        /// </summary>
        /// <param name="color"></param>
        public void HighlightHoveredBlueprint(Color color)
        {
            if (Time.time > LastHightlightTime + HighlightTimeout)
            {
                if (LastHoveredPiece != null && LastHoveredPiece.TryGetComponent(out PlanPiece hoveredPlanPiece))
                {
                    ZDOID blueprintID = hoveredPlanPiece.GetBlueprintID();
                    if (blueprintID != ZDOID.None)
                    {
                        foreach (PlanPiece planPiece in GetPlanPiecesInBlueprint(blueprintID))
                        {
                            planPiece.m_wearNTear.Highlight(color);
                        }
                    }
                }
                LastHightlightTime = Time.time;
            }
        }

        /// <summary>
        ///     Get all pieces belonging to a given Blueprint identified by its <see cref="ZDOID"/>
        /// </summary>
        /// <param name="blueprintID"></param>
        /// <returns></returns>
        public List<PlanPiece> GetPlanPiecesInBlueprint(ZDOID blueprintID)
        {
            List<PlanPiece> result = new List<PlanPiece>();
            ZDO blueprintZDO = ZDOMan.instance.GetZDO(blueprintID);
            if (blueprintZDO == null)
            {
                return result;
            }
            ZDOIDSet planPieces = GetPlanPieces(blueprintZDO);
            foreach (ZDOID pieceZDOID in planPieces)
            {
                GameObject pieceObject = ZNetScene.instance.FindInstance(pieceZDOID);
                if (pieceObject && pieceObject.TryGetComponent(out PlanPiece planPiece))
                {
                    result.Add(planPiece);
                }
            }
            return result;
        }

        /// <summary>
        ///     Get a specific <see cref="Piece"/> from a Blueprint identified by its <see cref="ZDO"/>
        /// </summary>
        /// <param name="blueprintZDO"></param>
        /// <returns></returns>
        public ZDOIDSet GetPlanPieces(ZDO blueprintZDO)
        {
            byte[] data = blueprintZDO.GetByteArray(PlanPiece.zdoBlueprintPiece);
            if (data == null)
            {
                return null;
            }
            return ZDOIDSet.From(new ZPackage(data));
        }

        /// <summary>
        ///     Remove a <see cref="Piece"/> instances ZDO from its Blueprint <see cref="ZDOIDSet"/>
        /// </summary>
        /// <param name="planPiece"></param>
        public void PlanPieceRemovedFromBlueprint(PlanPiece planPiece)
        {
            ZDOID blueprintID = planPiece.GetBlueprintID();
            if (blueprintID == ZDOID.None)
            {
                return;
            }

            ZDO blueprintZDO = ZDOMan.instance.GetZDO(blueprintID);
            if (blueprintZDO == null)
            {
                return;
            }
            ZDOIDSet planPieces = GetPlanPieces(blueprintZDO);
            planPieces?.Remove(planPiece.GetPlanPieceID());
            if (planPieces == null || planPieces.Count() == 0)
            {
                GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
                if (blueprintObject)
                {
                    ZNetScene.instance.Destroy(blueprintObject);
                }
            }
            else
            {
                blueprintZDO.Set(PlanPiece.zdoBlueprintPiece, planPieces.ToZPackage().GetArray());
            }
        }

        /// <summary>
        ///     Create custom KeyHints for the static Blueprint Rune pieces
        /// </summary>
        private void CreateCustomKeyHints()
        {
            PlanSwitchButton = new ButtonConfig
            {
                Name = "RuneModeToggle",
                Config = BlueprintConfig.planSwitchConfig,
                HintToken = "$hud_bp_toggle_plan_mode"
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, PlanSwitchButton);

            GUIToggleButton = new ButtonConfig
            {
                Name = "GUIToggle",
                Config = BlueprintConfig.serverGuiSwitchConfig,
                ActiveInGUI = true
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, GUIToggleButton);

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_blueprint_mode" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" }
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.MakeBlueprintName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcapture" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_bpcapture_highlight" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.BlueprintSnapPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpsnappoint" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" },
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.BlueprintCenterPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcenterpoint" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" },
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.DeletePlansName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpdelete" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_bpdelete_radius" },
                    new ButtonConfig { Name = "Alt", HintToken = "$hud_bpdelete_all" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            GUIManager.OnPixelFixCreated -= CreateCustomKeyHints;
        }

        /// <summary>
        ///     Create prefabs for all known local Blueprints
        /// </summary>
        public void RegisterKnownBlueprints()
        {
            // Client only
            if (ZNet.instance != null && !ZNet.instance.IsDedicated())
            {
                Jotunn.Logger.LogInfo("Registering known blueprints");

                // Create prefabs for all known blueprints
                foreach (var bp in LocalBlueprints.Values)
                {
                    bp.CreatePiece();
                }
            }
        }

        private void OnOnSpawned(On.Player.orig_OnSpawned orig, Player self)
        {
            orig(self);
            GameObject workbench = PrefabManager.Instance.GetPrefab("piece_workbench");
            SelectionSegment = Object.Instantiate(workbench.GetComponentInChildren<CircleProjector>().m_prefab);
            SelectionSegment.SetActive(false);
        }

        private bool OnPieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            LastHoveredPiece = piece;
            if (result && PlacementOffset != Vector3.zero && self.m_placementGhost)
            {
                point += self.m_placementGhost.transform.TransformDirection(PlacementOffset);
            }
            return result;
        }

        /// <summary>
        ///     Dont highlight pieces when make/delete tool is active
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void OnUpdateWearNTearHover(On.Player.orig_UpdateWearNTearHover orig, Player self)
        {
            Piece piece = self.GetSelectedPiece();
            if (piece &&
                (piece.name.StartsWith(BlueprintRunePrefab.MakeBlueprintName)
              || piece.name.StartsWith(BlueprintRunePrefab.DeletePlansName)))
            {
                return;
            }

            orig(self);
        }

        /// <summary>
        ///     Lazy instantiate blueprint ghost
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void OnSetupPlacementGhost(On.Player.orig_SetupPlacementGhost orig, Player self)
        {
            if (self.m_buildPieces == null)
            {
                orig(self);
                return;
            }

            GameObject prefab = self.m_buildPieces.GetSelectedPrefab();
            if (!prefab || !prefab.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                orig(self);
                return;
            }

            string bpname = prefab.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            if (LocalBlueprints.TryGetValue(bpname, out var bp))
            {
                bp.InstantiateGhost();
            }
            
            orig(self);
        }

        /// <summary>
        ///     Update the blueprint tools
        /// </summary>
        private void OnUpdatePlacement(On.Player.orig_UpdatePlacement orig, Player self, bool takeInput, float dt)
        {
            orig(self, takeInput, dt);

            if (self.m_placementGhost && takeInput)
            {
                var piece = self.m_placementGhost.GetComponent<Piece>();
                if (piece != null)
                {
                    // Capture Blueprint
                    if (piece.name.StartsWith(BlueprintRunePrefab.MakeBlueprintName) && !piece.IsCreator())
                    {
                        if (!self.m_placementMarkerInstance)
                        {
                            return;
                        }

                        EnableSelectionCircle(self);

                        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                        if (scrollWheel != 0f)
                        {
                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                UpdateCameraOffset(scrollWheel);
                            }
                            else
                            {
                                UpdateSelectionRadius(scrollWheel);
                            }
                        }
                        
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, Instance.SelectionRadius, Color.green);
                        }
                    }
                    // Place Blueprint
                    else if (piece.name.StartsWith(Blueprint.PieceBlueprintName))
                    {
                        DisableSelectionCircle();

                        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                        if (scrollWheel != 0f)
                        {
                            if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt)) || 
                                (Input.GetKey(KeyCode.RightControl) && Input.GetKey(KeyCode.RightAlt)))
                            {
                                PlacementOffset.y += GetPlacementOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                PlacementOffset.x += GetPlacementOffset(scrollWheel);
                                UndoRotation(self, scrollWheel); 
                            }
                            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                PlacementOffset.z += GetPlacementOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                UpdateCameraOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                        }
                    }
                    // Delete Plans
                    else if (piece.name.StartsWith(BlueprintRunePrefab.DeletePlansName))
                    {
                        if (!self.m_placementMarkerInstance)
                        {
                            return;
                        }
                        
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            EnableSelectionCircle(self);
                        }
                        else
                        {
                            DisableSelectionCircle();
                        }

                        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                        if (scrollWheel != 0)
                        {
                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                UpdateCameraOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                UpdateSelectionRadius(scrollWheel);
                            }
                        }

                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            HighlightPiecesInRadius(self.m_placementMarkerInstance.transform.position, Instance.SelectionRadius, Color.red, onlyPlanned: true);
                        }
                        else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                        {
                            HighlightHoveredBlueprint(Color.red);
                        }
                        else
                        {
                            HighlightHoveredPiece(Color.red);
                        }
                    }
                    else
                    {
                        DisableSelectionCircle();

                        Instance.CameraOffset = 5f;
                        Instance.PlacementOffset = Vector3.zero;
                    }
                }
            }

            // Always update the selection circle
            UpdateSelectionCircle();
        }

        private float GetPlacementOffset(float scrollWheel)
        {
            bool scrollingDown = scrollWheel < 0f;
            if (BlueprintConfig.invertPlacementOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                return -BlueprintConfig.placementOffsetIncrementConfig.Value;
            }
            else
            {
                return BlueprintConfig.placementOffsetIncrementConfig.Value;
            }
        }

        private void UndoRotation(Player player, float scrollWheel)
        {
            if (scrollWheel < 0f)
            {
                player.m_placeRotation++;
            }
            else
            {
                player.m_placeRotation--;
            }
        }

        private void UpdateSelectionRadius(float scrollWheel)
        {
            if (SelectionCircle == null)
            {
                return;
            }

            bool scrollingDown = scrollWheel < 0f;
            if (BlueprintConfig.invertSelectionScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                Instance.SelectionRadius -= BlueprintConfig.selectionIncrementConfig.Value;
                if (Instance.SelectionRadius < 2f)
                {
                    Instance.SelectionRadius = 2f;
                }
            }
            else
            {
                Instance.SelectionRadius += BlueprintConfig.selectionIncrementConfig.Value;
            }
        }

        private void EnableSelectionCircle(Player self)
        {
            if (SelectionCircle == null && ShowSelectionCircle)
            {
                SelectionCircle = self.m_placementMarkerInstance.AddComponent<CircleProjector>();
                SelectionCircle.m_prefab = SelectionSegment;
                SelectionCircle.m_prefab.SetActive(true);
                SelectionCircle.m_radius = Instance.SelectionRadius;
                SelectionCircle.m_nrOfSegments = (int)SelectionCircle.m_radius * 4;
                SelectionCircle.Start();
            }
        }

        private void DisableSelectionCircle()
        {
            if (SelectionCircle != null)
            {
                foreach (GameObject segment in SelectionCircle.m_segments)
                {
                    Object.Destroy(segment);
                }
                Object.Destroy(SelectionCircle);
            }
        }

        private void UpdateSelectionCircle()
        {
            if (SelectionCircle != null && !ShowSelectionCircle)
            {
                DisableSelectionCircle();
            }
            if (SelectionCircle == null)
            {
                return;
            }
            if (SelectionCircle.m_radius != Instance.SelectionRadius)
            {
                SelectionCircle.m_radius = Instance.SelectionRadius;
                SelectionCircle.m_nrOfSegments = (int)SelectionCircle.m_radius * 4;
                SelectionCircle.Update();

                Jotunn.Logger.LogDebug($"Setting radius to {Instance.SelectionRadius}");
            }
        }

        /// <summary>
        ///     Flatten the circle selector transform
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="flashGuardStone"></param>
        private void OnUpdatePlacementGhost(On.Player.orig_UpdatePlacementGhost orig, Player self, bool flashGuardStone)
        {
            orig(self, flashGuardStone);

            if (self.m_placementMarkerInstance && self.m_placementGhost &&
                (self.m_placementGhost.name == BlueprintRunePrefab.MakeBlueprintName
                || self.m_placementGhost.name == BlueprintRunePrefab.DeletePlansName)
               )
            {
                self.m_placementMarkerInstance.transform.up = Vector3.back;
            }
        }

        private void UpdateCameraOffset(float scrollWheel)
        {
            // TODO: base min/max off of selected piece dimensions
            float minOffset = 2f;
            float maxOffset = 20f;
            bool scrollingDown = scrollWheel < 0f;
            if (BlueprintConfig.invertCameraOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                Instance.CameraOffset = Mathf.Clamp(Instance.CameraOffset += BlueprintConfig.cameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
            else
            {
                Instance.CameraOffset = Mathf.Clamp(Instance.CameraOffset -= BlueprintConfig.cameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
        }

        /// <summary>
        ///     Adjust camera height when using certain tools
        /// </summary>
        private void OnUpdateCamera(On.GameCamera.orig_UpdateCamera orig, GameCamera self, float dt)
        {
            orig(self, dt);

            if (PatcherBuildCamera.UpdateCamera
                && Player.m_localPlayer
                && Player.m_localPlayer.InPlaceMode()
                && Player.m_localPlayer.m_placementGhost)
            {
                var pieceName = Player.m_localPlayer.m_placementGhost.name;
                if (pieceName.StartsWith(BlueprintRunePrefab.MakeBlueprintName)
                    || pieceName.StartsWith(Blueprint.PieceBlueprintName)
                    || pieceName.StartsWith(BlueprintRunePrefab.DeletePlansName))
                {
                    self.transform.position += new Vector3(0, Instance.CameraOffset, 0);
                }
            }
        }

        /// <summary>
        ///     Incept placing of the meta pieces.
        ///     Cancels the real placement of the placeholder pieces.
        /// </summary>
        private bool OnPlacePiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
        {
            // Client only
            if (!ZNet.instance.IsDedicated())
            {
                // Capture a new blueprint
                if (piece.name == "make_blueprint")
                {
                    return MakeBlueprint(self);
                }
                // Place a known blueprint
                if (self.m_placementStatus == Player.PlacementStatus.Valid
                    && piece.name != BlueprintRunePrefab.BlueprintSnapPointName
                    && piece.name != BlueprintRunePrefab.BlueprintCenterPointName
                    && piece.name.StartsWith(Blueprint.PieceBlueprintName))
                {
                    return PlaceBlueprint(self, piece);
                }
                // Delete plans
                else if (piece.name.StartsWith(BlueprintRunePrefab.DeletePlansName))
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        return DeletePlans(self);
                    }
                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        return UndoBlueprint();
                    }
                    else
                    {
                        return UndoPiece();
                    }
                }
            }

            return orig(self, piece);
        }

        private bool MakeBlueprint(Player self)
        {
            var bpname = $"blueprint{LocalBlueprints.Count() + 1:000}";
            Jotunn.Logger.LogInfo($"Capturing blueprint {bpname}");

            var bp = new Blueprint();
            Vector3 capturePosition = self.m_placementMarkerInstance.transform.position;
            if (bp.Capture(capturePosition, Instance.SelectionRadius))
            {
                TextInput.instance.m_queuedSign = new Blueprint.BlueprintSaveGUI(bp);
                TextInput.instance.Show($"Save Blueprint ({bp.GetPieceCount()} pieces captured)", bpname, 50);
            }
            else
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bpname}");
            }

            // Don't place the piece and clutter the world with it
            return false;
        }

        private bool PlaceBlueprint(Player player, Piece piece)
        {
            string id = piece.gameObject.name.Substring(Blueprint.PieceBlueprintName.Length + 1);
            Blueprint bp = LocalBlueprints[id];
            var transform = player.m_placementGhost.transform;
            var position = transform.position;
            var rotation = transform.rotation;

            bool placeDirect = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (placeDirect && !BlueprintConfig.allowDirectBuildConfig.Value)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_direct_build_disabled");
                return false;
            }

            bool flatten = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (flatten)
            {
                Bounds bounds = bp.GetBounds();
                FlattenTerrain.FlattenForBlueprint(transform, bounds, bp.PieceEntries);
            }

            uint cntEffects = 0u;
            uint maxEffects = 10u;

            GameObject blueprintPrefab = PrefabManager.Instance.GetPrefab(Blueprint.PieceBlueprintName);
            GameObject blueprintObject = Object.Instantiate(blueprintPrefab, position, rotation);
            ZDO blueprintZDO = blueprintObject.GetComponent<ZNetView>().GetZDO();
            blueprintZDO.Set(Blueprint.ZDOBlueprintName, bp.Name);
            ZDOIDSet createdPlans = new ZDOIDSet();

            for (int i = 0; i < bp.PieceEntries.Length; i++)
            {
                PieceEntry entry = bp.PieceEntries[i];
                // Final position
                Vector3 entryPosition = transform.TransformPoint(entry.GetPosition());

                // Final rotation
                Quaternion entryQuat = transform.rotation * entry.GetRotation();
                // Get the prefab of the piece or the plan piece
                string prefabName = entry.name;
                if (!placeDirect)
                {
                    prefabName += PlanPiecePrefab.PlannedSuffix;
                }

                GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                if (!prefab)
                {
                    Jotunn.Logger.LogWarning(entry.name + " not found, you are probably missing a dependency for blueprint " + bp.Name + ", not placing @ " + entryPosition);
                    continue;
                }

                // Instantiate a new object with the new prefab
                GameObject gameObject = Object.Instantiate(prefab, entryPosition, entryQuat);

                ZNetView zNetView = gameObject.GetComponent<ZNetView>();
                if (!zNetView)
                {
                    Jotunn.Logger.LogWarning("No ZNetView for " + gameObject + "!!??");
                }
                else if (gameObject.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.PartOfBlueprint(blueprintZDO.m_uid, entry);
                    createdPlans.Add(planPiece.GetPlanPieceID());
                }

                // Register special effects
                CraftingStation craftingStation = gameObject.GetComponentInChildren<CraftingStation>();
                if (craftingStation)
                {
                    player.AddKnownStation(craftingStation);
                }
                Piece newpiece = gameObject.GetComponent<Piece>();
                if (newpiece)
                {
                    newpiece.SetCreator(player.GetPlayerID());
                }
                PrivateArea privateArea = gameObject.GetComponent<PrivateArea>();
                if (privateArea)
                {
                    privateArea.Setup(Game.instance.GetPlayerProfile().GetName());
                }
                WearNTear wearntear = gameObject.GetComponent<WearNTear>();
                if (wearntear)
                {
                    wearntear.OnPlaced();
                }
                TextReceiver textReceiver = gameObject.GetComponent<TextReceiver>();
                if (textReceiver != null)
                {
                    textReceiver.SetText(entry.additionalInfo);
                }

                // Limited build effects
                if (cntEffects < maxEffects)
                {
                    newpiece.m_placeEffect.Create(gameObject.transform.position, rotation, gameObject.transform, 1f);
                    player.AddNoise(50f);
                    cntEffects++;
                }

                // Count up player builds
                Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
            }

            blueprintZDO.Set(PlanPiece.zdoBlueprintPiece, createdPlans.ToZPackage().GetArray());

            // Dont set the blueprint piece and clutter the world with it
            return false;
        }

        private bool UndoPiece()
        {
            if (LastHoveredPiece)
            {
                if (LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                }
            }

            return false;
        }

        private bool UndoBlueprint()
        {
            if (LastHoveredPiece)
            {
                if (LastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    ZDOID blueprintID = planPiece.GetBlueprintID();
                    if (blueprintID != ZDOID.None)
                    {
                        int removedPieces = 0;
                        foreach (PlanPiece pieceToRemove in GetPlanPiecesInBlueprint(blueprintID))
                        {
                            pieceToRemove.Remove();
                            removedPieces++;
                        }

                        GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
                        if (blueprintObject)
                        {
                            ZNetScene.instance.Destroy(blueprintObject);
                        }

                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));
                    }
                }
            }

            return false;
        }

        private bool DeletePlans(Player self)
        {
            Vector3 deletePosition = self.m_placementMarkerInstance.transform.position;
            int removedPieces = 0;
            foreach (Piece pieceToRemove in GetPiecesInRadius(deletePosition, SelectionRadius))
            {
                if (pieceToRemove.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                    removedPieces++;
                }
            }
            self.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));

            return false;
        }

        private void OnUnequipItem(On.Humanoid.orig_UnequipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer &&
                item != null && item.m_shared.m_name == BlueprintRunePrefab.BlueprintRuneItemName)
            {
                Player.m_localPlayer.m_maxPlaceDistance = OriginalPlaceDistance;
                Jotunn.Logger.LogDebug("Setting placeDistance to " + Player.m_localPlayer.m_maxPlaceDistance);
            }
        }

        private bool OnEquipItem(On.Humanoid.orig_EquipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            bool result = orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer && result &&
                item != null && item.m_shared.m_name == BlueprintRunePrefab.BlueprintRuneItemName)
            {
                RegisterKnownBlueprints();
                OriginalPlaceDistance = Math.Max(Player.m_localPlayer.m_maxPlaceDistance, 8f);
                Player.m_localPlayer.m_maxPlaceDistance = BlueprintConfig.rayDistanceConfig.Value;
                Jotunn.Logger.LogDebug("Setting placeDistance to " + Player.m_localPlayer.m_maxPlaceDistance);
            }
            return result;
        }

        private void ResetServerBlueprints(On.ZNet.orig_OnDestroy orig, ZNet self)
        {
            ServerBlueprints?.Clear();
            orig(self);
        }
    }
}