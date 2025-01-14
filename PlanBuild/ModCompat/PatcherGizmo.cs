using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using BepInEx;
using PlanBuild.Blueprints.Components;
using PlanBuild.Plans;

namespace PlanBuild.ModCompat
{
    internal static class PatcherGizmo
    {
        public const string GizmoGUID = "bruce.valheim.comfymods.gizmo";
        private static bool? _ComfyGizmoInstalled = null;
        private static BaseUnityPlugin ComfyGizmoPlugin;

        private const string ComfyGizmoTargetType = "ComfyGizmo.Gizmos";
        private static Type GizmosType;

        private const string GizmosFieldName = "_gizmoInstances";
        private static FieldInfo GizmosField;

        private const string GizmosHideMethodName = "Hide";
        private static MethodInfo GizmosHideMethod;

        private static readonly List<object> EmptyList = new List<object>();

        /// <summary>
        ///     Get whether comfy gizmo is installed and the target fields/methods 
        ///     can be accessed via reflection to apply the desired patches.
        /// </summary>
        public static bool ComfyGizmoInstalled
        {
            get
            {
                // Check for ComfyGizmo
                _ComfyGizmoInstalled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GizmoGUID);
                if (_ComfyGizmoInstalled.Value && GizmosType is null)
                {
                    ComfyGizmoPlugin ??= BepInEx.Bootstrap.Chainloader.PluginInfos[GizmoGUID].Instance;
                    Type[] types = AccessTools.GetTypesFromAssembly(ComfyGizmoPlugin.GetType().Assembly);
                    foreach (Type type in types)
                    {
                        if (type.ToString() == ComfyGizmoTargetType)
                        {
                            GizmosType ??= type;
                            GizmosHideMethod ??= AccessTools.Method(type, GizmosHideMethodName);
                            GizmosField ??= AccessTools.Field(type, GizmosFieldName);
                            break;
                        }
                    }
                }

                bool canPatchGizmo = GizmosType is not null && GizmosHideMethod is not null && GizmosField is not null;
                if (_ComfyGizmoInstalled.Value && !canPatchGizmo)
                {
                    Jotunn.Logger.LogWarning("Found ComfyGizmo installed but cannot patch it!");
                }
                return _ComfyGizmoInstalled.Value && canPatchGizmo;
            }
        }

        private static List<object> GetGizmoInstances()
        {
            if (!ComfyGizmoInstalled)
            {
                return EmptyList;
            }
            return (List<object>)GizmosField.GetValue(null);

        }

        [HarmonyPatch("ComfyGizmo.PlayerPatch", "UpdatePlacementPostfix")]
        [HarmonyPrefix]
        private static bool ComfyGizmo_UpdatePlacementPostfix_Prefix()
        {
            if (!Player.m_localPlayer || Player.m_localPlayer.m_buildPieces || !Player.m_localPlayer.m_placementGhost)
            {
                return true;
            }

            // cache this to avoid performing multiple casts to List<object>
            var gizmoInstances = GetGizmoInstances();
            if (gizmoInstances.Count <= 0)
            {
                return true;
            }

            if (Player.m_localPlayer.m_placementGhost.TryGetComponent<ToolComponentBase>(out var tool) &&
                tool.SuppressGizmo)
            {
                foreach (var gizmoInstance in gizmoInstances)
                {
                    GizmosHideMethod.Invoke(gizmoInstance, null);
                }
                return false;
            }

            if (Player.m_localPlayer.m_buildPieces.name.StartsWith(PlanHammerPrefab.PieceTableName, StringComparison.Ordinal) &&
                Player.m_localPlayer.m_placementGhost.name.StartsWith(PlanHammerPrefab.PieceDeletePlansName, StringComparison.Ordinal))
            {
                foreach (var gizmoInstance in gizmoInstances)
                {
                    GizmosHideMethod.Invoke(gizmoInstance, null);
                }
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