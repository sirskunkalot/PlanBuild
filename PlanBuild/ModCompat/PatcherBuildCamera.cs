using HarmonyLib;
using PlanBuild.Blueprints;

namespace PlanBuild.ModCompat
{
    internal class PatcherBuildCamera
    {
        internal static bool UpdateCamera = true;

        [HarmonyPatch(typeof(Valheim_Build_Camera.Valheim_Build_Camera), "IsTool")]
        [HarmonyPrefix]
        private static bool ValheimBuildCamera_IsTool_Prefix(ItemDrop.ItemData itemData, ref bool __result)
        {
            if (itemData?.m_shared.m_name == BlueprintRunePrefab.BlueprintRuneItemName)
            {
                __result = true;
                return false;
            }
            return true;
        }

        internal static void OnUpdateCamera(On.GameCamera.orig_UpdateCamera orig, GameCamera self, float dt)
        {
            UpdateCamera = !Valheim_Build_Camera.Valheim_Build_Camera.InBuildMode();
            orig(self, dt);
            UpdateCamera = true;
        }
    }
}