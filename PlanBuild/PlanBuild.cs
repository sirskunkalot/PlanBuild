// PlanBuild
// a Valheim mod skeleton using Jötunn
// 
// File:    PlanBuild.cs
// Project: PlanBuild

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static PlanBuild.ShaderHelper;
using Object = UnityEngine.Object;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(Patches.buildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.craftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuild : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.1.2";

        public static ManualLogSource logger;
        public const string PlanBuildButton = "PlanBuild_PlanBuildMode";

        Harmony harmony;

        public static ConfigEntry<string> buildModeHotkeyConfig;
        public static ConfigEntry<bool> showAllPieces;
        public static ConfigEntry<bool> configTransparentGhostPlacement;
        public static ConfigEntry<bool> configBuildShare;
        private PlanHammerPrefabConfig planHammerPrefabConfig;
        public static PlanBuild Instance;

        public static readonly Dictionary<string, PlanPiecePrefabConfig> planPiecePrefabConfigs = new Dictionary<string, PlanPiecePrefabConfig>();

        private void Awake()
        {
            harmony = new Harmony("marcopogo.PlanBuild");
            Instance = this;
            logger = Logger;
            PlanPiece.logger = logger;
            PlanCrystalPrefabConfig.logger = logger;
            PlanPiecePrefabConfig.logger = logger;
            logger.LogInfo("Harmony patches");
            Patches.Apply(harmony);
            
            ShaderHelper.planShader = Shader.Find("Lux Lit Particles/ Bumped");

            buildModeHotkeyConfig = base.Config.Bind<string>("General", "Hammer mode toggle Hotkey", "P", new ConfigDescription("Hotkey to switch between Hammer modes", new AcceptableValueList<string>(GetAcceptableKeyCodes())));
            showAllPieces = base.Config.Bind<bool>("General", "Plan unknown pieces", false, new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            configBuildShare = base.Config.Bind<bool>("BuildShare", "Place as planned pieces", false, new ConfigDescription("Place .vbuild as planned pieces instead", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            configTransparentGhostPlacement = base.Config.Bind<bool>("Visual", "Transparent Ghost Placement", false, new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)"));
             
            ShaderHelper.unsupportedColorConfig = base.Config.Bind<Color>("Visual", "Unsupported color", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported plan pieces"));
            ShaderHelper.supportedPlanColorConfig = base.Config.Bind<Color>("Visual", "Supported color", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported plan pieces"));

            ShaderHelper.transparencyConfig = base.Config.Bind<float>("Visual", "Transparency", 0.30f, new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f)));
            ShaderHelper.supportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.unsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.transparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;

            buildModeHotkeyConfig.SettingChanged += UpdateBuildKey;
            On.ObjectDB.CopyOtherDB += AddClonedItems;

            PrefabManager.OnPrefabsRegistered += ScanHammer;
            ItemManager.OnItemsRegistered += OnItemsRegistered;
            PieceManager.OnPiecesRegistered += LateScanHammer;

            showAllPieces.SettingChanged += UpdateKnownRecipes;

            UpdateBuildKey();
        }

        private void LateScanHammer()
        {
            ScanHammer(true);
        }

        private void UpdateBuildKey(object sender, EventArgs e)
        {
            UpdateBuildKey();
        }

        private void UpdateBuildKey()
        {
            if (Enum.TryParse(buildModeHotkeyConfig.Value, out KeyCode keyCode))
            {
                InputManager.Instance.AddButton(PluginGUID, new Jotunn.Configs.ButtonConfig()
                {
                    Name = PlanBuildButton,
                    Key = keyCode
                });
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchAll(PluginGUID);
        }

        private void OnItemsRegistered()
        {
            planHammerPrefabConfig.PrefabCreated();
            planCrystalPrefabConfig.PrefabCreated();
            hammerPrefab = PrefabManager.Instance.GetPrefab("Hammer");
            hammerPrefabItemDrop = hammerPrefab.GetComponent<ItemDrop>();
            
        }

        private void AddClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            try
            {
                PieceManager.Instance.AddPieceTable(PlanHammerPrefabConfig.pieceTableName);
                PlanCrystalPrefabConfig.startPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefabConfig.prefabName + "StartEffect", "vfx_blocked");
                PlanCrystalPrefabConfig.startPlanCrystalEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                PlanCrystalPrefabConfig.stopPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefabConfig.prefabName + "StopEffect", "vfx_blocked");
                PlanCrystalPrefabConfig.stopPlanCrystalEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();
                planHammerPrefabConfig = new PlanHammerPrefabConfig();
                planCrystalPrefabConfig = new PlanCrystalPrefabConfig();

                ItemManager.Instance.AddItem(planHammerPrefabConfig);
                ItemManager.Instance.AddItem(planCrystalPrefabConfig);
                planHammerPrefabConfig.Register();
                planCrystalPrefabConfig.Register();

            }
            finally
            {
                On.ObjectDB.CopyOtherDB -= AddClonedItems;
            }
            orig(self, other);
        }

        bool addedHammer = false;

        internal void ScanHammer()
        {
            this.ScanHammer(false);
        }

        internal void ScanHammer(bool lateAdd)
        {
            try
            {
                logger.LogDebug("Scanning Hammer PieceTable for Pieces");
                foreach (GameObject hammerRecipe in PieceManager.Instance.GetPieceTable("Hammer").m_pieces)
                {
                    if(hammerRecipe == null)
                    {
                        logger.LogWarning("null recipe in Hammer PieceTable");
                        continue;
                    }
                    Piece piece = hammerRecipe.GetComponent<Piece>();

                    if (piece.name == "piece_repair")
                    {
                        if (!addedHammer)
                        {
                            PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(PlanHammerPrefabConfig.pieceTableName);
                            if(planHammerPieceTable != null)
                            {
                                planHammerPieceTable.m_pieces.Add(hammerRecipe);
                                addedHammer = true;
                            }
                        }
                        continue;
                    }
                    if (planPiecePrefabConfigs.ContainsKey(piece.name))
                    {
                        continue;
                    }
                    if (!piece.m_enabled
                        || piece.GetComponent<Ship>() != null
                        || piece.GetComponent<Plant>() != null
                        || piece.GetComponent<TerrainModifier>() != null
                        || piece.m_resources.Length == 0)
                    {
                        continue;
                    }
                    PlanPiecePrefabConfig prefabConfig = new PlanPiecePrefabConfig(piece);
                    PieceManager.Instance.AddPiece(prefabConfig);
                    planPiecePrefabConfigs.Add(piece.name, prefabConfig);
                    PrefabManager.Instance.RegisterToZNetScene(prefabConfig.PiecePrefab);
                    if (lateAdd)
                    {
                        PieceTable pieceTable = PieceManager.Instance.GetPieceTable(PlanHammerPrefabConfig.pieceTableName);
                        if (!pieceTable.m_pieces.Contains(prefabConfig.PiecePrefab))
                        {
                            pieceTable.m_pieces.Add(prefabConfig.PiecePrefab);
                        }
                    }
                }
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= ScanHammer;
            }
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
                foreach (PlanPiecePrefabConfig planPieceConfig in planPiecePrefabConfigs.Values)
                {
                    if (!player.HaveRequirements(planPieceConfig.originalPiece, Player.RequirementMode.IsKnown))
                    {
                        logger.LogInfo("Removing planned piece from m_knownRecipes: " + planPieceConfig.Piece.m_name);
                        player.m_knownRecipes.Remove(planPieceConfig.Piece.m_name);
                    }
                    else
                    {
                        logger.LogDebug("Player knows about " + planPieceConfig.originalPiece.m_name);
                    }
                }
            }
            player.UpdateKnownRecipesList();
            PieceManager.Instance.GetPieceTable(PlanHammerPrefabConfig.pieceTableName)
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
            UpdateAllPlanPieceTextures();
        }

        public static void UpdateAllPlanPieceTextures()
        {
            foreach (PlanPiece planPiece in Object.FindObjectsOfType<PlanPiece>())
            {
                planPiece.UpdateTextures();
            }
        }

        public void Update()
        {
            Player player = Player.m_localPlayer;
            if (ZInput.instance == null
                || player == null)
            {
                return;
            }

            if (!CheckInput())
            {
                return;
            }

            // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
            // If we hold the button down, it won't spam toggle our menu.
            if (ZInput.GetButtonDown(PlanBuildButton))
            {
                TogglePlanBuildMode();
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

        private PlanCrystalPrefabConfig planCrystalPrefabConfig;
        internal static bool showRealTextures;
        private GameObject hammerPrefab;
        private ItemDrop hammerPrefabItemDrop;

        private void TogglePlanBuildMode()
        {
            ScanHammer(lateAdd: true);
            UpdateKnownRecipes();
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData hammerItem = player.GetInventory().GetItem(hammerPrefabItemDrop.m_itemData.m_shared.m_name);
            ItemDrop.ItemData planHammerItem = player.GetInventory().GetItem(planHammerPrefabConfig.itemData.m_shared.m_name);
            if (hammerItem == null && planHammerItem == null)
            {
                return;
            }
            if (hammerItem != null)
            {
                logger.LogInfo("Replacing Hammer with PlanHammer");
                player.GetInventory().RemoveOneItem(hammerItem);
                player.GetInventory().AddItem(
                    name: PlanHammerPrefabConfig.planHammerName,
                    stack: 1,
                    durability: hammerItem.m_durability,
                    pos: hammerItem.m_gridPos,
                    equiped: false,
                    quality: hammerItem.m_quality,
                    variant: hammerItem.m_variant,
                    crafterID: hammerItem.m_crafterID,
                    crafterName: hammerItem.m_crafterName
                );
                if (hammerItem.m_equiped)
                {
                    player.EquipItem(player.GetInventory().GetItemAt(hammerItem.m_gridPos.x, hammerItem.m_gridPos.y));
                }
            }
            else
            {
                logger.LogInfo("Replacing PlanHammer with Hammer");
                player.GetInventory().RemoveOneItem(planHammerItem);
                player.GetInventory().AddItem(
                    name: hammerPrefab.name,
                    stack: 1,
                    durability: planHammerItem.m_durability,
                    pos: planHammerItem.m_gridPos,
                    equiped: false,
                    quality: planHammerItem.m_quality,
                    variant: planHammerItem.m_variant,
                    crafterID: planHammerItem.m_crafterID,
                    crafterName: planHammerItem.m_crafterName
                );
                if (planHammerItem.m_equiped)
                {
                    player.EquipItem(player.GetInventory().GetItemAt(planHammerItem.m_gridPos.x, planHammerItem.m_gridPos.y));
                }
            }
        }

        public static string GetAssetPath(string assetName, bool isDirectory = false)
        {
            string text = Path.Combine(BepInEx.Paths.PluginPath, PluginName, assetName);
            if (isDirectory)
            {
                if (!Directory.Exists(text))
                {
                    Assembly assembly = typeof(PlanBuild).Assembly;
                    text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                    if (!Directory.Exists(text))
                    {
                        logger.LogWarning($"Could not find directory ({assetName}).");
                        return null;
                    }
                }
                return text;
            }
            if (!File.Exists(text))
            {
                Assembly assembly = typeof(PlanBuild).Assembly;
                text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                if (!File.Exists(text))
                {
                    logger.LogWarning($"Could not find asset ({assetName}).");
                    return null;
                }
            }
            return text;
        }

    }
}