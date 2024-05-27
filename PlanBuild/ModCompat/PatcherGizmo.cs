using System;
using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Components;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(ComfyGizmo.Patches.PlayerPatch), "UpdatePlacementPostfix")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_UpdatePlacementPostfix_Prefix()
        {
            if (!(Player.m_localPlayer && Player.m_localPlayer.m_buildPieces&&
                  Player.m_localPlayer.m_placementGhost && ComfyGizmo.Gizmos._gizmoInstances.Count > 0))
            {
                return true;
            }
            
            if (Player.m_localPlayer.m_placementGhost.TryGetComponent<ToolComponentBase>(out var tool) &&
                tool.SuppressGizmo)
            {
                foreach (var gizmoInstance in ComfyGizmo.Gizmos._gizmoInstances)
                {
                    gizmoInstance.Hide();
                }
                return false;
            }

            if (Player.m_localPlayer.m_buildPieces.name.StartsWith(PlanHammerPrefab.PieceTableName, StringComparison.Ordinal) &&
                Player.m_localPlayer.m_placementGhost.name.StartsWith(PlanHammerPrefab.PieceDeletePlansName, StringComparison.Ordinal))
            {
                foreach (var gizmoInstance in ComfyGizmo.Gizmos._gizmoInstances)
                {
                    gizmoInstance.Hide();
                }
                return false;
            }

            return true;
        }

        /*[HarmonyPatch(typeof(ComfyGizmo.ComfyGizmo), "Rotate")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_Rotate_Prefix()
        {
            return CheckPlanBuildTool();
        }

        [HarmonyPatch(typeof(ComfyGizmo.ComfyGizmo), "RotateLocalFrame")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_RotateLocalFrame_Prefix()
        {
            return CheckPlanBuildTool();
        }

        private static bool CheckPlanBuildTool()
        {
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces &&
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName, StringComparison.Ordinal) &&
                (ZInput.GetButton(Config.ShiftModifierButton.Name) ||
                 ZInput.GetButton(Config.AltModifierButton.Name) ||
                 ZInput.GetButton(Config.CtrlModifierButton.Name)))
            {
                return false;
            }
            return true;
        }*/
    }
}