using System;
using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Components;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(ComfyGizmo.PlayerPatch), "UpdatePlacementPostfix")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_UpdatePlacementPostfix_Prefix()
        {
            bool isBuildPieces = Player.m_localPlayer && Player.m_localPlayer.m_buildPieces;

            if (!(isBuildPieces
                && Player.m_localPlayer.m_placementGhost
                && ComfyGizmo.Gizmos._gizmoInstances.Count > 0)
            ) return true;

            Action HideAllGizmos = () => {
                foreach (var gizmoInstance in ComfyGizmo.Gizmos._gizmoInstances)
                {
                    gizmoInstance.Hide();
                }
            };
            
            if (Player.m_localPlayer.m_placementGhost.TryGetComponent<ToolComponentBase>(out var tool)
                && tool.SuppressGizmo)
            {
                HideAllGizmos();
                return false;
            }

            bool hasPlanhammer = Player.m_localPlayer.m_buildPieces.name.StartsWith(
                PlanHammerPrefab.PieceTableName,
                StringComparison.Ordinal
            );
            bool hasDeletePlansGhost = Player.m_localPlayer.m_placementGhost.name.StartsWith(
                PlanHammerPrefab.PieceDeletePlansName,
                StringComparison.Ordinal
            );

            if (hasPlanhammer && hasDeletePlansGhost)
            {
                HideAllGizmos();
                return false;
            }

            bool hasBlueprint = Player.m_localPlayer.m_buildPieces.name.StartsWith(
                BlueprintAssets.PieceTableName,
                StringComparison.Ordinal
            );
            bool offsetButtonPressed = ZInput.GetButton(Config.ShiftModifierButton.Name)
                || ZInput.GetButton(Config.AltModifierButton.Name)
                || ZInput.GetButton(Config.CtrlModifierButton.Name);

            if (isBuildPieces && hasBlueprint && offsetButtonPressed) return false;

            return true;
        }
    }
}