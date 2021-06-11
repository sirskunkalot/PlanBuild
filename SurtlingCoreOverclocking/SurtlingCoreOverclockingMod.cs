using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.IO;
using System.Reflection;

namespace SurtlingCoreOverclocking
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public partial class SurtlingCoreOverclockingMod : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.SurtlingCoreOverclocking";
        public const string PluginName = "Surtling Core Overclocking";
        public const string PluginVersion = "1.1.1";

        private readonly Harmony harmony = new Harmony(PluginGUID);
        public static ManualLogSource logger;
        private ConfigEntry<int> nexusID;
        private bool clonedItemsProcessed;
        private OverclockCoreSlotPrefabConfig coreSlot;
        private OverclockSpeedCorePrefabConfig speedCore;
        private OverclockEfficiencyCorePrefabConfig efficiencyCore;
        private OverclockProductivityCorePrefabConfig productivityCore;

        public void Awake()
        {
            logger = Logger;
            nexusID = Config.Bind<int>("General", "NexusID", 909, new ConfigDescription("Nexus mod ID for updates", new AcceptableValueList<int>(new int[] { 909 })));

            SurtlingCoreOverclocking.m_speedCoreSpeedMultiplier = base.Config.Bind("Speed", "Speed core bonus", 0.20, new ConfigDescription("Bonus from Speed Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SurtlingCoreOverclocking.m_speedCoreEfficiencyPenalty = base.Config.Bind("Speed", "Speed core efficiency penalty", 0.5, new ConfigDescription("Efficiency penalty from Speed Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SurtlingCoreOverclocking.m_efficiencyCoreEfficiencyBonus = base.Config.Bind("Efficiency", "Efficiency core bonus", 0.25, new ConfigDescription("Bonus from Efficiency Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SurtlingCoreOverclocking.m_efficiencyCoreSpeedPenalty = base.Config.Bind("Efficiency", "Efficiency core speed penalty", 0.1, new ConfigDescription("Speed penalty from Efficiency Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SurtlingCoreOverclocking.m_productivityCoreProductivityBonus = base.Config.Bind("Productivity", "Productivity core bonus", 0.1, new ConfigDescription("Bonus from Productivity Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SurtlingCoreOverclocking.m_productivityCoreEfficiencyPenalty = base.Config.Bind("Productivity", "Productivity core efficiency penalty", 0.25, new ConfigDescription("Efficiency penalty from Productivity Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SurtlingCoreOverclocking.m_productivityCoreSpeedPenalty = base.Config.Bind("Productivity", "Productivity core speed penalty", 0.25, new ConfigDescription("Efficiency penalty from Productivity Cores", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SurtlingCoreOverclocking.m_defaultMaxOverclockCores = base.Config.Bind("Core slots", "Default max overclock core slots", 4, new ConfigDescription("Default maximum of Overclock cores on each Kiln, Smelter and Blast Furnace", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SurtlingCoreOverclocking.m_maxAdditionalOverclockCores = base.Config.Bind("Core slots", "Max additionial overclock core slots", 4, new ConfigDescription("Maximum of additional Overclock cores on each Smelter", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SurtlingCoreOverclocking.m_speedCoreSpeedMultiplier.SettingChanged += UpdateDescriptionCallback;
            SurtlingCoreOverclocking.m_speedCoreEfficiencyPenalty.SettingChanged += UpdateDescriptionCallback;

            SurtlingCoreOverclocking.m_efficiencyCoreEfficiencyBonus.SettingChanged += UpdateDescriptionCallback;
            SurtlingCoreOverclocking.m_efficiencyCoreSpeedPenalty.SettingChanged += UpdateDescriptionCallback;

            SurtlingCoreOverclocking.m_productivityCoreProductivityBonus.SettingChanged += UpdateDescriptionCallback;
            SurtlingCoreOverclocking.m_productivityCoreEfficiencyPenalty.SettingChanged += UpdateDescriptionCallback;
            SurtlingCoreOverclocking.m_productivityCoreSpeedPenalty.SettingChanged += UpdateDescriptionCallback;

            SurtlingCoreOverclocking.m_maxAdditionalOverclockCores.SettingChanged += UpdateDescriptionCallback;

            On.ObjectDB.CopyOtherDB += AddClonedItems;
            ItemManager.OnItemsRegistered += UpdateDescription;

            harmony.PatchAll();
        }

        private void AddClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            // You want that to run only once, JotunnLib has the item cached for the game session
            if (!clonedItemsProcessed)
            {
                coreSlot = new OverclockCoreSlotPrefabConfig();
                ItemManager.Instance.AddItem(coreSlot);
                coreSlot.PrefabCreated();
                speedCore = new OverclockSpeedCorePrefabConfig();
                ItemManager.Instance.AddItem(speedCore);
                speedCore.PrefabCreated();
                efficiencyCore = new OverclockEfficiencyCorePrefabConfig();
                ItemManager.Instance.AddItem(efficiencyCore);
                efficiencyCore.PrefabCreated();
                productivityCore = new OverclockProductivityCorePrefabConfig();
                ItemManager.Instance.AddItem(productivityCore);
                productivityCore.PrefabCreated();

                clonedItemsProcessed = true;
            }
            orig(self, other);
        }

        public static string GetAssetPath(string assetName, bool isDirectory = false)
        {
            string text = Path.Combine(BepInEx.Paths.PluginPath, "SurtlingCoreOverclocking", assetName);
            if (isDirectory)
            {
                if (!Directory.Exists(text))
                {
                    Assembly assembly = typeof(SurtlingCoreOverclockingMod).Assembly;
                    text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                    if (!Directory.Exists(text))
                    {
                        logger.LogWarning($"Could not find directory ({assetName}).");
                        return null;
                    }
                }
                return text;
            }
            if (!File.Exists(text))
            {
                Assembly assembly = typeof(SurtlingCoreOverclockingMod).Assembly;
                text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                if (!File.Exists(text))
                {
                    logger.LogWarning($"Could not find asset ({assetName}).");
                    return null;
                }
            }
            return text;
        }

        private void UpdateDescriptionCallback(object sender, EventArgs e)
        {
            UpdateDescription();
        }

        public void UpdateDescription()
        {
            Logger.LogInfo("Updating description of items");
            coreSlot.UpdateDescription();
            speedCore.UpdateDescription();
            efficiencyCore.UpdateDescription();
            productivityCore.UpdateDescription();
        }

        public static string InsertWords(string text, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                string newValue = words[i];
                text = text.Replace("$" + (i + 1), newValue);
            }
            return text;
        }

        [HarmonyPatch(typeof(Smelter), "Awake")]
        private class Smelter_Awake_Patch
        {
            private static void Prefix(Smelter __instance)
            {
                SurtlingCoreOverclocking surtlingCoreOverclocking = __instance.GetComponentInParent<SurtlingCoreOverclocking>();
                if (surtlingCoreOverclocking == null)
                {
                    logger.LogInfo("Adding SurtlingCoreOverclocking Component to smelter");
                    __instance.gameObject.AddComponent<SurtlingCoreOverclocking>();
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), "Spawn")]
        private class Smelter_Spawn_Patch
        {
            private static void Prefix(Smelter __instance, string ore, ref int stack)
            {
                SurtlingCoreOverclocking surtlingCoreOverclocking = __instance.GetComponentInParent<SurtlingCoreOverclocking>();
                if (surtlingCoreOverclocking)
                {
                    int aditional = surtlingCoreOverclocking.OnSpawn(ore);
                    stack += aditional;
                }
                else
                {
                    logger.LogWarning("No SurtlingCoreOverclocking component on smelter " + __instance.transform.position);
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), "GetFuel")]
        private class Smelter_GetFuel_Patch
        {
            private static void Postfix(Smelter __instance, float __result)
            {
                SurtlingCoreOverclocking surtlingCoreOverclocking = __instance.GetComponentInParent<SurtlingCoreOverclocking>();
                if (surtlingCoreOverclocking)
                {
                    surtlingCoreOverclocking.OnGetFuel(__result);
                }
                else
                {
                    logger.LogWarning("No SurtlingCoreOverclocking component on smelter " + __instance.transform.position);
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), "SetFuel")]
        private class Smelter_SetFuel_Patch
        {
            private static void Prefix(Smelter __instance, ref float fuel)
            {
                SurtlingCoreOverclocking surtlingCoreOverclocking = __instance.GetComponentInParent<SurtlingCoreOverclocking>();
                if (surtlingCoreOverclocking)
                {
                    fuel = surtlingCoreOverclocking.OnSetFuel(fuel);
                }
                else
                {
                    logger.LogWarning("No SurtlingCoreOverclocking component on smelter " + __instance.transform.position);
                }
            }
        }
    }
}