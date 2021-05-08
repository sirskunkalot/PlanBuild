using HarmonyLib;
using System;
using System.IO;

namespace PlanBuild
{
    public class PatcherBuildShare
    {

        internal static bool interceptReadFile = false;

        [HarmonyPatch(typeof(BuildShare.BuildShare), "Build")]
        [HarmonyPrefix]
        static void BuildShare_Build_Prefix()
        {
            interceptReadFile = PlanBuild.configBuildShare.Value;
        }

        [HarmonyPatch(typeof(BuildShare.BuildShare), "Build")]
        [HarmonyPostfix]
        static void BuildShare_Build_Postfix()
        {
            interceptReadFile = false;
        }

        [HarmonyPatch(typeof(File), "ReadAllLines", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        static void File_ReadAllLines_Postfix(ref string[] __result)
        { 
            if (interceptReadFile)
            {
                Jotunn.Logger.LogInfo("Replacing .vbuild with planned pieces");
                string[] newResult = new string[__result.Length];
                for (int i = 0; i < __result.Length; i++)
                {

                    string[] parts = __result[i].Split(' ');
                    string prefabName = parts[0];
                    prefabName += PlanPiecePrefabConfig.plannedSuffix; 
                    var planPrefab = ZNetScene.instance.GetPrefab(prefabName);
                    if (planPrefab != null)
                    {
                        parts[0] = prefabName;
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning("No planned version for '" + prefabName + "' using real piece instead!");
                    }
                    newResult[i] = string.Join(" ", parts); 
                }
                __result = newResult;
                interceptReadFile = false;
            }
        }
    }
}
