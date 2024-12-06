using HarmonyLib;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;

namespace PlanBuild.ModCompat
{
    internal class PatcherCraftFromContainers
    {
        [HarmonyPatch(typeof(PlanPiece), "GetInventories")]
        [HarmonyPostfix]
        private static void PlanPiece_GetInventories_Postfix(Humanoid player, ref List<PlanPiece.IInventory> __result)
        {
            // Check if the plugin is active, and append any nearby container inventories to the list
            if (CraftFromContainers.BepInExPlugin.modEnabled.Value) {
                foreach (Container container in CraftFromContainers.BepInExPlugin.GetNearbyContainers(player.transform.position)) {
                    __result.Add(new PlanPiece.StandardInventory(container.GetInventory()));
                }
            }
        }
    }
}