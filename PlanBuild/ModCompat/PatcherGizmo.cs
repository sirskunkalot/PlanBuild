using HarmonyLib;
using PlanBuild.Blueprints;
using System;
using UnityEngine;

namespace PlanBuild.ModCompat
{
    internal class PatcherGizmo
    {
        [HarmonyPatch(typeof(Gizmo.Plugin), "HandleAxisInput")]
        [HarmonyPrefix]
        private static bool GizmoPlugin_HandleAxisInput_Prefix(int scrollWheelInput, ref int rot, Transform gizmo)
        {
            if (Player.m_localPlayer && Player.m_localPlayer.m_placementGhost &&
                Player.m_localPlayer.m_placementGhost.name.StartsWith(Blueprint.BlueprintPrefabName)
                && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                    || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                return false;
            }
;
            return true;
        }

        [HarmonyPatch(typeof(BlueprintManager), "UndoRotation")]
        [HarmonyPrefix]
        private static bool BlueprintManager_UndoRotation_Prefix(Player player, float scrollWheel)
        {
            //Undo rotation with Gizmo instead
            Gizmo.Plugin.instance.HandleAxisInput(-1 * Math.Sign(scrollWheel), ref Gizmo.Plugin.instance.yRot, Gizmo.Plugin.instance.yGizmo);
            return false;
        }
    }
}