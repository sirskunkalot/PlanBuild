using EquipmentAndQuickSlots;
using HarmonyLib;

namespace PlanBuild.ModCompat
{
    class PatcherEquipmentQuickSlots
    {

        [HarmonyPatch(typeof(PlanBuildPlugin), "ReplaceHammerInInventory")]
        [HarmonyPrefix]
        static bool PlanBuild_ReplaceHammerInInventory_Prefix(PlanBuildPlugin __instance)
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
