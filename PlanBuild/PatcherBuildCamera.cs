using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanBuild
{
    class PatcherBuildCamera
    { 
        [HarmonyPatch(typeof(Valheim_Build_Camera.Valheim_Build_Camera), "IsTool")]
        [HarmonyPrefix]
        static bool ValheimBuildCamera_IsTool_Prefix(ItemDrop.ItemData itemData, ref bool __result)
        {
            if (itemData?.m_shared.m_name == PlanHammerPrefabConfig.itemName)
            {
                __result = true;
                return false;
            }
            return true;
        } 
    }
}
