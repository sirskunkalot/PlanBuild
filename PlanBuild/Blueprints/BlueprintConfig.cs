using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintConfig
    {
        internal static ButtonConfig PlanSwitchButton;
        internal static ButtonConfig GUIToggleButton;

        private const string RuneSection = "Blueprint Rune";
        internal static ConfigEntry<bool> AllowDirectBuildConfig;
        internal static ConfigEntry<bool> AllowTerrainmodConfig;
        internal static ConfigEntry<bool> InvertCameraOffsetScrollConfig;
        internal static ConfigEntry<bool> InvertPlacementOffsetScrollConfig;
        internal static ConfigEntry<bool> InvertSelectionScrollConfig;
        internal static ConfigEntry<float> RayDistanceConfig;
        internal static ConfigEntry<float> CameraOffsetIncrementConfig;
        internal static ConfigEntry<float> PlacementOffsetIncrementConfig;
        internal static ConfigEntry<float> SelectionIncrementConfig;
        internal static ConfigEntry<KeyCode> PlanSwitchConfig;
        internal static ConfigEntry<bool> ShowGridConfig;

        private const string MarketSection = "Blueprint Market";
        internal static ConfigEntry<bool> AllowServerBlueprints;
        internal static ConfigEntry<bool> AllowMarketHotkey;
        internal static ConfigEntry<KeyCode> ServerGuiSwitchConfig;

        private const string DirectorySection = "Directories";
        internal static ConfigEntry<string> BlueprintSearchDirectoryConfig;
        internal static ConfigEntry<string> BlueprintSaveDirectoryConfig;

        private const string KeybindSection = "Keybindings";
        internal static ConfigEntry<KeyCode> RadiusModifierConfig;
        internal static ButtonConfig RadiusModifierButton;
        internal static ConfigEntry<KeyCode> MarkerSwitchConfig;
        internal static ButtonConfig MarkerSwitchButton;
        internal static ConfigEntry<KeyCode> DeleteModifierConfig;
        internal static ButtonConfig DeleteModifierButton;
         
        internal static void Init()
        {
            // Rune Section

            AllowDirectBuildConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Allow direct build", false,
                new ConfigDescription("Allow placement of blueprints without materials", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            AllowTerrainmodConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Allow terrain tools", false,
                new ConfigDescription("Allow usage of the terrain modification tools", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            InvertCameraOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Invert camera offset scroll", false,
                new ConfigDescription("Invert the direction of camera offset scrolling"));

            InvertPlacementOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Invert placement height change scroll", false,
                new ConfigDescription("Invert the direction of placement offset scrolling"));

            InvertSelectionScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Invert selection scroll", false,
                new ConfigDescription("Invert the direction of selection scrolling"));

            RayDistanceConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Place distance", 50f,
                new ConfigDescription("Place distance while using the Blueprint Rune",
                    new AcceptableValueRange<float>(8f, 80f)));

            CameraOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Camera offset increment", 2f,
                new ConfigDescription("Camera height change when holding Shift and scrolling while in Blueprint mode"));

            PlacementOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Placement offset increment", 0.1f,
                new ConfigDescription("Placement height change when holding Ctrl+Alt and scrolling while in Blueprint mode"));

            SelectionIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Selection increment", 1f,
                new ConfigDescription("Selection radius increment when scrolling while in Blueprint mode"));

            PlanSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Rune mode toggle key", KeyCode.P,
                new ConfigDescription("Hotkey to switch between rune modes"));

            ShowGridConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Show the transform bound grid", false,
                new ConfigDescription("Show a grid around the blueprints' bounds"));

            // Market Scetion

            AllowServerBlueprints = PlanBuildPlugin.Instance.Config.Bind(
                MarketSection, "Allow serverside blueprints", false,
                new ConfigDescription("Allow sharing of blueprints on this server", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            AllowMarketHotkey = PlanBuildPlugin.Instance.Config.Bind(
                MarketSection, "Allow clients to use the GUI toggle key", true,
                new ConfigDescription("Allow clients to use the Hotkey to access server blueprints", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ServerGuiSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                MarketSection, "Blueprint Marketplace GUI toggle key", KeyCode.End,
                new ConfigDescription("Hotkey to show blueprint marketplace GUI"));

            // Directory Section

            BlueprintSearchDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind(
                DirectorySection, "Search directory", ".",
                new ConfigDescription("Base directory to scan (recursively) for blueprints and vbuild files, relative paths are relative to the valheim.exe location"));

            BlueprintSaveDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind(
                DirectorySection, "Save directory", "BepInEx/config/PlanBuild/blueprints",
                new ConfigDescription("Directory to save blueprint files, relative paths are relative to the valheim.exe location"));

            // Keybind section

            RadiusModifierConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "RadiusModifier", KeyCode.LeftControl,
                new ConfigDescription("Modifier key to use radius based selection on various tools", null,
                    new ConfigurationManagerAttributes{Order = 1}));

            DeleteModifierConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "DeleteModifier", KeyCode.LeftAlt,
                new ConfigDescription("Modifier key for removal operations on various tools", null,
                    new ConfigurationManagerAttributes{Order = 2}));

            MarkerSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "MarkerSwitch", KeyCode.Q,
                new ConfigDescription("Key to switch between marker shapes on various tools", null,
                    new ConfigurationManagerAttributes{Order = 3}));

            // Create Buttons and KeyHints if and when PixelFix is created
            GUIManager.OnCustomGUIAvailable += CreateCustomKeyHints;
        }

        /// <summary>
        ///     Create custom KeyHints for the static Blueprint Rune pieces
        /// </summary>
        private static void CreateCustomKeyHints()
        {
            // Global

            PlanSwitchButton = new ButtonConfig
            {
                Name = "RuneModeToggle",
                Config = PlanSwitchConfig,
                HintToken = "$hud_bp_toggle_plan_mode"
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, PlanSwitchButton);

            GUIToggleButton = new ButtonConfig
            {
                Name = "GUIToggle",
                Config = ServerGuiSwitchConfig,
                ActiveInCustomGUI = true
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, GUIToggleButton);
            
            // Shared

            RadiusModifierButton = new ButtonConfig
            {
                Name = nameof(RadiusModifierButton),
                Config = RadiusModifierConfig
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, RadiusModifierButton);
            
            DeleteModifierButton = new ButtonConfig
            {
                Name = nameof(DeleteModifierButton),
                Config = DeleteModifierConfig
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, DeleteModifierButton);

            MarkerSwitchButton = new ButtonConfig
            {
                Name = nameof(MarkerSwitchButton),
                Config = MarkerSwitchConfig
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, MarkerSwitchButton);

            // Mode Switch

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_blueprint_mode" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" }
                }
            });
            
            // Capture
            
            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceCaptureName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcapture" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, HintToken = "$hud_bpcapture_highlight" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Snap Point

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSelectAddName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_add" },
                    //new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_blueprint_select_add_connected" },
                    new ButtonConfig { Name = "Shift", HintToken = "$hud_blueprint_select_add_radius" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSelectAddName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_remove" },
                    //new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_blueprint_select_remove_connected" },
                    new ButtonConfig { Name = "Shift", HintToken = "$hud_blueprint_select_remove_radius" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSelectSaveName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_save" },
                    //new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" }, 
                }
            });

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSnapPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpsnappoint" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" },
                }
            });

            // Center point

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceCenterPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcenterpoint" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" },
                }
            });

            // Remove
            
            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceDeletePlansName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpdelete" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, HintToken = "$hud_bpdelete_radius" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, HintToken = "$hud_bpdelete_all" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Terrain

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceTerrainName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpterrain_flatten" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, HintToken = "$hud_bpterrain_delete" },
                    new ButtonConfig { Name = MarkerSwitchButton.Name, HintToken = "$hud_bpterrain_marker" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpterrainradius" }
                }
            });

            // Delete

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceDeleteObjectsName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpobjects_deleteveg" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, HintToken = "$hud_bpobjects_deleteall" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpobjectsradius" }
                }
            });

            // Paint

            GUIManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PiecePaintName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bppaint_reset" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_bppaint_dirt" },
                    new ButtonConfig { Name = "Alt", HintToken = "$hud_bppaint_paved" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bppaintradius" }
                }
            });

            GUIManager.OnCustomGUIAvailable -= CreateCustomKeyHints;
        }
    }
}
