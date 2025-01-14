﻿using BepInEx.Bootstrap;
using HarmonyLib;
using PlanBuild.Plans;

namespace PlanBuild
{
    internal class Patches
    {
        public const string BuildCameraGUID = "org.gittywithexcitement.plugins.valheim.buildCamera";
        public const string CraftFromContainersGUID = "aedenthorn.CraftFromContainers";
        public const string AzuCraftyBoxesGUID = "Azumatt.AzuCraftyBoxes";
        public const string ValheimRaftGUID = "BepIn.Sarcen.ValheimRAFT";
        public const string ItemDrawersGUID = "mkz.itemdrawers";

        private static Harmony Harmony;

        internal static void Apply()
        {
            Harmony = new Harmony(PlanBuildPlugin.PluginGUID);
            Harmony.PatchAll(typeof(PlanPiece));

            if (Chainloader.PluginInfos.ContainsKey(BuildCameraGUID))
            {
                Jotunn.Logger.LogInfo("Applying BuildCamera patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherBuildCamera));
            }

            if (Chainloader.PluginInfos.ContainsKey(CraftFromContainersGUID))
            {
                Jotunn.Logger.LogInfo("Applying CraftFromContainers patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherCraftFromContainers));
            }
            else if (Chainloader.PluginInfos.ContainsKey(AzuCraftyBoxesGUID))
            {
                Jotunn.Logger.LogInfo("Applying AzuCraftyBoxes patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherAzuCraftyBoxes));
            }

            if (ModCompat.PatcherGizmo.ComfyGizmoInstalled)
            {
                Jotunn.Logger.LogInfo("Applying Gizmo patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherGizmo));
            }

            if (Chainloader.PluginInfos.ContainsKey(ValheimRaftGUID))
            {
                Jotunn.Logger.LogInfo("Applying ValheimRAFT patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherValheimRaft));
            }
        }
    }
}