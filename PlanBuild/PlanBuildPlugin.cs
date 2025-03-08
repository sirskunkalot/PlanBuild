// PlanBuild
// a Valheim mod using Jötunn
//
// File:    PlanBuildPlugin.cs
// Project: PlanBuild

using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints;
using PlanBuild.Plans;
using System.Reflection;
using UnityEngine;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.24.2")]
    [BepInDependency(Patches.BuildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.CraftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.AzuCraftyBoxesGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.GizmoGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.ValheimRaftGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.ItemDrawersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.18.2";

        public static PlanBuildPlugin Instance;

        public void Awake()
        {
            Instance = this;
            Assembly assembly = typeof(PlanBuildPlugin).Assembly;

            // Init config
            PlanBuild.Config.Init();

            // Init Blueprints
            AssetBundle blueprintsBundle = AssetUtils.LoadAssetBundleFromResources("blueprints", assembly);
            BlueprintAssets.Load(blueprintsBundle);
            blueprintsBundle.Unload(false);
            BlueprintManager.Init();

            // Init Plans
            AssetBundle planbuildBundle = AssetUtils.LoadAssetBundleFromResources("planbuild", assembly);
            PlanTotemPrefab.Create(planbuildBundle);
            PlanCrystalPrefab.Create(planbuildBundle);
            PlanHammerPrefab.Create(planbuildBundle);
            planbuildBundle.Unload(false);
            PlanManager.Init();
            
            // Harmony patching
            Patches.Apply();
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
                !SelectionSaveGUI.IsVisible() && !TerrainModGUI.IsVisible() && !SelectionGUI.IsVisible() &&
                (PlanBuild.Config.AllowMarketHotkey.Value || SynchronizationManager.Instance.PlayerIsAdmin) &&
                ZInput.GetButtonDown(PlanBuild.Config.MarketHotkeyButton.Name))
            {
                BlueprintGUI.Instance.Toggle();
            }

            // Return from world interface GUI again
            if (BlueprintGUI.IsVisible() && !BlueprintGUI.TextFieldHasFocus() && ZInput.GetButtonDown("Use"))
            {
                BlueprintGUI.Instance.Toggle(shutWindow: true);
                ZInput.ResetButtonStatus("Use");
            }
        }

        /// <summary>
        ///     Public API method so mods that add/remove pieces in-game can 
        ///     trigger PlanBuild to update the PlanHammer piece table. 
        /// </summary>
        public void UpdateScanPieces()
        {
            PlanDB.Instance.ScanPieceTables();
        }
    }
}