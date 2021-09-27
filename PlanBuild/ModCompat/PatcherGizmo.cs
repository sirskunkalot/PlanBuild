using HarmonyLib;
using PlanBuild.Blueprints;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(GizmoReloaded.Plugin), "UpdatePlacement")]
        [HarmonyPrefix]
        private static bool GizmoPlugin_UpdatePlacement_Prefix(Transform ___gizmoRoot)
        {
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces &&
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName))
            {
                if (___gizmoRoot)
                {
                    Object.Destroy(___gizmoRoot.gameObject);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GizmoReloaded.Plugin), "GetPlacementAngle")]
        [HarmonyPrefix]
        private static bool GizmoPlugin_GetPlacementAngle_Prefix(ref Quaternion __result)
        {
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces &&
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName))
            {
                __result = Quaternion.Euler(0f, 22.5f * (float)Player.m_localPlayer.m_placeRotation, 0f);
                return false;
            }
            return true;
        }
    }
}