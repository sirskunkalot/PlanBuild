using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PlanBuild.Blueprints;
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
          
        [HarmonyPatch(typeof(PieceManager), "RegisterInPieceTables")]
        [HarmonyPrefix]
        private static void PieceManager_RegisterInPieceTables_Prefix()
        {
            PlanBuildPlugin.Instance.InitialScanHammer();
        }

        [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        [HarmonyPrefix]
        private static bool Player_HaveRequirements_Prefix(Player __instance, Piece piece, ref bool __result)
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
        private static void ZNetScene_GetPrefab_Postfix(ZNetScene __instance, int hash, ref GameObject __result)
        {
            if (__result == null
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
            On.WearNTear.Highlight += OnHighlight;
            // IL.Plant.HaveGrowSpace += ILHaveGrowSpace;

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
      //
      //private static void ILHaveGrowSpace(ILContext il)
      //{
      //    ILCursor cContinue = new ILCursor(il);
      //    ILLabel lblContinueTarget = null;
      //    cContinue.GotoNext(
      //        zz => zz.MatchBrtrue(out lblContinueTarget) //Capture the label to continue to
      //        );        
      //
      //    ILCursor c = new ILCursor(il);
      //    c.GotoNext(MoveType.Before,
      //        zz => zz.MatchBr(out lblContinueTarget),                              
      //        zz => zz.MatchLdloc(0),                                               
      //        zz => zz.MatchLdloc(1),
      //        zz => zz.MatchLdelemRef(),
      //        zz => zz.MatchCallOrCallvirt<Component>("GetComponent")             //Find the Object.Instantiate function
      //    );
      //    c.Emit(OpCodes.Ldloc, 0);
      //    c.Emit(OpCodes.Ldloc, 1);
      //    c.Emit(OpCodes.Ldelem_Ref);
      //    c.Emit(OpCodes.Call, typeof(Component).GetMethod("GetComponent").MakeGenericMethod(typeof(PlanPiece)));
      //
      //
      //    // c.Emit(OpCodes.Ldloc, resultLoc);                                       //Load the instantiated object for ...
      //    // c.Emit(OpCodes.Call, typeof(PulleyManager).GetMethod("PlacedPiece"));   //my hook :D
      //}

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