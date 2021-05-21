// PlanBuild
// a Valheim mod skeleton using Jötunn
// 
// File:    PlanBuild.cs
// Project: PlanBuild

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints;
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
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(Patches.buildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.craftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.equipmentQuickSlotsGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuild : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.1.7";

        public const string PlanBuildButton = "PlanBuild_PlanBuildMode";

        public static PlanBuild Instance;

        public static ConfigEntry<string> buildModeHotkeyConfig;
        public static ConfigEntry<bool> showAllPieces;
        public static ConfigEntry<bool> configTransparentGhostPlacement;
        public static ConfigEntry<bool> configBuildShare;

        internal PlanHammerPrefab planHammerPrefab;
        internal PlanCrystalPrefab planCrystalPrefab;
        internal BlueprintRunePrefab blueprintRunePrefab;

        internal static bool showRealTextures;
        internal GameObject hammerPrefab;
        internal ItemDrop hammerPrefabItemDrop;

        public static readonly Dictionary<string, PlanPiecePrefab> planPiecePrefabs = new Dictionary<string, PlanPiecePrefab>();

        private void Awake()
        {
            Instance = this;

            // Init Blueprints
            blueprintRunePrefab = new BlueprintRunePrefab();
            BlueprintManager.Instance.Init();

            // Init Shader
            ShaderHelper.planShader = Shader.Find("Lux Lit Particles/ Bumped");

            // Harmony patching
            Patches.Apply();

            // Configs
            buildModeHotkeyConfig = base.Config.Bind<string>("General", "Hammer mode toggle Hotkey", "P", new ConfigDescription("Hotkey to switch between Hammer modes", new AcceptableValueList<string>(GetAcceptableKeyCodes())));
            showAllPieces = base.Config.Bind<bool>("General", "Plan unknown pieces", false, new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            configBuildShare = base.Config.Bind<bool>("BuildShare", "Place as planned pieces", false, new ConfigDescription("Place .vbuild as planned pieces instead", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            configTransparentGhostPlacement = base.Config.Bind<bool>("Visual", "Transparent Ghost Placement", false, new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)"));

            ShaderHelper.unsupportedColorConfig = base.Config.Bind<Color>("Visual", "Unsupported color", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported plan pieces"));
            ShaderHelper.supportedPlanColorConfig = base.Config.Bind<Color>("Visual", "Supported color", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported plan pieces"));
            ShaderHelper.transparencyConfig = base.Config.Bind<float>("Visual", "Transparency", 0.30f, new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f)));

            ShaderHelper.unsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.supportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.transparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;

            buildModeHotkeyConfig.SettingChanged += UpdateBuildKey;
            showAllPieces.SettingChanged += UpdateKnownRecipes;

            // Hooks
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;
            PrefabManager.OnPrefabsRegistered += ScanHammer;
            ItemManager.OnItemsRegistered += OnItemsRegistered;
            PieceManager.OnPiecesRegistered += LateScanHammer;

            UpdateBuildKey(null, null);
        }

        private void OnDestroy()
        {
            Patches.Remove();
        }

        private void UpdateBuildKey(object sender, EventArgs e)
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

        private void AddClonedItems()
        {
            try
            {
                PieceManager.Instance.AddPieceTable(PlanHammerPrefab.pieceTableName);
                PlanCrystalPrefab.startPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.prefabName + "StartEffect", "vfx_blocked");
                PlanCrystalPrefab.startPlanCrystalEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                PlanCrystalPrefab.stopPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.prefabName + "StopEffect", "vfx_blocked");
                PlanCrystalPrefab.stopPlanCrystalEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();

                planHammerPrefab = new PlanHammerPrefab();
                planHammerPrefab.Setup();
                ItemManager.Instance.AddItem(planHammerPrefab);

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
            planHammerPrefab.FixShader();
            planCrystalPrefab.FixShader();
            hammerPrefab = PrefabManager.Instance.GetPrefab("Hammer");
            hammerPrefabItemDrop = hammerPrefab.GetComponent<ItemDrop>();
        }

        internal bool addedHammer = false;

        internal void ScanHammer()
        {
            try
            {
                this.ScanHammer(false);
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= ScanHammer;
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
            foreach (GameObject hammerRecipe in hammerPieceTable.m_pieces)
            {
                if (hammerRecipe == null)
                {
                    Jotunn.Logger.LogWarning("null recipe in Hammer PieceTable");
                    continue;
                }
                Piece piece = hammerRecipe.GetComponent<Piece>();

                if (piece.name == "piece_repair")
                {
                    if (!addedHammer)
                    {
                        PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(PlanHammerPrefab.pieceTableName);
                        if (planHammerPieceTable != null)
                        {
                            planHammerPieceTable.m_pieces.Add(hammerRecipe);
                            addedHammer = true;
                        }
                    }
                    continue;
                }
                if (planPiecePrefabs.ContainsKey(piece.name))
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
                PlanPiecePrefab prefabConfig = new PlanPiecePrefab(piece);
                PieceManager.Instance.AddPiece(prefabConfig);
                planPiecePrefabs.Add(piece.name, prefabConfig);
                PrefabManager.Instance.RegisterToZNetScene(prefabConfig.PiecePrefab);
                if (lateAdd)
                {
                    PieceTable pieceTable = PieceManager.Instance.GetPieceTable(PlanHammerPrefab.pieceTableName);
                    if (!pieceTable.m_pieces.Contains(prefabConfig.PiecePrefab))
                    {
                        pieceTable.m_pieces.Add(prefabConfig.PiecePrefab);
                        addedPiece = true;
                    }
                }
            }
            return addedPiece;
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
            PieceManager.Instance.GetPieceTable(PlanHammerPrefab.pieceTableName)
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

        private void TogglePlanBuildMode()
        {
            if (ScanHammer(lateAdd: true))
            {
                UpdateKnownRecipes();
            }
            ReplaceHammerInInventory();
        }

        private void ReplaceHammerInInventory()
        {
            Player player = Player.m_localPlayer;
            Inventory inventory = player.GetInventory();
            ReplaceHammer(player, inventory);
        }

        internal bool ReplaceHammer(Player player, Inventory inventory)
        {
            ItemDrop.ItemData hammerItem = inventory.GetItem(hammerPrefabItemDrop.m_itemData.m_shared.m_name);
            ItemDrop.ItemData planHammerItem = inventory.GetItem(planHammerPrefab.itemData.m_shared.m_name);
            if (hammerItem == null && planHammerItem == null)
            {
                return false;
            }
            if (hammerItem != null)
            {
                Jotunn.Logger.LogInfo("Replacing Hammer with PlanHammer");
                inventory.RemoveOneItem(hammerItem);
                inventory.AddItem(
                    name: PlanHammerPrefab.planHammerName,
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
                    player.EquipItem(inventory.GetItemAt(hammerItem.m_gridPos.x, hammerItem.m_gridPos.y));
                }
            }
            else
            {
                Jotunn.Logger.LogInfo("Replacing PlanHammer with Hammer");
                inventory.RemoveOneItem(planHammerItem);
                inventory.AddItem(
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
                    player.EquipItem(inventory.GetItemAt(planHammerItem.m_gridPos.x, planHammerItem.m_gridPos.y));
                }
            }
            return true;
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
                        Jotunn.Logger.LogWarning($"Could not find directory ({assetName}).");
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
                    Jotunn.Logger.LogWarning($"Could not find asset ({assetName}).");
                    return null;
                }
            }
            return text;
        }

    }
}