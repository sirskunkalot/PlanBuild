using HarmonyLib;
using PlanBuild.Blueprints;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(Gizmo.ComfyGizmo.PlayerPatch), "UpdatePlacementPostfix")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_UpdatePlacementPostfix_Prefix()
        {
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces && Player.m_localPlayer.m_placementGhost &&
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName) &&
                !Player.m_localPlayer.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName))
            {
                if (Gizmo.ComfyGizmo._gizmoRoot)
                {
                    Gizmo.ComfyGizmo._gizmoRoot.gameObject.SetActive(false);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Gizmo.ComfyGizmo), "HandleAxisInput")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_HandleAxisInput_Prefix()
        {
            if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces &&
                Player.m_localPlayer.m_buildPieces.name.StartsWith(BlueprintAssets.PieceTableName) &&
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