// PlanBuild
// a Valheim mod skeleton using Jötunn
// 
// File:    PlanBuild.cs
// Project: PlanBuild

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
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
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuild : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.1.1";

        public static ManualLogSource logger;
        public const string PlanBuildButton = "PlanBuild_PlanBuildMode";


        Harmony harmony = new Harmony("marcopogo.PlanBuild");

        public static ConfigEntry<string> buildModeHotkeyConfig;
        public static ConfigEntry<bool> showAllPieces;
        public static ConfigEntry<bool> configTransparentGhostPlacement;
        private PlanHammerPrefabConfig planHammerPrefabConfig;

        private void Awake()
        {
            logger = Logger;
            PlanPiece.logger = logger;
            PlanCrystalPrefabConfig.logger = logger;
            PlanPiecePrefabConfig.logger = logger;

            harmony.PatchAll();

            ShaderHelper.planShader = Shader.Find("Lux Lit Particles/ Bumped");

            buildModeHotkeyConfig = base.Config.Bind<string>("General", "Hammer mode toggle Hotkey", "P", new ConfigDescription("Hotkey to switch between Hammer modes", new AcceptableValueList<string>(GetAcceptableKeyCodes())));
            showAllPieces = base.Config.Bind<bool>("General", "Plan unknown pieces", false, new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            configTransparentGhostPlacement = base.Config.Bind<bool>("Visual", "Transparent Ghost Placement", false, new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)"));

            ShaderHelper.unsupportedColorConfig = base.Config.Bind<Color>("Visual", "Unsupported color", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported plan pieces"));
            ShaderHelper.supportedPlanColorConfig = base.Config.Bind<Color>("Visual", "Supported color", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported plan pieces"));

            ShaderHelper.transparencyConfig = base.Config.Bind<float>("Visual", "Transparency", 0.30f, new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f)));
            ShaderHelper.supportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.unsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.transparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;

            On.ObjectDB.CopyOtherDB += AddClonedItems;

             
            ItemManager.OnItemsRegistered += OnItemsRegistered;

            PieceManager.OnPiecesRegistered += OnPiecesRegistered;

            showAllPieces.SettingChanged += UpdateKnownRecipes;
        }

        private void OnItemsRegistered()
        {
            planHammerPrefabConfig.PrefabCreated();
            planCrystalPrefabConfig.PrefabCreated();
        }

        private void AddClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            try
            {
                PlanCrystalPrefabConfig.startPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefabConfig.prefabName + "StartEffect", "vfx_blocked");
                PlanCrystalPrefabConfig.startPlanCrystalEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                PlanCrystalPrefabConfig.stopPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefabConfig.prefabName + "StopEffect", "vfx_blocked");
                PlanCrystalPrefabConfig.stopPlanCrystalEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();
                planHammerPrefabConfig = new PlanHammerPrefabConfig();
                planCrystalPrefabConfig = new PlanCrystalPrefabConfig();

                PieceManager.Instance.AddPieceTable(PlanHammerPrefabConfig.pieceTableName);
                ItemManager.Instance.AddItem(planHammerPrefabConfig);
                ItemManager.Instance.AddItem(planCrystalPrefabConfig);
                planHammerPrefabConfig.Register();
                planCrystalPrefabConfig.Register();

            } finally
            {
                On.ObjectDB.CopyOtherDB -= AddClonedItems;
            }
            orig(self, other);
        }
          
        private void OnPiecesRegistered()
        {
            try
            { 
                logger.LogInfo("Scanning Hammer PieceTable for Pieces");
                foreach (GameObject hammerRecipe in PieceManager.Instance.GetPieceTable("Hammer").m_pieces)
                {
                    Piece piece = hammerRecipe.GetComponent<Piece>();
                    if (piece.name == "piece_repair")
                    {
                        PieceManager.Instance.GetPieceTable(PlanHammerPrefabConfig.pieceTableName).m_pieces.Add(hammerRecipe);
                        continue;
                    }

                    if (!piece.m_enabled
                        || piece.GetComponent<Ship>() != null
                        || piece.GetComponent<Plant>() != null
                        || piece.GetComponent<TerrainModifier>() != null
                        || piece.m_resources.Length == 0)
                    {
                        logger.LogInfo($"Skipping piece {piece.name}");
                        continue;
                    }
                    PlanPiecePrefabConfig prefabConfig = new PlanPiecePrefabConfig(piece);
                    PieceManager.Instance.AddPiece(prefabConfig);
                    prefabConfig.Register();
                    planPiecePrefabConfigs.Add(prefabConfig);
                } 
            } finally
            {
                PieceManager.OnPiecesRegistered -= OnPiecesRegistered;
            }
        }

        private void UpdateKnownRecipes(object sender, EventArgs e)
        {
            Player player = Player.m_localPlayer;
            if (!showAllPieces.Value)
            {
                foreach (PlanPiecePrefabConfig planPieceConfig in planPiecePrefabConfigs)
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
         
        private static readonly List<PlanPiecePrefabConfig> planPiecePrefabConfigs = new List<PlanPiecePrefabConfig>();


        private void PrefabsLoaded(object sender, EventArgs e)
        {
            hammerPrefab = PrefabManager.Instance.GetPrefab("Hammer");
            hammerPrefabItemDrop = hammerPrefab.GetComponent<ItemDrop>();

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