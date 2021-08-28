// PlanBuild
// a Valheim mod using Jötunn
//
// File:    PlanBuildPlugin.cs
// Project: PlanBuild

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Marketplace;
using PlanBuild.Plans;
using PlanBuild.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.2.4")]
    [BepInDependency(Patches.BuildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.CraftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.GizmoGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.ValheimRaftGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.5.0";

        public static PlanBuildPlugin Instance;
        public static ConfigEntry<bool> ConfigTransparentGhostPlacement;

        internal PlanCrystalPrefab PlanCrystalPrefab;
        internal BlueprintAssets BlueprintRuneAssets;
        private PlanTotemPrefab PlanTotemPrefab;

        internal static bool ShowRealTextures;

        public void Awake()
        {
            Instance = this;
            Assembly assembly = typeof(PlanBuildPlugin).Assembly;

            // Configs
            SetupConfig();

            // Init Plans
            AssetBundle planbuildBundle = AssetUtils.LoadAssetBundleFromResources("planbuild", assembly);
            PlanTotemPrefab = new PlanTotemPrefab(planbuildBundle);
            PlanCrystalPrefab = new PlanCrystalPrefab(planbuildBundle);
            planbuildBundle.Unload(false);
            PlanManager.Instance.Init();

            // Init Blueprints
            AssetBundle blueprintsBundle = AssetUtils.LoadAssetBundleFromResources("blueprints", assembly);
            BlueprintRuneAssets = new BlueprintAssets(blueprintsBundle);
            blueprintsBundle.Unload(false);
            BlueprintManager.Instance.Init();

            // Init Shader
            ShaderHelper.PlanShader = Shader.Find("Lux Lit Particles/ Bumped");

            // Harmony patching
            Patches.Apply();

            // Hooks
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;
            ItemManager.OnItemsRegistered += OnItemsRegistered;
        }

        private void OnItemsRegistered()
        {
            PlanCrystalPrefab.FixShader();
        }

        private void SetupConfig()
        {
            PlanTotem.radiusConfig = Config.Bind("General", "Plan totem build radius", 30f, new ConfigDescription("Build radius of the Plan totem", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));
            PlanTotem.showParticleEffects = Config.Bind("General", "Plan totem particle effects", true, new ConfigDescription("Build radius of the Plan totem"));

            PlanTotem.radiusConfig.SettingChanged += UpdatePlanTotem;

            ConfigTransparentGhostPlacement = Config.Bind("Visual", "Transparent Ghost Placement", false, new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)"));
            ShaderHelper.UnsupportedColorConfig = Config.Bind("Visual", "Unsupported color", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported plan pieces"));
            ShaderHelper.SupportedPlanColorConfig = Config.Bind("Visual", "Supported color", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported plan pieces"));
            ShaderHelper.TransparencyConfig = Config.Bind("Visual", "Transparency", 0.30f, new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f)));
            PlanTotemPrefab.GlowColorConfig = Config.Bind("Visual", "Plan totem glow color", Color.cyan, new ConfigDescription("Color of the glowing lines on the Plan totem"));

            ShaderHelper.UnsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.SupportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.TransparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;
            PlanTotemPrefab.GlowColorConfig.SettingChanged += UpdatePlanTotem;
        }

        public void Update()
        {
            // No keys without ZInput
            if (ZInput.instance == null)
            {
                return;
            }

            // Never in the Settings dialogue
            if (Settings.instance && Settings.instance.isActiveAndEnabled)
            {
                return;
            }

            // BP Market GUI is OK in the main menu
            if (BlueprintGUI.IsAvailable() &&
                BlueprintConfig.AllowMarketHotkey.Value &&
                ZInput.GetButtonDown(BlueprintConfig.GUIToggleButton.Name))
            {
                BlueprintGUI.Instance.Toggle();
            }

            // Return from world interface GUI again
            if (BlueprintGUI.IsVisible() && !BlueprintGUI.TextFieldHasFocus() && ZInput.GetButtonDown("Use"))
            {
                BlueprintGUI.Instance.Toggle(shutWindow: true);
                ZInput.ResetButtonStatus("Use");
                return;
            }

            // Not in game menus
            if (!CheckInput())
            {
                return;
            }

            // Rune mode toogle
            if (ZInput.GetButtonDown(BlueprintConfig.PlanSwitchButton.Name))
            {
                TogglePlanBuildMode();
            }
        }
        
        public void TogglePlanBuildMode()
        {
            if (Player.m_localPlayer.m_visEquipment.m_rightItem != BlueprintAssets.BlueprintRuneName)
            {
                return;
            }
            ItemDrop.ItemData blueprintRune = Player.m_localPlayer.GetInventory().GetItem(BlueprintAssets.BlueprintRuneItemName);
            if (blueprintRune == null)
            {
                return;
            }
            PieceTable planPieceTable = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PieceTableName);
            PieceTable blueprintPieceTable = PieceManager.Instance.GetPieceTable(BlueprintAssets.PieceTableName);
            if (blueprintRune.m_shared.m_buildPieces == planPieceTable)
            {
                blueprintRune.m_shared.m_buildPieces = blueprintPieceTable;
            }
            else
            {
                blueprintRune.m_shared.m_buildPieces = planPieceTable;
            }
            Player.m_localPlayer.UnequipItem(blueprintRune);
            Player.m_localPlayer.EquipItem(blueprintRune);

            Color color = blueprintRune.m_shared.m_buildPieces == planPieceTable ? Color.red : Color.cyan;
            ShaderHelper.SetEmissionColor(Player.m_localPlayer.m_visEquipment.m_rightItemInstance, color);

            Player.m_localPlayer.UpdateKnownRecipesList();
        }

        private void UpdatePlanTotem(object sender, EventArgs e)
        {
            PlanTotemPrefab.SettingsUpdated();
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
                PlanCrystalPrefab.StartPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.PrefabName + "StartEffect", "vfx_blocked");
                PlanCrystalPrefab.StartPlanCrystalEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                PlanCrystalPrefab.StopPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.PrefabName + "StopEffect", "vfx_blocked");
                PlanCrystalPrefab.StopPlanCrystalEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();

                ItemManager.Instance.AddItem(PlanCrystalPrefab.Create());
            }
            finally
            {
                ItemManager.OnVanillaItemsAvailable -= AddClonedItems;
            }
        }

        private void UpdateAllPlanPieceTextures(object sender, EventArgs e)
        {
            ShaderHelper.ClearCache();
            UpdateAllPlanPieceTextures();
        }

        public static void UpdateAllPlanPieceTextures()
        {
            if (ShowRealTextures
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
            return Player.m_localPlayer != null
                && (!Chat.instance || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !InventoryGui.IsVisible()
                && !StoreGui.IsVisible()
                && !Menu.IsVisible()
                && !Minimap.IsOpen()
                && !Player.m_localPlayer.InCutscene();
        }
    }
}