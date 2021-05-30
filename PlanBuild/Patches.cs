using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using PlanBuild.Plans;
using static PlanBuild.ShaderHelper;
using PlanBuild.Blueprints;

namespace PlanBuild
{
    class Patches
    {
        public const string buildCameraGUID = "org.dkillebrew.plugins.valheim.buildCamera"; 
        public const string craftFromContainersGUID = "aedenthorn.CraftFromContainers"; 

        private static Harmony harmony;

        [HarmonyPatch(typeof(PieceManager), "RegisterInPieceTables")]
        [HarmonyPrefix]
        static void PieceManager_RegisterInPieceTables_Prefix()
        {
            PlanBuildPlugin.Instance.InitialScanHammer();
        }

        [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        [HarmonyPrefix]
        static bool Player_HaveRequirements_Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (PlanBuildPlugin.showAllPieces.Value)
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
          
        private static bool interceptGetPrefab = true;
        private static HashSet<int> checkedHashes = new HashSet<int>();

        [HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(int) })]
        [HarmonyPostfix]
        static void ZNetScene_GetPrefab_Postfix(ZNetScene __instance, int hash, ref GameObject __result)
        {
            if(__result == null
                && interceptGetPrefab
                && !checkedHashes.Contains(hash))
            {
                interceptGetPrefab = false;
                checkedHashes.Add(hash);
                PlanBuildPlugin.Instance.ScanHammer(true);
                __result = __instance.GetPrefab(hash);
                interceptGetPrefab = true;
            } 
        }

        internal static void Apply()
        {
            On.Player.SetupPlacementGhost += SetupPlacementGhost;

            harmony = new Harmony("marcopogo.PlanBuild");
            harmony.PatchAll(typeof(Patches));
            harmony.PatchAll(typeof(PlanPiece));
            if (Chainloader.PluginInfos.ContainsKey(buildCameraGUID))
            {
                Jotunn.Logger.LogInfo("Applying BuildCamera patches");
                harmony.PatchAll(typeof(ModCompat.PatcherBuildCamera));
                On.GameCamera.UpdateCamera += ModCompat.PatcherBuildCamera.UpdateCamera;
            }
            if (Chainloader.PluginInfos.ContainsKey(craftFromContainersGUID))
            {
                Jotunn.Logger.LogInfo("Applying CraftFromContainers patches");
                harmony.PatchAll(typeof(ModCompat.PatcherCraftFromContainers));
            } 
        }

        private static void SetupPlacementGhost(On.Player.orig_SetupPlacementGhost orig, Player self)
        {
            PlanPiece.m_forceDisableInit = true;
            orig(self);
            if(self.m_placementGhost)
            {
                if(PlanBuildPlugin.showRealTextures)
                {
                    UpdateTextures(self.m_placementGhost, ShaderState.Skuld);
                } else if (PlanBuildPlugin.configTransparentGhostPlacement.Value
                    && (self.m_placementGhost.name.StartsWith(Blueprint.BlueprintPrefabName)
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
