using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintConfig
    {
        internal static ConfigEntry<float> rayDistanceConfig;
        internal static ConfigEntry<float> cameraOffsetIncrementConfig;
        internal static ConfigEntry<float> placementOffsetIncrementConfig;
        internal static ConfigEntry<float> selectionIncrementConfig;
        internal static ConfigEntry<bool> allowDirectBuildConfig;
        internal static ConfigEntry<bool> invertCameraOffsetScrollConfig;
        internal static ConfigEntry<bool> invertPlacementOffsetScrollConfig;
        internal static ConfigEntry<bool> invertSelectionScrollConfig;
        internal static ConfigEntry<KeyCode> planSwitchConfig;
        internal static ConfigEntry<string> blueprintSearchDirectoryConfig;
        internal static ConfigEntry<string> blueprintSaveDirectoryConfig;

        internal static void Init()
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

        }
    }
}
