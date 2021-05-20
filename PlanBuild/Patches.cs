using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using PlanBuild.Plans;
using static PlanBuild.ShaderHelper;

namespace PlanBuild
{
    class Patches
    {
        public const string buildCameraGUID = "org.dkillebrew.plugins.valheim.buildCamera";
        public const string buildShareGUID = "com.valheim.cr_advanced_builder";
        public const string craftFromContainersGUID = "aedenthorn.CraftFromContainers";
        public const string equipmentQuickSlotsGUID = "randyknapp.mods.equipmentandquickslots";

        [HarmonyPatch(typeof(PieceManager), "RegisterInPieceTables")]
        [HarmonyPrefix]
        static void PieceManager_RegisterInPieceTables_Prefix()
        {
            PlanBuild.Instance.ScanHammer();
        }

        [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        [HarmonyPrefix]
        static bool Player_HaveRequirements_Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (PlanBuild.showAllPieces.Value)
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

        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        [HarmonyPrefix]
        static void Player_SetupPlacementGhost_Prefix()
        {
            PlanPiece.m_forceDisableInit = true;
        }

        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        [HarmonyPostfix]
        static void Player_SetupPlacementGhost_Postfix(GameObject ___m_placementGhost)
        {
            PlanPiece.m_forceDisableInit = false;
            if (___m_placementGhost != null && PlanBuild.configTransparentGhostPlacement.Value)
            {
                ShaderHelper.UpdateTextures(___m_placementGhost, ShaderState.Supported);
            }
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
                PlanBuild.Instance.ScanHammer(true);
                __result = __instance.GetPrefab(hash);
                interceptGetPrefab = true;
            } 
        }

        internal static void Apply(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
            harmony.PatchAll(typeof(PlanPiece));
            if (Chainloader.PluginInfos.ContainsKey(buildCameraGUID))
            {
                Jotunn.Logger.LogInfo("Applying BuildCamera patches");
                harmony.PatchAll(typeof(ModCompat.PatcherBuildCamera));
            }
            if (Chainloader.PluginInfos.ContainsKey(craftFromContainersGUID))
            {
                Jotunn.Logger.LogInfo("Applying CraftFromContainers patches");
                harmony.PatchAll(typeof(ModCompat.PatcherCraftFromContainers));
            }
            if (Chainloader.PluginInfos.ContainsKey(equipmentQuickSlotsGUID))
            {
                Jotunn.Logger.LogInfo("Applying EquipmentQuickSlots patches");
                harmony.PatchAll(typeof(ModCompat.PatcherEquipmentQuickSlots));
            }
            HarmonyLib.Patches patches = Harmony.GetPatchInfo(typeof(Player).GetMethod("OnSpawned"));
            if (patches?.Owners.Contains(buildShareGUID) == true)
            {
                Jotunn.Logger.LogInfo("Applying BuildShare patches");
                harmony.PatchAll(typeof(ModCompat.PatcherBuildShare));
            }
            
        }
    }
}
