using BepInEx.Bootstrap;
using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.PlanBuild;
using PlanBuild.Plans;
using System;
using static PlanBuild.Utils.ShaderHelper;

namespace PlanBuild
{
    internal class Patches
    {
        public const string BuildCameraGUID = "org.dkillebrew.plugins.valheim.buildCamera";
        public const string CraftFromContainersGUID = "aedenthorn.CraftFromContainers";
        public const string GizmoGUID = "com.rolopogo.Gizmo";
        public const string ValheimRaftGUID = "BepIn.Sarcen.ValheimRAFT";
        private static Harmony Harmony;

        internal static void Apply()
        {
            On.Player.SetupPlacementGhost += SetupPlacementGhost;
            On.WearNTear.Highlight += OnHighlight;
            On.Player.HaveRequirements_Piece_RequirementMode += Player_HaveRequirements_Piece_RequirementMode;

            Harmony = new Harmony("marcopogo.PlanBuild");
            Harmony.PatchAll(typeof(PlanPiece));
            if (Chainloader.PluginInfos.ContainsKey(BuildCameraGUID))
            {
                Jotunn.Logger.LogInfo("Applying BuildCamera patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherBuildCamera));
                On.GameCamera.UpdateCamera += ModCompat.PatcherBuildCamera.OnUpdateCamera;
            }
            if (Chainloader.PluginInfos.ContainsKey(CraftFromContainersGUID))
            {
                Jotunn.Logger.LogInfo("Applying CraftFromContainers patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherCraftFromContainers));
            }
            if (Chainloader.PluginInfos.ContainsKey(GizmoGUID))
            {
                Jotunn.Logger.LogInfo("Applying Gizmo patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherGizmo));
            }
            if (Chainloader.PluginInfos.ContainsKey(ValheimRaftGUID))
            {
                Jotunn.Logger.LogInfo("Applying ValheimRAFT patches");
                Harmony.PatchAll(typeof(ModCompat.PatcherValheimRaft));
            }
        }

        private static bool Player_HaveRequirements_Piece_RequirementMode(On.Player.orig_HaveRequirements_Piece_RequirementMode orig, Player self, Piece piece, Player.RequirementMode mode)
        {
            try
            {
                if (piece && !PlanManager.showAllPieces.Value && PlanPiecePrefab.PlanToOriginalMap.TryGetValue(piece.gameObject.name, out Piece originalPiece))
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
            if (!PlanBuildPlugin.ShowRealTextures && self.TryGetComponent(out PlanPiece planPiece))
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
                if (PlanBuildPlugin.ShowRealTextures)
                {
                    UpdateTextures(self.m_placementGhost, ShaderState.Skuld);
                }
                else if (PlanBuildPlugin.ConfigTransparentGhostPlacement.Value
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
            Harmony?.UnpatchAll(PlanBuildPlugin.PluginGUID);
        }
    }
}