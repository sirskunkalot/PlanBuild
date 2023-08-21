using System;
using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Components;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(Gizmo.Patches.PlayerPatch), "UpdatePlacementPostfix")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_UpdatePlacementPostfix_Prefix()
        {
            if (!(Player.m_localPlayer && Player.m_localPlayer.m_buildPieces&&
                  Player.m_localPlayer.m_placementGhost && Gizmo.ComfyGizmo.GizmoRoot))
            {
                return true;
            }

            if (Player.m_localPlayer.m_placementGhost.TryGetComponent<ToolComponentBase>(out var tool) &&
                tool.SuppressGizmo)
            {
                Gizmo.ComfyGizmo.GizmoRoot.gameObject.SetActive(false);
                return false;
            }

            if (Player.m_localPlayer.m_buildPieces.name.StartsWith(PlanHammerPrefab.PieceTableName, StringComparison.Ordinal) &&
                Player.m_localPlayer.m_placementGhost.name.StartsWith(PlanHammerPrefab.PieceDeletePlansName, StringComparison.Ordinal))
            {
                Gizmo.ComfyGizmo.GizmoRoot.gameObject.SetActive(false);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Gizmo.ComfyGizmo), "Rotate")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_Rotate_Prefix()
        {
            return CheckPlanBuildTool();
        }

        [HarmonyPatch(typeof(Gizmo.ComfyGizmo), "RotateLocalFrame")]
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
        }
    }
}