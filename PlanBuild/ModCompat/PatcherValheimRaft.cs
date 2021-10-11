using HarmonyLib;
using PlanBuild.Blueprints.Tools;
using PlanBuild.Plans;
using UnityEngine;
using ValheimRAFT;

namespace PlanBuild.ModCompat
{
    internal class PatcherValheimRaft
    {
        private PatcherValheimRaft()
        {
        }
          
        [HarmonyPatch(typeof(PlanPiece), "CalculateSupported")]
        [HarmonyPrefix]
        private static bool PlanPiece_CalculateSupported_Prefix(PlanPiece __instance, ref bool __result)
        {
            if (__instance.GetComponentInParent<MoveableBaseRootComponent>())
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlanPiece), "OnPiecePlaced")]
        [HarmonyPrefix]
        private static void PlanPiece_OnPiecePlaced_Postfix(PlanPiece __instance, GameObject actualPiece)
        {
            MoveableBaseRootComponent moveableBaseRoot = __instance.GetComponentInParent<MoveableBaseRootComponent>();
            if (moveableBaseRoot)
            {
                moveableBaseRoot.AddNewPiece(actualPiece.GetComponent<Piece>());
            }
        }

        [HarmonyPatch(typeof(BlueprintComponent), "OnPiecePlaced")]
        [HarmonyPrefix]
        private static void BlueprintManager_OnPiecePlaced_Postfix(GameObject placedPiece)
        {
            ValheimRAFT.Patches.ValheimRAFT_Patch.PlacedPiece(Player.m_localPlayer, placedPiece);
        }
    }
}