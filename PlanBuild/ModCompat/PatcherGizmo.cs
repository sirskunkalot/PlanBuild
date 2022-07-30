using System;
using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Components;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(Gizmo.ComfyGizmo.PlayerPatch), "UpdatePlacementPostfix")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_UpdatePlacementPostfix_Prefix()
        {
            if (!(Player.m_localPlayer && Player.m_localPlayer.m_buildPieces&&
                  Player.m_localPlayer.m_placementGhost && Gizmo.ComfyGizmo._gizmoRoot))
            {
                return true;
            }

            if (Player.m_localPlayer.m_placementGhost.TryGetComponent<ToolComponentBase>(out var tool) &&
                tool.SuppressGizmo)
            {
                Gizmo.ComfyGizmo._gizmoRoot.gameObject.SetActive(false);
                return false;
            }

            if (Player.m_localPlayer.m_buildPieces.name.StartsWith(PlanHammerPrefab.PieceTableName, StringComparison.Ordinal) &&
                Player.m_localPlayer.m_placementGhost.name.StartsWith(PlanHammerPrefab.PieceDeletePlansName, StringComparison.Ordinal))
            {
                Gizmo.ComfyGizmo._gizmoRoot.gameObject.SetActive(false);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Gizmo.ComfyGizmo), "HandleAxisInput")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_HandleAxisInput_Prefix()
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