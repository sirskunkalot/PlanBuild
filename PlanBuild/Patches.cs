using HarmonyLib;
using System; 
using UnityEngine;
using static PlanBuild.PlanBuild;
using static PlanBuild.ShaderHelper;

namespace PlanBuild
{
    class Patches
    {
        [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        class Player_HaveRequirements_Patch
        {

            static bool Prefix(Player __instance, Piece piece, ref bool __result)
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

        }

        [HarmonyPatch(typeof(ZInput), "Load")]
        class ZInput_Load_Patch
        {
            static void Postfix(ZInput __instance)
            {
                if (Enum.TryParse(buildModeHotkeyConfig.Value, out KeyCode keyCode))
                {
                    __instance.m_buttons.Remove(PlanBuildButton);
                    __instance.AddButton(PlanBuildButton, keyCode);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        class Player_SetupPlacementGhost_Patch
        {

            static void Prefix()
            {
                // logger.LogInfo("m_forceDisableInit = true");
                PlanPiece.m_forceDisableInit = true;
            }

            static void Postfix(GameObject ___m_placementGhost)
            {
                // logger.LogInfo("m_forceDisableInit = false");
                PlanPiece.m_forceDisableInit = false;
                if (___m_placementGhost != null && configTransparentGhostPlacement.Value)
                {
                    ShaderHelper.UpdateTextures(___m_placementGhost, ShaderState.Supported);
                }
            }
        }

    }
}
