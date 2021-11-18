// PlanBuild
// a Valheim mod using Jötunn
//
// File:    PlanBuildPlugin.cs
// Project: PlanBuild

using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Marketplace;
using PlanBuild.Plans;
using System.Reflection;
using UnityEngine;
using ShaderHelper = PlanBuild.Utils.ShaderHelper;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.4.0")]
    [BepInDependency(Patches.BuildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.CraftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.GizmoGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.ValheimRaftGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.ItemDrawersGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.9.0";

        public static PlanBuildPlugin Instance;

        internal PlanCrystalPrefab PlanCrystalPrefab;
        internal PlanTotemPrefab PlanTotemPrefab;
        internal BlueprintAssets BlueprintRuneAssets;

        public void Awake()
        {
            Instance = this;
            Assembly assembly = typeof(PlanBuildPlugin).Assembly;

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
        }

        public void OnDestroy()
        {
            Patches.Remove();
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
                (BlueprintConfig.AllowMarketHotkey.Value || SynchronizationManager.Instance.PlayerIsAdmin) &&
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