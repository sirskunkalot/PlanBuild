using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    internal class PatcherBuildCamera
    {
        internal static bool UpdateCamera = true;

        [HarmonyPatch(typeof(Valheim_Build_Camera.Valheim_Build_Camera), "IsTool")]
        [HarmonyPrefix]
        private static bool ValheimBuildCamera_IsTool_Prefix(ItemDrop.ItemData itemData, ref bool __result)
        {
            if (itemData?.m_shared.m_name == BlueprintAssets.BlueprintRuneItemName ||
                itemData?.m_shared.m_name == PlanHammerPrefab.PlanHammerItemName)
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Valheim_Build_Camera.Valheim_Build_Camera), "EnableBuildMode")]
        [HarmonyPostfix]
        internal static void ValheimBuildCamera_EnableBuildMode_Postfix()
        {
            UpdateCamera = false;
        }

        [HarmonyPatch(typeof(Valheim_Build_Camera.Valheim_Build_Camera), "DisableBuildMode")]
        [HarmonyPostfix]
        internal static void ValheimBuildCamera_DisableBuildMode_Postfix()
        {
            UpdateCamera = true;
        }
    }
}