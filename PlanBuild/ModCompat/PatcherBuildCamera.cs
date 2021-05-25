using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    class PatcherBuildCamera
    { 
        [HarmonyPatch(typeof(Valheim_Build_Camera.Valheim_Build_Camera), "IsTool")]
        [HarmonyPrefix]
        static bool ValheimBuildCamera_IsTool_Prefix(ItemDrop.ItemData itemData, ref bool __result)
        {
            if (itemData?.m_shared.m_name == BlueprintRunePrefab.BlueprintRuneName)
            {
                __result = true;
                return false;
            }
            return true;
        } 
    }
}
