using BepInEx.Configuration;
using PlanBuild.Utils;
using System;
using UnityEngine;

namespace PlanBuild.Plans
{
    internal class PlanConfig
    {
        private const string GeneralSection = "General";
        internal static ConfigEntry<bool> ShowAllPieces;
        internal static ConfigEntry<float> RadiusConfig;
        internal static ConfigEntry<bool> ShowParticleEffects;

        private const string VisualSection = "Visual";
        internal static ConfigEntry<bool> ConfigTransparentGhostPlacement;
        internal static ConfigEntry<Color> UnsupportedColorConfig;
        internal static ConfigEntry<Color> SupportedPlanColorConfig;
        internal static ConfigEntry<float> TransparencyConfig;
        internal static ConfigEntry<Color> GlowColorConfig;

        internal static void Init()
        {
            // General Section

            ShowAllPieces = PlanBuildPlugin.Instance.Config.Bind(
                GeneralSection, "Plan unknown pieces", false,
                new ConfigDescription("Show all plans, even for pieces you don't know yet", null,
                    new ConfigurationManagerAttributes() { Order = 1, IsAdminOnly = true }));
            RadiusConfig = PlanBuildPlugin.Instance.Config.Bind(
                GeneralSection, "Plan totem build radius", 30f,
                new ConfigDescription("Build radius of the plan totem", null,
                    new ConfigurationManagerAttributes() { Order = 2, IsAdminOnly = true }));
            ShowParticleEffects = PlanBuildPlugin.Instance.Config.Bind(
                GeneralSection, "Plan totem particle effects", true,
                new ConfigDescription("Show particle effects when building pieces with the plan totem", null,
                    new ConfigurationManagerAttributes() { Order = 3, IsAdminOnly = true }));

            ShowAllPieces.SettingChanged += (_, _) => PlanManager.Instance.UpdateKnownRecipes();
            //RadiusConfig.SettingChanged += (_, _) => PlanManager.Instance.UpdateAllPlanTotems();  // that doesnt change the radius...

            // Visual Section

            ConfigTransparentGhostPlacement = PlanBuildPlugin.Instance.Config.Bind(
                VisualSection, "Transparent Ghost Placement", false, 
                new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)", null,
                    new ConfigurationManagerAttributes() { Order = 1 }));
            UnsupportedColorConfig = PlanBuildPlugin.Instance.Config.Bind(
                VisualSection, "Unsupported color", new Color(1f, 1f, 1f, 0.1f), 
                new ConfigDescription("Color of unsupported plan pieces", null,
                    new ConfigurationManagerAttributes() { Order = 2 }));
            SupportedPlanColorConfig = PlanBuildPlugin.Instance.Config.Bind(
                VisualSection, "Supported color", new Color(1f, 1f, 1f, 0.5f), 
                new ConfigDescription("Color of supported plan pieces", null,
                    new ConfigurationManagerAttributes() { Order = 3 }));
            TransparencyConfig = PlanBuildPlugin.Instance.Config.Bind(
                VisualSection, "Transparency", 0.30f, 
                new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes() { Order = 4 }));
            GlowColorConfig = PlanBuildPlugin.Instance.Config.Bind(
                VisualSection, "Plan totem glow color", Color.cyan, 
                new ConfigDescription("Color of the glowing lines on the Plan totem", null,
                    new ConfigurationManagerAttributes() { Order = 5 }));

            ConfigTransparentGhostPlacement.SettingChanged += UpdateGhostPlanPieceTextures;
            UnsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            SupportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            TransparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;
            GlowColorConfig.SettingChanged += UpdateAllPlanTotems;

        }

        private static void UpdateGhostPlanPieceTextures(object sender, EventArgs e)
        {
            PlanManager.Instance.UpdateAllPlanPieceTextures();
        }
        
        private static void UpdateAllPlanPieceTextures(object sender, EventArgs e)
        {
            ShaderHelper.ClearCache();
            PlanManager.Instance.UpdateAllPlanPieceTextures();
        }

        private static void UpdateAllPlanTotems(object sender, EventArgs e)
        {
            PlanManager.Instance.UpdateAllPlanTotems();
        }
    }
}
