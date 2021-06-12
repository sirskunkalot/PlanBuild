using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class BlueprintManager
    {
        public const string PanelName = "BlueprintManagerGUI";
        public const string ZDOBlueprintName = "BlueprintName";

        internal float selectionRadius = 10.0f;
        internal float placementOffset = 0f;

        internal float cameraOffset = 5.0f;
        internal bool updateCamera = true;

        internal bool ActiveRunestone(WorldBlueprintManager worldBlueprintManager)
        {
            return worldBlueprintManager == activeRunestone;
        }

        internal void SetActiveRunestone(WorldBlueprintManager worldBlueprintManager)
        {
            activeRunestone = worldBlueprintManager;
            if (!panel)
            {
                CreateBlueprintManagerPanel();
            }
            panel.SetActive(activeRunestone);
        }

        private const float HighlightTimeout = 1f;
        private float m_lastHightlight = 0;

        internal Piece lastHoveredPiece;

        internal readonly Dictionary<string, Blueprint> m_blueprints = new Dictionary<string, Blueprint>();
        internal readonly Dictionary<int, List<PlanPiece>> m_worldBlueprints = new Dictionary<int, List<PlanPiece>>();

        internal static ConfigEntry<float> rayDistanceConfig;
        private ConfigEntry<float> cameraOffsetIncrementConfig;
        private ConfigEntry<float> placementOffsetIncrementConfig;
        private ConfigEntry<float> selectionIncrementConfig;
        internal static ConfigEntry<bool> allowDirectBuildConfig;
        private ConfigEntry<bool> invertCameraOffsetScrollConfig;
        private ConfigEntry<bool> invertPlacementOffsetScrollConfig;
        private ConfigEntry<bool> invertSelectionScrollConfig;
        internal static ConfigEntry<KeyCode> planSwitchConfig;
        public static ConfigEntry<string> blueprintSearchDirectoryConfig;
        public static ConfigEntry<string> blueprintSaveDirectoryConfig;
        internal static ButtonConfig planSwitchButton;

        private static BlueprintManager _instance;

        private GameObject panel;

        public static BlueprintManager Instance
        {
            get
            {
                if (_instance == null) _instance = new BlueprintManager();
                return _instance;
            }
        }

        internal void Init()
        {
            //TODO: Client only - how to do? or just ignore - there are no bps and maybe someday there will be a server-wide directory of blueprints for sharing :)

            // KeyHints
            CreateCustomKeyHints();

            // Load Blueprints
            LoadKnownBlueprints();

            //Preload blueprints, some may still fail, these will be retried every time the blueprint rune is opened
            PieceManager.OnPiecesRegistered += RegisterKnownBlueprints;

            // Hooks
            On.PieceTable.UpdateAvailable += OnUpdateAvailable;
            On.Player.PlacePiece += BeforePlaceBlueprintPiece;
            On.GameCamera.UpdateCamera += AdjustCameraHeight;
            On.Player.UpdatePlacement += OnUpdatePlacement;
            On.Player.PieceRayTest += OnPieceRayTest;
            On.Humanoid.EquipItem += OnEquipItem;
            On.Humanoid.UnequipItem += OnUnequipItem;
            // On.GameCamera.UpdateCamera += GameCamera_UpdateCamera;
            // On.Player.TakeInput += OnPlayerTakeInput;
            // On.PlayerController.TakeInput += OnPlayerControllerTakeInput;

            GUIManager.OnPixelFixCreated += CreateBlueprintGUI;

            Jotunn.Logger.LogInfo("BlueprintManager Initialized");
        }

        private void CreateBlueprintGUI()
        {
            // Init GUI
            if (SceneManager.GetActiveScene().name == "main")
            {
                GUIManager.PixelFix.AddComponent<BlueprintGUI>();
            }
        }

        private void GameCamera_UpdateCamera(On.GameCamera.orig_UpdateCamera orig, GameCamera self, float dt)
        {
            if (panel && panel.activeInHierarchy)
            {
                return;
            }
            orig(self, dt);
        }

        private bool OnPlayerTakeInput(On.Player.orig_TakeInput orig, Player self)
        {
            if (panel && panel.activeInHierarchy)
            {
                return false;
            }
            return orig(self);
        }

        private void CreateBlueprintManagerPanel()
        {
            Jotunn.Logger.LogInfo("Creating Blueprint Manager panel");
            var prefabPanel = PrefabManager.Instance.GetPrefab(PanelName);
            panel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), 800f, 600f);
            panel.SetActive(false);
            prefabPanel.transform.SetParent(panel.transform);

            // panel.SetActive(false);
            // panel.name = PanelName;
            // GameObject scrollView = GUIManager.Instance.CreateScrollView(panel.transform, false, true, 3f, 3f, ColorBlock.defaultColorBlock, Color.clear, 200f, 290f);
            // ScrollRect scrollRect = scrollView.GetComponentInChildren<ScrollRect>();
            //
            // int i = 0;
            // foreach (Blueprint blueprint in m_blueprints.Values) {
            //     i++;
            //     var guiEntry = new GameObject("blueprint_entry_" + i, typeof(RectTransform), typeof(LayoutElement));
            //     guiEntry.GetComponent<LayoutElement>().preferredHeight = 64f;
            //     guiEntry.transform.SetParent(scrollRect.content.transform);
            //     RectTransform guiEntryTransform = guiEntry.GetComponent<RectTransform>();
            //     guiEntryTransform.anchoredPosition = Vector2.zero;
            //
            //     GameObject iconObject = new GameObject("icon", typeof(RectTransform), typeof(Image));
            //     iconObject.transform.SetParent(guiEntryTransform);
            //     iconObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
            //     iconObject.GetComponent<Image>().sprite = blueprint.m_piece.m_icon;
            //
            //     GameObject textObject = GUIManager.Instance.CreateText(blueprint.m_name, guiEntryTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), GUIManager.Instance.AveriaSerifBold, 18, GUIManager.Instance.ValheimOrange, true, Color.black, 130f, 64f, false);
            //     textObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            // }
        }

        private bool OnPlayerControllerTakeInput(On.PlayerController.orig_TakeInput orig, PlayerController self)
        {
            if (panel && panel.activeInHierarchy)
            {
                return false;
            }
            return orig(self);
        }

        private float originalPlaceDistance;
        private WorldBlueprintManager activeRunestone;

        private bool OnEquipItem(On.Humanoid.orig_EquipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            bool result = orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer && result &&
                item != null && item.m_shared.m_name == BlueprintRunePrefab.BlueprintRuneItemName)
            {
                originalPlaceDistance = Math.Max(Player.m_localPlayer.m_maxPlaceDistance, 8f);
                Player.m_localPlayer.m_maxPlaceDistance = rayDistanceConfig.Value;
                Jotunn.Logger.LogDebug("Setting placeDistance to " + Player.m_localPlayer.m_maxPlaceDistance);
            }
            return result;
        }

        private void OnUnequipItem(On.Humanoid.orig_UnequipItem orig, Humanoid self, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            orig(self, item, triggerEquipEffects);
            if (Player.m_localPlayer &&
                item != null && item.m_shared.m_name == BlueprintRunePrefab.BlueprintRuneItemName)
            {
                Player.m_localPlayer.m_maxPlaceDistance = originalPlaceDistance;
                Jotunn.Logger.LogDebug("Setting placeDistance to " + Player.m_localPlayer.m_maxPlaceDistance);
            }
        }

        private bool OnPieceRayTest(On.Player.orig_PieceRayTest orig, Player self, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
        {
            bool result = orig(self, out point, out normal, out piece, out heightmap, out waterSurface, water);
            lastHoveredPiece = piece;
            if (result && placementOffset != 0)
            {
                point += new Vector3(0, placementOffset, 0);
            }
            return result;
        }

        private void OnUpdateAvailable(On.PieceTable.orig_UpdateAvailable orig, PieceTable self, HashSet<string> knownRecipies, Player player, bool hideUnavailable, bool noPlacementCost)
        {
            RegisterKnownBlueprints();
            player.UpdateKnownRecipesList();
            orig(self, knownRecipies, player, hideUnavailable, noPlacementCost);
        }

        private void LoadKnownBlueprints()
        {
            Jotunn.Logger.LogMessage("Loading known blueprints");

            if (!Directory.Exists(blueprintSaveDirectoryConfig.Value))
            {
                Directory.CreateDirectory(blueprintSaveDirectoryConfig.Value);
            }

            List<string> blueprintFiles = new List<string>();
            blueprintFiles.AddRange(Directory.EnumerateFiles(blueprintSearchDirectoryConfig.Value, "*.blueprint", SearchOption.AllDirectories));
            blueprintFiles.AddRange(Directory.EnumerateFiles(blueprintSearchDirectoryConfig.Value, "*.vbuild", SearchOption.AllDirectories));

            blueprintFiles = blueprintFiles.Select(absolute => absolute.Replace(BepInEx.Paths.BepInExRootPath, null)).ToList();

            // Try to load all saved blueprints
            foreach (var relativeFilePath in blueprintFiles)
            {
                string name = Path.GetFileNameWithoutExtension(relativeFilePath);
                if (!m_blueprints.ContainsKey(name))
                {
                    var bp = Blueprint.FromPath(relativeFilePath);
                    if (bp.Load())
                    {
                        m_blueprints.Add(name, bp);
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning($"Could not load blueprint {relativeFilePath}");
                    }
                }
            }
        }

        private void CreateCustomKeyHints()
        {
            allowDirectBuildConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Allow direct build", false,
                new ConfigDescription("Allow placement of blueprints without materials", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));

            invertCameraOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Invert camera offset scroll", false,
                new ConfigDescription("Invert the direction of camera offset scrolling"));

            invertPlacementOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Invert placement height change scroll", false,
                new ConfigDescription("Invert the direction of placement offset scrolling"));

            invertSelectionScrollConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Invert selection scroll", false,
                new ConfigDescription("Invert the direction of selection scrolling"));

            rayDistanceConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Place distance", 50f,
                new ConfigDescription("Place distance while using the Blueprint Rune", new AcceptableValueRange<float>(8f, 80f)));

            cameraOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Camera offset increment", 2f,
                new ConfigDescription("Camera height change when holding Shift and scrolling while in Blueprint mode"));

            placementOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Placement offset increment", 0.1f,
                new ConfigDescription("Placement height change when holding Ctrl and scrolling while in Blueprint mode"));

            selectionIncrementConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Selection increment", 1f,
                new ConfigDescription("Selection radius increment when scrolling while in Blueprint mode"));

            planSwitchConfig = PlanBuildPlugin.Instance.Config.Bind("Blueprint Rune", "Rune mode toggle key", KeyCode.P,
                new ConfigDescription("Hotkey to switch between rune modes"));

            blueprintSearchDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind("Directories", "Search directory", ".",
                new ConfigDescription("Base directory to scan (recursively) for blueprints and vbuild files, relative paths are relative to the valheim.exe location"));

            blueprintSaveDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind("Directories", "Save directory", "BepInEx/config/PlanBuild/blueprints",
                new ConfigDescription("Directory to save blueprint files, relative paths are relative to the valheim.exe location"));

            planSwitchButton = new ButtonConfig
            {
                Name = "Rune mode toggle key",
                Key = planSwitchConfig.Value,
                HintToken = "$hud_bp_toggle_plan_mode"
            };

            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, planSwitchButton);

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = planSwitchButton.Name, HintToken = "$hud_bp_switch_to_blueprint_mode" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" }
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.MakeBlueprintName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = planSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcapture" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" },
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.DeletePlansName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = planSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bp_delete_plans" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" },
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = BlueprintRunePrefab.UndoBlueprintName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = planSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bp_undo_blueprint" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" },
                }
            });
            foreach (var entry in m_blueprints)
            {
                entry.Value.CreateKeyHint();
            }
        }

        public static List<Piece> GetPiecesInRadius(Vector3 position, float radius)
        {
            List<Piece> result = new List<Piece>();
            foreach (var piece in Piece.m_allPieces)
            {
                if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(piece.transform.position.x, piece.transform.position.z)) <= radius
                    && Blueprint.CanCapture(piece))
                {
                    result.Add(piece);
                }
            }
            return result;
        }

        private void RegisterKnownBlueprints()
        {
            // Client only
            if (!ZNet.instance.IsDedicated())
            {
                // Create prefabs for all known blueprints
                foreach (var bp in Instance.m_blueprints.Values)
                {
                    bp.CreatePrefab();
                }
            }
        }

        private void Reset()
        {
            Instance.cameraOffset = 5f;
            Instance.placementOffset = 0f;
        }

        /// <summary>
        ///     Incept placing of the meta pieces.
        ///     Cancels the real placement of the placeholder pieces.
        /// </summary>
        private bool BeforePlaceBlueprintPiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
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
                if (Player.m_localPlayer.m_placementStatus == Player.PlacementStatus.Valid
                    && piece.name != BlueprintRunePrefab.BlueprintSnapPointName
                    && piece.name != BlueprintRunePrefab.BlueprintCenterPointName
                    && piece.name.StartsWith("piece_blueprint"))
                {
                    return PlaceBlueprint(self, piece);
                }
                else if (piece.name.StartsWith(BlueprintRunePrefab.DeletePlansName))
                {
                    return DeletePlans(self);
                }
                else if (piece.name.StartsWith(BlueprintRunePrefab.UndoBlueprintName))
                {
                    return UndoBlueprint();
                }
            }

            return orig(self, piece);
        }

        private bool UndoBlueprint()
        {
            if (lastHoveredPiece)
            {
                if (lastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                {
                    ZDOID blueprintID = planPiece.GetBlueprintID();
                    if (blueprintID != ZDOID.None)
                    {
                        int removedPieces = RemoveBlueprint(blueprintID);

                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));
                    }
                }
            }

            return false;
        }

        private bool DeletePlans(Player player)
        {
            var circleProjector = player.m_placementGhost.GetComponent<CircleProjector>();
            if (circleProjector != null)
            {
                Object.Destroy(circleProjector);
            }

            Vector3 deletePosition = player.m_placementMarkerInstance.transform.position;
            int removedPieces = 0;
            foreach (Piece pieceToRemove in GetPiecesInRadius(deletePosition, selectionRadius))
            {
                if (pieceToRemove.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Remove();
                    removedPieces++;
                }
            }
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_removed_plans", removedPieces.ToString()));

            return false;
        }

        private static bool PlaceBlueprint(Player player, Piece piece)
        {
            Blueprint bp = Instance.m_blueprints[piece.m_name];
            var transform = player.m_placementGhost.transform;
            var position = transform.position;
            var rotation = transform.rotation;

            bool placeDirect = ZInput.GetButton("Crouch");
            if (placeDirect && !allowDirectBuildConfig.Value)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_direct_build_disabled");
                return false;
            }

            if (ZInput.GetButton("AltPlace"))
            {
                Vector2 extent = bp.GetExtent();
                FlattenTerrain.FlattenForBlueprint(transform, extent.x, extent.y, bp.PieceEntries);
            }

            uint cntEffects = 0u;
            uint maxEffects = 10u;

            GameObject blueprintPrefab = PrefabManager.Instance.GetPrefab(Blueprint.BlueprintPrefabName);

            GameObject blueprintObject = Object.Instantiate(blueprintPrefab, position, rotation);

            ZDO blueprintZDO = blueprintObject.GetComponent<ZNetView>().GetZDO();
            blueprintZDO.Set(ZDOBlueprintName, bp.Name);
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

        private static bool MakeBlueprint(Player self)
        {
            var circleProjector = self.m_placementGhost.GetComponent<CircleProjector>();
            if (circleProjector != null)
            {
                Object.Destroy(circleProjector);
            }

            var bpname = $"blueprint{Instance.m_blueprints.Count() + 1:000}";
            Jotunn.Logger.LogInfo($"Capturing blueprint {bpname}");

            var bp = new Blueprint();
            Vector3 capturePosition = self.m_placementMarkerInstance.transform.position;
            if (bp.Capture(capturePosition, Instance.selectionRadius))
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

        /// <summary>
        ///     Add some camera height while planting a blueprint
        /// </summary>
        private void AdjustCameraHeight(On.GameCamera.orig_UpdateCamera orig, GameCamera self, float dt)
        {
            orig(self, dt);

            if (updateCamera
                && Player.m_localPlayer
                && Player.m_localPlayer.InPlaceMode()
                && Player.m_localPlayer.m_placementGhost)
            {
                var pieceName = Player.m_localPlayer.m_placementGhost.name;
                if (pieceName.StartsWith("make_blueprint")
                    || pieceName.StartsWith("piece_blueprint"))
                {
                    self.transform.position += new Vector3(0, Instance.cameraOffset, 0);
                }
            }
        }

        public void HighlightPieces(Vector3 startPosition, float radius, Color color)
        {
            if (Time.time < m_lastHightlight + HighlightTimeout)
            {
                return;
            }
            foreach (var piece in GetPiecesInRadius(startPosition, radius))
            {
                if (piece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.Highlight(color);
                }
            }
            m_lastHightlight = Time.time;
            return;
        }

        public int HighlightPlans(Vector3 startPosition, float radius, Color color)
        {
            int capturedPieces = 0;
            foreach (var piece in GetPiecesInRadius(startPosition, radius))
            {
                if (piece.TryGetComponent(out PlanPiece planPiece))
                {
                    planPiece.m_wearNTear.Highlight(color);
                }
                capturedPieces++;
            }
            return capturedPieces;
        }

        private void FlashBlueprint(ZDOID blueprintID, Color color)
        {
            foreach (PlanPiece planPiece in GetPlanPiecesForBlueprint(blueprintID))
            {
                planPiece.m_wearNTear.Highlight(color);
            }
        }

        private List<PlanPiece> GetPlanPiecesForBlueprint(ZDOID blueprintID)
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

        private static ZDOIDSet GetPlanPieces(ZDO blueprintZDO)
        {
            byte[] data = blueprintZDO.GetByteArray(PlanPiece.zdoBlueprintPiece);
            if (data == null)
            {
                return null;
            }
            return ZDOIDSet.From(new ZPackage(data));
        }

        private int RemoveBlueprint(ZDOID blueprintID)
        {
            int removedPieces = 0;
            Jotunn.Logger.LogInfo("Removing all pieces of blueprint " + blueprintID);
            foreach (PlanPiece planPiece in GetPlanPiecesForBlueprint(blueprintID))
            {
                planPiece.Remove();
                removedPieces++;
            }

            GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
            if (blueprintObject)
            {
                ZNetScene.instance.Destroy(blueprintObject);
            }
            return removedPieces;
        }

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
        ///     Show and change blueprint selection radius
        /// </summary>
        private void OnUpdatePlacement(On.Player.orig_UpdatePlacement orig, Player self, bool takeInput, float dt)
        {
            orig(self, takeInput, dt);

            if (self.m_placementGhost)
            {
                var piece = self.m_placementGhost.GetComponent<Piece>();
                if (piece != null)
                {
                    if (piece.name == "make_blueprint" && !piece.IsCreator())
                    {
                        if (!self.m_placementMarkerInstance)
                        {
                            return;
                        }

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

                        var circleProjector = self.m_placementMarkerInstance.GetComponent<CircleProjector>();
                        if (circleProjector == null)
                        {
                            circleProjector = self.m_placementMarkerInstance.AddComponent<CircleProjector>();
                            circleProjector.m_prefab = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponentInChildren<CircleProjector>().m_prefab;

                            // Force calculation of segment count
                            circleProjector.m_radius = -1;
                            circleProjector.Start();
                        }

                        if (circleProjector.m_radius != Instance.selectionRadius)
                        {
                            circleProjector.m_radius = Instance.selectionRadius;
                            circleProjector.m_nrOfSegments = (int)circleProjector.m_radius * 4;
                            circleProjector.Update();
                            Jotunn.Logger.LogDebug($"Setting radius to {Instance.selectionRadius}");
                        }

                        HighlightPieces(self.m_placementMarkerInstance.transform.position, Instance.selectionRadius, Color.green);
                    }
                    else if (piece.name.StartsWith(Blueprint.BlueprintPrefabName))
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (self.m_placementMarkerInstance)
                        {
                            Object.DestroyImmediate(self.m_placementMarkerInstance);
                        }

                        // Reset rotation when changing camera
                        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                        if (scrollWheel != 0f)
                        {
                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                UpdateCameraOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                UpdatePlacementOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                        }
                    }
                    else if (piece.name.StartsWith(BlueprintRunePrefab.DeletePlansName))
                    {
                        if (!self.m_placementMarkerInstance)
                        {
                            return;
                        }

                        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                        if (scrollWheel != 0)
                        {
                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                UpdateCameraOffset(scrollWheel);
                                UndoRotation(self, scrollWheel);
                            }
                            else
                            {
                                UpdateSelectionRadius(scrollWheel);
                            }
                        }

                        var circleProjector = self.m_placementMarkerInstance.GetComponent<CircleProjector>();
                        if (circleProjector == null)
                        {
                            circleProjector = self.m_placementMarkerInstance.AddComponent<CircleProjector>();
                            circleProjector.m_prefab = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponentInChildren<CircleProjector>().m_prefab;

                            // Force calculation of segment count
                            circleProjector.m_radius = -1;
                            circleProjector.Start();
                        }

                        if (circleProjector.m_radius != Instance.selectionRadius)
                        {
                            circleProjector.m_radius = Instance.selectionRadius;
                            circleProjector.m_nrOfSegments = (int)circleProjector.m_radius * 4;
                            circleProjector.Update();
                            Jotunn.Logger.LogDebug($"Setting radius to {Instance.selectionRadius}");
                        }

                        if (Time.time > m_lastHightlight + HighlightTimeout)
                        {
                            HighlightPlans(self.m_placementMarkerInstance.transform.position, Instance.selectionRadius, Color.red);
                            m_lastHightlight = Time.time;
                        }
                    }
                    else if (piece.name.StartsWith(BlueprintRunePrefab.UndoBlueprintName))
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (self.m_placementMarkerInstance)
                        {
                            Object.DestroyImmediate(self.m_placementMarkerInstance);
                        }

                        if (Time.time > m_lastHightlight + HighlightTimeout)
                        {
                            if (lastHoveredPiece)
                            {
                                if (lastHoveredPiece.TryGetComponent(out PlanPiece planPiece))
                                {
                                    ZDOID blueprintID = planPiece.GetBlueprintID();
                                    if (blueprintID != ZDOID.None)
                                    {
                                        FlashBlueprint(blueprintID, Color.red);
                                    }
                                }
                            }
                            m_lastHightlight = Time.time;
                        }
                    }
                    else
                    {
                        // Destroy placement marker instance to get rid of the circleprojector
                        if (self.m_placementMarkerInstance)
                        {
                            Object.DestroyImmediate(self.m_placementMarkerInstance);
                        }

                        Reset();
                    }
                }
            }
        }

        private void UpdatePlacementOffset(float scrollWheel)
        {
            bool scrollingDown = scrollWheel < 0f;
            if (invertPlacementOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                Instance.placementOffset -= placementOffsetIncrementConfig.Value;
            }
            else
            {
                Instance.placementOffset += placementOffsetIncrementConfig.Value;
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

        private void UpdateCameraOffset(float scrollWheel)
        {
            // TODO: base min/max off of selected piece dimensions
            float minOffset = 2f;
            float maxOffset = 20f;
            bool scrollingDown = scrollWheel < 0f;
            if (invertCameraOffsetScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                Instance.cameraOffset = Mathf.Clamp(Instance.cameraOffset += cameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
            else
            {
                Instance.cameraOffset = Mathf.Clamp(Instance.cameraOffset -= cameraOffsetIncrementConfig.Value, minOffset, maxOffset);
            }
        }

        private void UpdateSelectionRadius(float scrollWheel)
        {
            bool scrollingDown = scrollWheel < 0f;
            if (invertSelectionScrollConfig.Value)
            {
                scrollingDown = !scrollingDown;
            }
            if (scrollingDown)
            {
                Instance.selectionRadius -= selectionIncrementConfig.Value;
                if (Instance.selectionRadius < 2f)
                {
                    Instance.selectionRadius = 2f;
                }
            }
            else
            {
                Instance.selectionRadius += selectionIncrementConfig.Value;
            }
        }
    }
}