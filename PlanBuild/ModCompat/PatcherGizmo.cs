using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Tools;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        private static Transform gizmoRoot;
        private static float snapAngle;

        [HarmonyPatch(typeof(GizmoReloaded.Plugin), "UpdatePlacement")]
        [HarmonyPrefix]
        private static bool GizmoPlugin_UpdatePlacement_Prefix(Transform ___gizmoRoot,  float ___snapAngle)
        {
            gizmoRoot = ___gizmoRoot;
            snapAngle = ___snapAngle;
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces && Player.m_localPlayer.m_placementGhost &&
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName) &&
                !Player.m_localPlayer.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName))
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
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName) &&
                !Player.m_localPlayer.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                __result = Quaternion.Euler(0f, 22.5f * (float)Player.m_localPlayer.m_placeRotation, 0f);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GizmoReloaded.Plugin), "UpdateRotation")]
        [HarmonyPrefix]
        private static bool GizmoPlugin_UpdateRotation_Prefix(string axis)
        {
            if (axis == "Y" && Player.m_localPlayer && Player.m_localPlayer.m_buildPieces &&
             Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName) &&
             ZInput.GetButton(BlueprintConfig.CameraModifierButton.Name))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ToolComponentBase), "UndoRotation")]
        [HarmonyPrefix]
        private static bool ToolComponentBase_UndoRotation_Prefix(Player player, float scrollWheel)
        {
            if(gizmoRoot)
            {
                gizmoRoot.rotation *= Quaternion.AngleAxis(-Mathf.Sign(scrollWheel) * snapAngle, Vector3.up);
            }
            return false;
        }

    }
}