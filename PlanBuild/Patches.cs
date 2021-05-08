using HarmonyLib;
using Jotunn.Managers;
using System;
using UnityEngine;
using static PlanBuild.PlanBuild;
using static PlanBuild.ShaderHelper;

namespace PlanBuild
{
    class Patches
    {

        [HarmonyPatch(typeof(PieceManager), "RegisterInPieceTables")]
        [HarmonyPrefix]
        static void PieceManager_RegisterInPieceTables_Prefix()
        {
            PlanBuild.Instance.ScanHammer();
        }

        [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        [HarmonyPrefix]
        static bool Player_HaveRequirements_Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (PlanBuild.showAllPieces.Value)
            {
                return true;
            }
            if (PlanPiecePrefabConfig.planToOriginalMap.TryGetValue(piece, out Piece originalPiece))
            {
                __result = __instance.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        [HarmonyPrefix]
        static void Player_SetupPlacementGhost_Prefix()
        { 
            PlanPiece.m_forceDisableInit = true;
        }

        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        [HarmonyPostfix]
        static void Player_SetupPlacementGhost_Postfix(GameObject ___m_placementGhost)
        { 
            PlanPiece.m_forceDisableInit = false;
            if (___m_placementGhost != null && configTransparentGhostPlacement.Value)
            {
                ShaderHelper.UpdateTextures(___m_placementGhost, ShaderState.Supported);
            }
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        [HarmonyPostfix]
        static void Player_Awake_Postfix()
        {
            if(Player.m_localPlayer)
            {
                PlanBuild.Instance.ScanHammer();
            }
        }

        internal static void Apply(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
            harmony.PatchAll(typeof(PlanPiece));
        }
    }
}
