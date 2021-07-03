// PlanBuild
// a Valheim mod skeleton using Jötunn
//
// File:    PlanBuild.cs
// Project: PlanBuild

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints;
using PlanBuild.PlanBuild;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.1.0")]
    [BepInDependency(Patches.buildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.craftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.gizmoGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.OnlySyncWhenInstalled, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.3.0";

        public static PlanBuildPlugin Instance;
        public static ConfigEntry<bool> showAllPieces;
        public static ConfigEntry<bool> configTransparentGhostPlacement;

        internal PlanCrystalPrefab planCrystalPrefab;
        internal BlueprintRunePrefab blueprintRunePrefab;

        internal static bool showRealTextures;

        public static readonly Dictionary<string, PlanPiecePrefab> planPiecePrefabs = new Dictionary<string, PlanPiecePrefab>();

        public void Awake()
        {
            Instance = this;

            // Create plan piece table for the Hammer
            PieceManager.Instance.AddPieceTable(new CustomPieceTable(
                PlanPiecePrefab.PlanHammerPieceTableName,
                new PieceTableConfig()
                {
                    CanRemovePieces = true,
                    UseCategories = true
                }
             ));

            // Configs
            SetupConfig();

            // Init Blueprints
            Assembly assembly = typeof(PlanBuildPlugin).Assembly;
            AssetBundle blueprintsBundle = AssetUtils.LoadAssetBundleFromResources("blueprints", assembly);

            blueprintRunePrefab = new BlueprintRunePrefab(blueprintsBundle);
            blueprintsBundle.Unload(false);
            BlueprintManager.Instance.Init();

            AssetBundle planbuildBundle = AssetUtils.LoadAssetBundleFromResources("planbuild", assembly);
            planTotemPrefab = new PlanTotemPrefab(planbuildBundle);
            planbuildBundle.Unload(false);

            // Init Shader
            ShaderHelper.planShader = Shader.Find("Lux Lit Particles/ Bumped");

            // Harmony patching
            Patches.Apply();

            // Hooks
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;
            ItemManager.OnItemsRegistered += OnItemsRegistered;
            On.Player.Awake += OnPlayerAwake;
        }

        private void SetupConfig()
        {
            showAllPieces = Config.Bind("General", "Plan unknown pieces", false, new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            PlanTotem.radiusConfig = Config.Bind("General", "Plan totem build radius", 30f, new ConfigDescription("Build radius of the Plan totem"));

            PlanTotem.radiusConfig.SettingChanged += UpdatePlanTotem;

            configTransparentGhostPlacement = Config.Bind("Visual", "Transparent Ghost Placement", false, new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)"));
            ShaderHelper.unsupportedColorConfig = Config.Bind("Visual", "Unsupported color", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported plan pieces"));
            ShaderHelper.supportedPlanColorConfig = Config.Bind("Visual", "Supported color", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported plan pieces"));
            ShaderHelper.transparencyConfig = Config.Bind("Visual", "Transparency", 0.30f, new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f)));
            PlanTotemPrefab.glowColorConfig = Config.Bind("Visual", "Plan totem glow color", Color.cyan, new ConfigDescription("Color of the glowing lines on the Plan totem"));

            ShaderHelper.unsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.supportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.transparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;
            PlanTotemPrefab.glowColorConfig.SettingChanged += UpdatePlanTotem;
            showAllPieces.SettingChanged += UpdateKnownRecipes;
        }

        private void OnPlayerAwake(On.Player.orig_Awake orig, Player self)
        {
            orig(self);
            LateScanHammer();
        }

        public void Update()
        {
            if (BlueprintGUI.IsAvailable() && Input.GetKeyDown(BlueprintConfig.serverGuiSwitchKey))
            {
                BlueprintGUI.Instance.Toggle();
            }

            Player player = Player.m_localPlayer;
            if (ZInput.instance == null || player == null)
            {
                return;
            }

            if (!CheckInput())
            {
                return;
            }

            // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
            // If we hold the button down, it won't spam toggle our menu.
            if (ZInput.GetButtonDown(BlueprintManager.PlanSwitchButton.Name))
            {
                TogglePlanBuildMode();
            }
        }

        private void TogglePlanBuildMode()
        {
            if (ScanHammer(lateAdd: true))
            {
                UpdateKnownRecipes();
            }
            if (Player.m_localPlayer.m_visEquipment.m_rightItem != BlueprintRunePrefab.BlueprintRuneName)
            {
                return;
            }
            ItemDrop.ItemData blueprintRune = Player.m_localPlayer.GetInventory().GetItem(BlueprintRunePrefab.BlueprintRuneItemName);
            if (blueprintRune == null)
            {
                return;
            }
            PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
            PieceTable bluePrintRunePieceTable = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
            if (blueprintRune.m_shared.m_buildPieces == planHammerPieceTable)
            {
                blueprintRune.m_shared.m_buildPieces = bluePrintRunePieceTable;
                if (blueprintRune.m_shared.m_buildPieces.m_selectedCategory == 0)
                {
                    blueprintRune.m_shared.m_buildPieces.m_selectedCategory = PieceManager.Instance.AddPieceCategory(BlueprintRunePrefab.PieceTableName, BlueprintRunePrefab.CategoryTools);
                }
            }
            else
            {
                blueprintRune.m_shared.m_buildPieces = planHammerPieceTable;
            }
            Player.m_localPlayer.UnequipItem(blueprintRune);
            Player.m_localPlayer.EquipItem(blueprintRune);

            Color color = blueprintRune.m_shared.m_buildPieces == planHammerPieceTable ? Color.red : Color.cyan;
            ShaderHelper.SetEmissionColor(Player.m_localPlayer.m_visEquipment.m_rightItemInstance, color);

            Player.m_localPlayer.UpdateAvailablePiecesList();
        }

        private void UpdatePlanTotem(object sender, EventArgs e)
        {
            planTotemPrefab.SettingsUpdated();
            foreach (PlanTotem planTotem in PlanTotem.m_allPlanTotems)
            {
                PlanTotemPrefab.UpdateGlowColor(planTotem.gameObject);
            }
        }

        public void OnDestroy()
        {
            Patches.Remove();
        }

        private void AddClonedItems()
        {
            try
            {
                PlanCrystalPrefab.startPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.prefabName + "StartEffect", "vfx_blocked");
                PlanCrystalPrefab.startPlanCrystalEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                PlanCrystalPrefab.stopPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.prefabName + "StopEffect", "vfx_blocked");
                PlanCrystalPrefab.stopPlanCrystalEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();

                planCrystalPrefab = new PlanCrystalPrefab();
                planCrystalPrefab.Setup();
                ItemManager.Instance.AddItem(planCrystalPrefab);
            }
            finally
            {
                ItemManager.OnVanillaItemsAvailable -= AddClonedItems;
            }
        }

        private void OnItemsRegistered()
        {
            planCrystalPrefab.FixShader();
        }

        internal bool addedHammer = false;
        private PlanTotemPrefab planTotemPrefab;

        internal void InitialScanHammer()
        {
            try
            {
                this.ScanHammer(false);
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= InitialScanHammer;
            }
        }

        internal void LateScanHammer()
        {
            ScanHammer(true);
        }

        internal bool ScanHammer(bool lateAdd)
        {
            Jotunn.Logger.LogDebug("Scanning Hammer PieceTable for Pieces");
            PieceTable hammerPieceTable = PieceManager.Instance.GetPieceTable("Hammer");
            if (!hammerPieceTable)
            {
                return false;
            }
            bool addedPiece = false;
            foreach (GameObject piecePrefab in hammerPieceTable.m_pieces)
            {
                if (!piecePrefab)
                {
                    Logger.LogWarning("Invalid prefab in Hammer PieceTable");
                    continue;
                }
                Piece piece = piecePrefab.GetComponent<Piece>();
                if (!piece)
                {
                    Logger.LogWarning("Recipe in Hammer has no Piece?! " + piecePrefab.name);
                    continue;
                }
                try
                {
                    if (piece.name == "piece_repair")
                    {
                        if (!addedHammer)
                        {
                            PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
                            if (planHammerPieceTable != null)
                            {
                                planHammerPieceTable.m_pieces.Add(piecePrefab);
                                addedHammer = true;
                            }
                        }
                        continue;
                    }
                    if (planPiecePrefabs.ContainsKey(piece.name))
                    {
                        continue;
                    }
                    if (!CanCreatePlan(piece))
                    {
                        continue;
                    }
                    if (!EnsurePrefabRegistered(piece))
                    {
                        continue;
                    }

                    PlanPiecePrefab planPiece = new PlanPiecePrefab(piece);
                    PieceManager.Instance.AddPiece(planPiece);
                    planPiecePrefabs.Add(piece.name, planPiece);
                    PrefabManager.Instance.RegisterToZNetScene(planPiece.PiecePrefab);
                    if (lateAdd)
                    {
                        PieceTable pieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
                        if (!pieceTable.m_pieces.Contains(planPiece.PiecePrefab))
                        {
                            pieceTable.m_pieces.Add(planPiece.PiecePrefab);
                            addedPiece = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning("Error while creating plan of " + piece.name + ": " + e);
                };
            }
            return addedPiece;
        }

        public static bool CanCreatePlan(Piece piece)
        {
            return piece.m_enabled
                && piece.GetComponent<Ship>() == null
                && piece.GetComponent<Plant>() == null
                && piece.GetComponent<TerrainModifier>() == null
                && piece.m_resources.Length != 0;
        }

        private bool EnsurePrefabRegistered(Piece piece)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(piece.gameObject.name);
            if (prefab)
            {
                return true;
            }
            Logger.LogWarning("Piece " + piece.name + " in Hammer not fully registered? Could not find prefab " + piece.gameObject.name);
            if (!ZNetScene.instance.m_prefabs.Contains(piece.gameObject))
            {
                Logger.LogWarning(" Not registered in ZNetScene.m_prefabs! Adding now");
                ZNetScene.instance.m_prefabs.Add(piece.gameObject);
            }
            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(piece.gameObject.name.GetStableHashCode()))
            {
                Logger.LogWarning(" Not registered in ZNetScene.m_namedPrefabs! Adding now");
                ZNetScene.instance.m_namedPrefabs[piece.gameObject.name.GetStableHashCode()] = piece.gameObject;
            }
            //Prefab was added incorrectly, make sure the game doesn't delete it when logging out
            GameObject prefabParent = piece.gameObject.transform.parent?.gameObject;
            if (!prefabParent)
            {
                Logger.LogWarning(" Prefab has no parent?! Adding to Jotunn");
                PrefabManager.Instance.AddPrefab(piece.gameObject);
            }
            else if (prefabParent.scene.buildIndex != -1)
            {
                Logger.LogWarning(" Prefab container not marked as DontDestroyOnLoad! Marking now");
                Object.DontDestroyOnLoad(prefabParent);
            }
            return PrefabManager.Instance.GetPrefab(piece.gameObject.name) != null;
        }

        private void UpdateKnownRecipes(object sender, EventArgs e)
        {
            UpdateKnownRecipes();
        }

        private void UpdateKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (!showAllPieces.Value)
            {
                foreach (PlanPiecePrefab planPieceConfig in planPiecePrefabs.Values)
                {
                    if (!player.HaveRequirements(planPieceConfig.originalPiece, Player.RequirementMode.IsKnown))
                    {
#if DEBUG
                        Jotunn.Logger.LogInfo("Removing planned piece from m_knownRecipes: " + planPieceConfig.Piece.m_name);
#endif
                        player.m_knownRecipes.Remove(planPieceConfig.Piece.m_name);
                    }
#if DEBUG
                    else
                    {
                        Jotunn.Logger.LogDebug("Player knows about " + planPieceConfig.originalPiece.m_name);
                    }
#endif
                }
            }
            player.UpdateKnownRecipesList();
            PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName)
                .UpdateAvailable(player.m_knownRecipes, player, true, false);
        }

        private string[] GetAcceptableKeyCodes()
        {
            Array keyCodes = Enum.GetValues(typeof(KeyCode));
            int i = 0;
            string[] acceptable = new string[keyCodes.Length];
            foreach (System.Object keyCode in keyCodes)
            {
                acceptable[i++] = keyCode.ToString();
            }
            return acceptable;
        }

        private void UpdateAllPlanPieceTextures(object sender, EventArgs e)
        {
            ShaderHelper.ClearCache();
            UpdateAllPlanPieceTextures();
        }

        public static void UpdateAllPlanPieceTextures()
        {
            if (showRealTextures
                && Player.m_localPlayer.m_placementGhost
                && Player.m_localPlayer.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                ShaderHelper.UpdateTextures(Player.m_localPlayer.m_placementGhost, ShaderHelper.ShaderState.Skuld);
            }
            foreach (PlanPiece planPiece in Object.FindObjectsOfType<PlanPiece>())
            {
                planPiece.UpdateTextures();
            }
        }

        private bool CheckInput()
        {
            return (!Chat.instance || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !InventoryGui.IsVisible()
                && !StoreGui.IsVisible()
                && !Menu.IsVisible()
                && !Minimap.IsOpen()
                && !Player.m_localPlayer.InCutscene();
        }

        public static string GetAssetPath(string assetName, bool isDirectory = false)
        {
            string text = Path.Combine(BepInEx.Paths.PluginPath, PluginName, assetName);
            if (isDirectory)
            {
                if (!Directory.Exists(text))
                {
                    Assembly assembly = typeof(PlanBuildPlugin).Assembly;
                    text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                    if (!Directory.Exists(text))
                    {
                        Jotunn.Logger.LogWarning($"Could not find directory ({assetName}).");
                        return null;
                    }
                }
                return text;
            }
            if (!File.Exists(text))
            {
                Assembly assembly = typeof(PlanBuildPlugin).Assembly;
                text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                if (!File.Exists(text))
                {
                    Jotunn.Logger.LogWarning($"Could not find asset ({assetName}).");
                    return null;
                }
            }
            return text;
        }
    }
}