using BepInEx.Configuration;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintConfig
    {
        private const string runeSection = "Blueprint Rune";
        internal static ConfigEntry<bool> allowDirectBuildConfig;
        internal static ConfigEntry<bool> invertCameraOffsetScrollConfig;
        internal static ConfigEntry<bool> invertPlacementOffsetScrollConfig;
        internal static ConfigEntry<bool> invertSelectionScrollConfig;
        internal static ConfigEntry<float> rayDistanceConfig;
        internal static ConfigEntry<float> cameraOffsetIncrementConfig;
        internal static ConfigEntry<float> placementOffsetIncrementConfig;
        internal static ConfigEntry<float> selectionIncrementConfig;
        internal static ConfigEntry<KeyCode> planSwitchConfig;

        private const string marketSection = "Blueprint Market";
        internal static ConfigEntry<bool> allowServerBlueprints;
        internal static ConfigEntry<bool> allowMarketHotkey;
        internal static ConfigEntry<KeyCode> serverGuiSwitchConfig;

        private const string directorySection = "Directories";
        internal static ConfigEntry<string> blueprintSearchDirectoryConfig;
        internal static ConfigEntry<string> blueprintSaveDirectoryConfig;

        internal static void Init()
        {
            // Rune Section

            allowDirectBuildConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Allow direct build", false,
                new ConfigDescription("Allow placement of blueprints without materials", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            invertCameraOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Invert camera offset scroll", false,
                new ConfigDescription("Invert the direction of camera offset scrolling"));

            invertPlacementOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Invert placement height change scroll", false,
                new ConfigDescription("Invert the direction of placement offset scrolling"));

            invertSelectionScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Invert selection scroll", false,
                new ConfigDescription("Invert the direction of selection scrolling"));

            rayDistanceConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Place distance", 50f,
                new ConfigDescription("Place distance while using the Blueprint Rune", new AcceptableValueRange<float>(8f, 80f)));

            cameraOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Camera offset increment", 2f,
                new ConfigDescription("Camera height change when holding Shift and scrolling while in Blueprint mode"));

            placementOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Placement offset increment", 0.1f,
                new ConfigDescription("Placement height change when holding Ctrl+Alt and scrolling while in Blueprint mode"));

            selectionIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Selection increment", 1f,
                new ConfigDescription("Selection radius increment when scrolling while in Blueprint mode"));

            planSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                runeSection, "Rune mode toggle key", KeyCode.P,
                new ConfigDescription("Hotkey to switch between rune modes"));

            // Market Scetion

            allowServerBlueprints = PlanBuildPlugin.Instance.Config.Bind(
                marketSection, "Allow serverside blueprints", false,
                new ConfigDescription("Allow sharing of blueprints on this server", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            allowMarketHotkey = PlanBuildPlugin.Instance.Config.Bind(
                marketSection, "Allow clients to use the GUI toggle key", true,
                new ConfigDescription("Allow clients to use the Hotkey to access server blueprints", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            serverGuiSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                marketSection, "Blueprint Marketplace GUI toggle key", KeyCode.End,
                new ConfigDescription("Hotkey to show blueprint marketplace GUI"));

            // Directory Section

            blueprintSearchDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind(
                directorySection, "Search directory", ".",
                new ConfigDescription("Base directory to scan (recursively) for blueprints and vbuild files, relative paths are relative to the valheim.exe location"));

            blueprintSaveDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind(
                directorySection, "Save directory", "BepInEx/config/PlanBuild/blueprints",
                new ConfigDescription("Directory to save blueprint files, relative paths are relative to the valheim.exe location"));

        }
         
    }
}
