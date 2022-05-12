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
    [BepInDependency(Jotunn.Main.ModGuid, "2.6.6")]
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
        public const string PluginVersion = "0.9.5";

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
            BlueprintManager.Instance.Init();

            // Init Plans
            AssetBundle planbuildBundle = AssetUtils.LoadAssetBundleFromResources("planbuild", assembly);
            PlanTotemPrefab.Create(planbuildBundle);
            PlanCrystalPrefab.Create(planbuildBundle);
            PlanHammerPrefab.Create(planbuildBundle);
            planbuildBundle.Unload(false);
            PlanManager.Instance.Init();

            // Init Shader
            ShaderHelper.PlanShader = Shader.Find("Lux Lit Particles/ Bumped");

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
    }
}