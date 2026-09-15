using BepInEx.Bootstrap;
using HarmonyLib;
using PlanBuild.Plans;

namespace PlanBuild
{
    internal class Patches
    {
        public const string BuildCameraGUID = "org.gittywithexcitement.plugins.valheim.buildCamera";
        public const string CraftFromContainersGUID = "aedenthorn.CraftFromContainers";
        public const string AzuCraftyBoxesGUID = "Azumatt.AzuCraftyBoxes";
        public const string GizmoGUID = "bruce.valheim.comfymods.gizmo";
        public const string ValheimRaftGUID = "BepIn.Sarcen.ValheimRAFT";
        public const string ItemDrawersGUID = "mkz.itemdrawers";

        // A field initializer (not assignment inside Apply()) so other managers can safely call
        // Patches.Harmony.PatchAll(...) from their own Init() regardless of whether that runs
        // before or after Apply() - the CLR initializes this on first access to the type.
        internal static readonly Harmony Harmony = new Harmony(PlanBuildPlugin.PluginGUID);

        internal static void Apply()
        {
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

            if (Chainloader.PluginInfos.ContainsKey(GizmoGUID))
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