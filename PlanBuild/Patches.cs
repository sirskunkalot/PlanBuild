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
using static PlanBuild.Utils.ShaderHelper;

namespace PlanBuild
{
    internal class Patches
    {
        public const string buildCameraGUID = "org.dkillebrew.plugins.valheim.buildCamera";
        public const string craftFromContainersGUID = "aedenthorn.CraftFromContainers";
        public const string gizmoGUID = "com.rolopogo.Gizmo";
        public const string valheimRaftGUID = "BepIn.Sarcen.ValheimRAFT";
        private static Harmony harmony;

        internal static void Apply()
        {
            On.Player.SetupPlacementGhost += SetupPlacementGhost;
            On.WearNTear.Highlight += OnHighlight;
            On.Player.HaveRequirements_Piece_RequirementMode += Player_HaveRequirements_Piece_RequirementMode;

            harmony = new Harmony("marcopogo.PlanBuild");
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
            if (Chainloader.PluginInfos.ContainsKey(valheimRaftGUID))
            {
                Jotunn.Logger.LogInfo("Applying ValheimRAFT patches");
                harmony.PatchAll(typeof(ModCompat.PatcherValheimRaft));
            }
        }

        private static bool Player_HaveRequirements_Piece_RequirementMode(On.Player.orig_HaveRequirements_Piece_RequirementMode orig, Player self, Piece piece, Player.RequirementMode mode)
        {
            try
            {
                if (piece && !PlanManager.showAllPieces.Value && PlanPiecePrefab.planToOriginalMap.TryGetValue(piece.gameObject.name, out Piece originalPiece))
                {
                    return self.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                }
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"Error while executing Player.HaveRequirements({piece},{mode}): {e}");
            }
            return orig(self, piece, mode);
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