using HarmonyLib;
using PlanBuild.Blueprints.Components;
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
          
        [HarmonyPatch(typeof(PlanPiece), nameof(PlanPiece.CalculateSupported))]
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

        [HarmonyPatch(typeof(PlanPiece), nameof(PlanPiece.OnPieceReplaced))]
        [HarmonyPrefix]
        private static void PlanPiece_OnPieceReplaced_Postfix(GameObject originatingPiece, GameObject placedPiece)
        {
            MoveableBaseRootComponent moveableBaseRoot = originatingPiece.GetComponentInParent<MoveableBaseRootComponent>();
            if (moveableBaseRoot)
            {
                moveableBaseRoot.AddNewPiece(placedPiece.GetComponent<Piece>());
            }
        }

        [HarmonyPatch(typeof(PlacementComponent), nameof(PlacementComponent.OnPiecePlaced))]
        [HarmonyPrefix]
        private static void BlueprintManager_OnPiecePlaced_Postfix(GameObject placedPiece)
        {
            ValheimRAFT.Patches.ValheimRAFT_Patch.PlacedPiece(Player.m_localPlayer, placedPiece);
        }
    }
}