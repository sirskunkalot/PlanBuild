using EquipmentAndQuickSlots;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanBuild
{
    class PatcherEquipmentQuickSlots
    {

        [HarmonyPatch(typeof(PlanBuild), "ReplaceHammerInInventory")]
        [HarmonyPrefix]
        static bool PlanBuild_ReplaceHammerInInventory_Prefix(PlanBuild __instance)
        {
            Player player = Player.m_localPlayer;
            ExtendedInventory extendedInventory = player.GetInventory() as ExtendedInventory;
            extendedInventory.CallBase = true;
            try
            {
                foreach(Inventory inventory in extendedInventory._inventories)
                { 
                    if (__instance.ReplaceHammer(player, inventory))
                    {
                        return false;
                    }
                }
            }
            finally
            {
                extendedInventory.CallBase = false;
            }

            return false;
        }
         
    }
}
