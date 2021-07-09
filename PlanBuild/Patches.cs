using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PlanBuild.Blueprints;
using PlanBuild.PlanBuild;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static PlanBuild.ShaderHelper;

namespace PlanBuild
{
    internal class Patches
    {
        public const string buildCameraGUID = "org.dkillebrew.plugins.valheim.buildCamera";
        public const string craftFromContainersGUID = "aedenthorn.CraftFromContainers";
        public const string gizmoGUID = "com.rolopogo.Gizmo"; 
        private static Harmony harmony;
           
        [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        [HarmonyPrefix]
        private static bool Player_HaveRequirements_Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (PlanManager.showAllPieces.Value)
            {
                return true;
            }
            if (PlanPiecePrefab.planToOriginalMap.TryGetValue(piece, out Piece originalPiece))
            {
                __result = __instance.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                return false;
            }
            return true;
        }

        /*private static bool interceptGetPrefab = true;
        private static HashSet<int> checkedHashes = new HashSet<int>();

        [HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(int) })]
        [HarmonyPostfix]
        internal static void ZNetScene_GetPrefab_Postfix(ZNetScene __instance, int hash, ref GameObject __result)
        {
            if (__result == null
                && interceptGetPrefab
                && !checkedHashes.Contains(hash))
            {
                interceptGetPrefab = false;
                checkedHashes.Add(hash);
                PlanManager.Instance.ScanPieceTables();
                __result = __instance.GetPrefab(hash);
                interceptGetPrefab = true;
            }
        }*/

        internal static void Apply()
        {
            On.Player.SetupPlacementGhost += SetupPlacementGhost;
            On.WearNTear.Highlight += OnHighlight; 

            harmony = new Harmony("marcopogo.PlanBuild");
            harmony.PatchAll(typeof(Patches));
            harmony.PatchAll(typeof(PlanPiece));
            if (Chainloader.PluginInfos.ContainsKey(buildCameraGUID))
            {
                Jotunn.Logger.LogInfo("Applying BuildCamera patches");
                harmony.PatchAll(typeof(ModCompat.PatcherBuildCamera));
                On.GameCamera.UpdateCamera += ModCompat.PatcherBuildCamera.OnUpdateCamera;
            }
            if (Chainloader.PluginInfos.ContainsKey(craftFromContainersGUID))
            {
                Jotunn.Logger.LogInfo("Applying CraftFromContainers patches");
                harmony.PatchAll(typeof(ModCompat.PatcherCraftFromContainers));
            }
            if (Chainloader.PluginInfos.ContainsKey(gizmoGUID))
            {
                Jotunn.Logger.LogInfo("Applying Gizmo patches");
                harmony.PatchAll(typeof(ModCompat.PatcherGizmo));
            }
        } 

        private static void OnHighlight(On.WearNTear.orig_Highlight orig, WearNTear self)
        {
            if (!PlanBuildPlugin.showRealTextures && self.TryGetComponent(out PlanPiece planPiece))
            {
                planPiece.Highlight();
                return;
            }
            orig(self);
        }

        private static void SetupPlacementGhost(On.Player.orig_SetupPlacementGhost orig, Player self)
        {
            PlanPiece.m_forceDisableInit = true;
            orig(self);
            if (self.m_placementGhost)
            {
                if (PlanBuildPlugin.showRealTextures)
                {
                    UpdateTextures(self.m_placementGhost, ShaderState.Skuld);
                }
                else if (PlanBuildPlugin.configTransparentGhostPlacement.Value
                  && (self.m_placementGhost.name.StartsWith(Blueprint.PieceBlueprintName)
                      || self.m_placementGhost.name.Split('(')[0].EndsWith(PlanPiecePrefab.PlannedSuffix))
                  )
                {
                    UpdateTextures(self.m_placementGhost, ShaderState.Supported);
                }
            }
            PlanPiece.m_forceDisableInit = false;
        }

        internal static void Remove()
        {
            harmony?.UnpatchAll(PlanBuildPlugin.PluginGUID);
        }
    }
}