using HarmonyLib;
using System; 

namespace PlanBuild
{
    class PatcherCraftFromContainers
    { 
        [HarmonyPatch(typeof(PlanPiece), "PlayerHaveResource")]
        [HarmonyPostfix]
        static void PlanPiece_PlayerHaveResource_Postfix(Humanoid player, string resourceName, ref bool __result)
        {
            if(!CraftFromContainers.BepInExPlugin.modEnabled.Value)
            {
                return;
            }
            if(__result == false)
            {
                foreach(Container container in CraftFromContainers.BepInExPlugin.GetNearbyContainers(player.transform.position))
                {
                    if(container.GetInventory().HaveItem(resourceName))
                    {
                        __result = true;
                        return;
                    }   
                }
            }
        }

        [HarmonyPatch(typeof(PlanPiece), "PlayerGetResourceCount")]
        [HarmonyPostfix]
        static void PlanPiece_PlayerGetResourceCount_Postfix(Humanoid player, string resourceName, ref int __result)
        {
            if (!CraftFromContainers.BepInExPlugin.modEnabled.Value)
            {
                return;
            }
            
            foreach (Container container in CraftFromContainers.BepInExPlugin.GetNearbyContainers(player.transform.position))
            {
                __result += container.GetInventory().CountItems(resourceName);
            }
        }

        [HarmonyPatch(typeof(PlanPiece), "PlayerRemoveResource")]
        [HarmonyPrefix]
        static bool PlanPiece_PlayerRemoveResource_Prefix(Humanoid player, string resourceName, int amount)
        {
            if (!CraftFromContainers.BepInExPlugin.modEnabled.Value)
            {
                return true;
            }
            int playerResourceCount = player.GetInventory().CountItems(resourceName);
            int amountToRemove = Math.Min(amount, playerResourceCount);
            player.GetInventory().RemoveItem(resourceName, amountToRemove);
            int remaining = amount - amountToRemove;
            if(remaining > 0)
            {
                foreach (Container container in CraftFromContainers.BepInExPlugin.GetNearbyContainers(player.transform.position))
                {
                    int containerResourceCount = container.GetInventory().CountItems(resourceName);
                    amountToRemove = Math.Min(remaining, containerResourceCount);
                    container.GetInventory().RemoveItem(resourceName, amountToRemove);
                    remaining -= amountToRemove;
                    if(remaining < 0)
                    {
                        break;
                    }
                }
            }
            return false;
        }

    }
}
