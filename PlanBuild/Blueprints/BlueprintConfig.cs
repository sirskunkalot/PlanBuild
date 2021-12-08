using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintConfig
    {
        public enum DefaultBuildMode
        {
            Planned, Direct
        }

        public static bool DirectBuildAllowed => AllowDirectBuildConfig.Value || SynchronizationManager.Instance.PlayerIsAdmin;
        public static bool DirectBuildDefault => DefaultBuildModeConfig.Value == DefaultBuildMode.Direct;
        
        private const string RuneSection = "Blueprint Rune";
        public static ConfigEntry<bool> AllowDirectBuildConfig;
        public static ConfigEntry<DefaultBuildMode> DefaultBuildModeConfig;
        public static ConfigEntry<bool> AllowTerrainmodConfig;
        public static ConfigEntry<bool> InvertCameraOffsetScrollConfig;
        public static ConfigEntry<bool> InvertPlacementOffsetScrollConfig;
        public static ConfigEntry<bool> InvertSelectionScrollConfig;
        public static ConfigEntry<float> RayDistanceConfig;
        public static ConfigEntry<float> CameraOffsetIncrementConfig;
        public static ConfigEntry<float> PlacementOffsetIncrementConfig;
        public static ConfigEntry<float> SelectionIncrementConfig;
        public static ConfigEntry<float> SelectionConnectedMarginConfig;
        public static ConfigEntry<bool> ShowGridConfig;
        public static ConfigEntry<bool> TooltipEnabledConfig;
        public static ConfigEntry<Color> TooltipBackgroundConfig;

        private const string MarketSection = "Blueprint Market";
        public static ConfigEntry<bool> AllowServerBlueprints;
        public static ConfigEntry<bool> AllowMarketHotkey;

        private const string DirectorySection = "Directories";
        public static ConfigEntry<string> BlueprintSearchDirectoryConfig;
        public static ConfigEntry<string> BlueprintSaveDirectoryConfig;

        private const string KeybindSection = "Keybindings";
        public static ConfigEntry<KeyCode> PlanSwitchConfig;
        public static ButtonConfig PlanSwitchButton;
        public static ConfigEntry<KeyCode> MarketHotkeyConfig;
        public static ButtonConfig MarketHotkeyButton;
        public static ConfigEntry<KeyCode> CameraModifierConfig;
        public static ButtonConfig CameraModifierButton;
        public static ConfigEntry<KeyCode> RadiusModifierConfig;
        public static ButtonConfig RadiusModifierButton;
        public static ConfigEntry<KeyCode> MarkerSwitchConfig;
        public static ButtonConfig MarkerSwitchButton;
        public static ConfigEntry<KeyCode> DeleteModifierConfig;
        public static ButtonConfig DeleteModifierButton;

        public static void Init()
        {
            int order = 0;

            // Rune Section
            
            AllowDirectBuildConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Allow direct build", false,
                new ConfigDescription("Allow placement of blueprints without materials for non-admin players. Admins are always allowed to use it.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Order = ++order }));
            
            DefaultBuildModeConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Default build mode", DefaultBuildMode.Planned,
                new ConfigDescription("Default build mode when placing blueprints. If direct build is disabled, build mode is always \"Planned\".", null,
                    new ConfigurationManagerAttributes { Order = ++order }));
            
            DefaultBuildModeConfig.SettingChanged += (sender, args) =>
            {
                foreach (var bp in BlueprintManager.LocalBlueprints.Values)
                {
                    bp.CreateKeyHint();
                }
            };

            AllowTerrainmodConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Allow terrain tools", false,
                new ConfigDescription("Allow usage of the terrain modification tools for non-admin players. Admins are always allowed to use them.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Order = ++order }));
            
            RayDistanceConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Place distance", 50f,
                new ConfigDescription("Place distance while using the Blueprint Rune.",
                    new AcceptableValueRange<float>(8f, 80f),
                    new ConfigurationManagerAttributes { Order = ++order }));

            CameraOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Camera offset increment", 2f,
                new ConfigDescription("Camera height change when holding the camera modifier key and scrolling while in Blueprint mode.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            InvertCameraOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Invert camera offset scroll", false,
                new ConfigDescription("Invert the direction of camera offset scrolling.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));
            
            PlacementOffsetIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Placement offset increment", 0.1f,
                new ConfigDescription("Placement height change when holding the modifier key and scrolling while in Blueprint mode.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            InvertPlacementOffsetScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Invert placement height change scroll", false,
                new ConfigDescription("Invert the direction of placement offset scrolling.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));
            
            SelectionIncrementConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Selection increment", 1f,
                new ConfigDescription("Selection radius increment when scrolling while in Blueprint mode.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            InvertSelectionScrollConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Invert selection scroll", false,
                new ConfigDescription("Invert the direction of selection scrolling.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            SelectionConnectedMarginConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Selection connected check margin", 0.01f,
                new ConfigDescription("Distance of the shell used to check for connectedness.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            ShowGridConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Show the transform bound grid", false,
                new ConfigDescription("Shows a grid around the blueprints' bounds to visualize the blueprints' edges.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            TooltipEnabledConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Tooltip enabled", true,
                new ConfigDescription("Show a tooltip with a bigger thumbnail for blueprint pieces.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            TooltipBackgroundConfig = PlanBuildPlugin.Instance.Config.Bind(
                RuneSection, "Tooltip Color", new Color(0.13f, 0.13f, 0.13f, 0.65f),
                new ConfigDescription("Set the background color for the tooltip on blueprint pieces.", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            // Market Section

            AllowServerBlueprints = PlanBuildPlugin.Instance.Config.Bind(
                MarketSection, "Allow serverside blueprints", false,
                new ConfigDescription("Allow sharing of blueprints on this server", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Order = ++order }));

            AllowMarketHotkey = PlanBuildPlugin.Instance.Config.Bind(
                MarketSection, "Allow clients to use the GUI toggle key", true,
                new ConfigDescription("Allow clients to use the Hotkey to access server blueprints", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Order = ++order }));

            // Directory Section

            BlueprintSearchDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind(
                DirectorySection, "Search directory", ".",
                new ConfigDescription("Base directory to scan (recursively) for blueprints and vbuild files, relative paths are relative to the valheim.exe location", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            BlueprintSaveDirectoryConfig = PlanBuildPlugin.Instance.Config.Bind(
                DirectorySection, "Save directory", "BepInEx/config/PlanBuild/blueprints",
                new ConfigDescription("Directory to save blueprint files, relative paths are relative to the valheim.exe location", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            // Keybind Section
            
            PlanSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "Rune mode toggle key", KeyCode.P,
                new ConfigDescription("Hotkey to switch between rune modes", null,
                    new ConfigurationManagerAttributes { Order = ++order }));
            
            MarketHotkeyConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "Blueprint Marketplace GUI toggle key", KeyCode.End,
                new ConfigDescription("Hotkey to show blueprint marketplace GUI", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            CameraModifierConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "CameraModifier", KeyCode.LeftShift,
                new ConfigDescription("Modifier key to modify camera behavior on various tools", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            RadiusModifierConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "RadiusModifier", KeyCode.LeftControl,
                new ConfigDescription("Modifier key to use radius based selection on various tools", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            DeleteModifierConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "DeleteModifier", KeyCode.LeftAlt,
                new ConfigDescription("Modifier key for removal operations on various tools", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

            MarkerSwitchConfig = PlanBuildPlugin.Instance.Config.Bind(
                KeybindSection, "MarkerSwitch", KeyCode.Q,
                new ConfigDescription("Key to switch between marker shapes on various tools", null,
                    new ConfigurationManagerAttributes { Order = ++order }));

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

            MarketHotkeyButton = new ButtonConfig
            {
                Name = "GUIToggle",
                Config = MarketHotkeyConfig,
                ActiveInCustomGUI = true
            };
            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, MarketHotkeyButton);

            // Shared

            CameraModifierButton = new ButtonConfig
            {
                Name = nameof(CameraModifierButton),
                Config = CameraModifierConfig
            };

            InputManager.Instance.AddButton(PlanBuildPlugin.PluginGUID, CameraModifierButton);

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

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_blueprint_mode" },
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" }
                }
            });

            // Capture

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceCaptureName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcapture" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, Config = RadiusModifierConfig, HintToken = "$hud_bpcapture_highlight" },
                    new ButtonConfig { Name = CameraModifierButton.Name, Config = CameraModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Add selection

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSelectAddName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_add" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, Config = DeleteModifierConfig, HintToken = "$hud_blueprint_select_add_connected" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, Config = RadiusModifierConfig, HintToken = "$hud_blueprint_select_add_radius" },
                    new ButtonConfig { Name = CameraModifierButton.Name, Config = CameraModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Remove selection

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSelectRemoveName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_remove" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, Config = DeleteModifierConfig, HintToken = "$hud_blueprint_select_remove_connected" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, Config = RadiusModifierConfig, HintToken = "$hud_blueprint_select_remove_radius" },
                    new ButtonConfig { Name = $"{DeleteModifierButton.Key} + {RadiusModifierButton.Key}", HintToken = "$hud_blueprint_select_remove_clear" },
                    new ButtonConfig { Name = CameraModifierButton.Name, Config = CameraModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Save selection

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSelectSaveName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_save" }
                }
            });

            // Snap Point

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceSnapPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpsnappoint" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" },
                }
            });

            // Center point

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceCenterPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcenterpoint" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" },
                }
            });

            // Remove

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceDeletePlansName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpdelete" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, Config = RadiusModifierConfig, HintToken = "$hud_bpdelete_radius" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, Config = DeleteModifierConfig, HintToken = "$hud_bpdelete_all" },
                    new ButtonConfig { Name = CameraModifierButton.Name, Config = CameraModifierConfig, HintToken = "$hud_bpcamera" }
                }
            });

            // Terrain

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceTerrainName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpterrain_flatten" },
                    new ButtonConfig { Name = MarkerSwitchButton.Name, Config = MarkerSwitchConfig, HintToken = "$hud_bpterrain_marker" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, Config = DeleteModifierConfig, HintToken = "$hud_bpterrain_delete" },
                    new ButtonConfig { Name = CameraModifierButton.Name, Config = CameraModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpterrainradius" }
                }
            });

            // Delete

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PieceDeleteObjectsName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpobjects_deleteveg" },
                    new ButtonConfig { Name = RadiusModifierButton.Name, Config = RadiusModifierConfig, HintToken = "$hud_bpobjects_deletepieces" },
                    new ButtonConfig { Name = DeleteModifierButton.Name, Config = DeleteModifierConfig, HintToken = "$hud_bpobjects_deleteall" },
                    new ButtonConfig { Name = CameraModifierButton.Name, Config = CameraModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Paint

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = BlueprintAssets.PiecePaintName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = PlanSwitchButton.Name, Config = PlanSwitchConfig, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bppaint_reset" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_bppaint_dirt" },
                    new ButtonConfig { Name = "Alt", HintToken = "$hud_bppaint_paved" },
                    new ButtonConfig { Name = CameraModifierButton.Name, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            GUIManager.OnCustomGUIAvailable -= CreateCustomKeyHints;
        }
    }
}