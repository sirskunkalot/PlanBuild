using HarmonyLib;
using PlanBuild.Plans;
using System.Collections.Generic;

using CraftyContainers = CraftFromContainers.CraftFromContainers;

namespace PlanBuild.ModCompat
{
    internal class PatcherCraftFromContainers
    {
        [HarmonyPatch(typeof(PlanPiece), nameof(PlanPiece.GetInventories))]
        [HarmonyPostfix]
        private static void PlanPiece_GetInventories_Postfix(
            Humanoid player,
            ref List<PlanPiece.IInventory> __result
        )
        {
            // Check if the plugin is active, and append any nearby container inventories to the list
            if (CraftyContainers.modEnabled.Value) {
                List<Container> containers = CraftyContainers.GetNearbyContainers(
                    player.transform.position
                );
                foreach (Container container in containers) {
                    __result.Add(new PlanPiece.StandardInventory(container.GetInventory()));
                }
            }
        }
    }
}