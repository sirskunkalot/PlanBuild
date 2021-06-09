using HarmonyLib;
using PlanBuild.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild.ModCompat
{
    class PatcherGizmo
    {
        [HarmonyPatch(typeof(Gizmo.Plugin), "HandleAxisInput")]
        [HarmonyPrefix]
        static bool GizmoPlugin_HandleAxisInput_Prefix(int scrollWheelInput, ref int rot, Transform gizmo)
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
        static bool BlueprintManager_UndoRotation_Prefix(Player player, float scrollWheel)
        {
            //Undo rotation with Gizmo instead
            Gizmo.Plugin.instance.HandleAxisInput(-1 * Math.Sign(scrollWheel), ref Gizmo.Plugin.instance.yRot, Gizmo.Plugin.instance.yGizmo);
            return false;
        }

    }
}
