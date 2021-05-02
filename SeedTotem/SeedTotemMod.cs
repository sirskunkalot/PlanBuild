// SeedTotem
// a Valheim mod skeleton using Jötunn
// 
// File:    SeedTotem.cs
// Project: SeedTotem

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using ModConfigEnforcer;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SeedTotem
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("pfhoenix.modconfigenforcer")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class SeedTotemMod : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.SeedTotem";
        public const string PluginName = "Seed Totem";
        public const string PluginVersion = "1.1.0";
        public static ManualLogSource logger; 
        public ConfigEntry<int> nexusID;
        private SeedTotemPrefabConfig seedTotemPrefabConfig;
        private Harmony harmony;

        public enum PieceLocation
        {
            Hammer, Cultivator
        }

        public void Awake()
        {
            logger = Logger;
            SeedTotemPrefabConfig.logger = logger;
            SeedTotem.logger = logger;

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            SeedTotem.configRadius = new ConfigVariable<float>(Config, "Server", "Dispersion radius", defaultValue: 5f, new ConfigDescription("Dispersion radius of the Seed Totem.", new AcceptableValueRange<float>(2f, 20f)), localOnly: false);
            SeedTotem.configDispersionTime = new ConfigVariable<float>(Config, "Server", "Dispersion time", defaultValue: 10f, new ConfigDescription("Time (in seconds) between each dispersion", new AcceptableValueRange<float>(10f, 3600f)), localOnly: false);
            SeedTotem.configMargin = new ConfigVariable<float>(Config, "Server", "Space requirement margin", defaultValue: 0.1f, new ConfigDescription("Extra distance to make sure plants have enough space", new AcceptableValueRange<float>(0f, 2f)), localOnly: false);
            SeedTotem.configDispersionCount = new ConfigVariable<int>(Config, "Server", "Dispersion count", defaultValue: 5, new ConfigDescription("Maximum number of plants to place when dispersing", new AcceptableValueRange<int>(1, 20)), localOnly: false);
            SeedTotem.configMaxRetries = new ConfigVariable<int>(Config, "Server", "Max retries", defaultValue: 8, new ConfigDescription("Maximum number of placement tests on each dispersion", new AcceptableValueRange<int>(1, 20)), localOnly: false);
            SeedTotem.configHarvestOnHit = new ConfigVariable<bool>(Config, "Server", "Harvest on hit", defaultValue: true, new ConfigDescription("Should the Seed totem send out a wave to pick all pickables in radius when hit?"), localOnly: false);
            SeedTotem.configCheckCultivated = new ConfigVariable<bool>(Config, "Server", "Check for cultivated ground", defaultValue: true, new ConfigDescription("Should the Seed totem also check for cultivated land?"), localOnly: false);
            SeedTotem.configCheckBiome = new ConfigVariable<bool>(Config, "Server", "Check for correct biome", defaultValue: true, new ConfigDescription("Should the Seed totem also check for the correct biome?"), localOnly: false);

            On.ObjectDB.CopyOtherDB += AddCustomPrefabs; 

          

            SeedTotemPrefabConfig.configCustomRecipe = new ConfigVariable<bool>(Config, "Server", "Custom piece requirements", false, new ConfigDescription("Load custom piece requirements from " + SeedTotemPrefabConfig.requirementsFile + "?"), localOnly: false);
       
            SeedTotem.configShowQueue = Config.Bind<bool>("UI", "Show queue", defaultValue: true, new ConfigDescription("Show the current queue on hover"));
            SeedTotem.configGlowColor = Config.Bind<Color>("Graphical", "Glow lines color", new Color(0f, 0.8f, 0f, 1f), new ConfigDescription("Color of the glowing lines on the Seed totem"));
            SeedTotem.configLightColor = Config.Bind<Color>("Graphical", "Glow light color", new Color(0f, 0.8f, 0f, 0.05f), new ConfigDescription("Color of the light from the Seed totem"));
            SeedTotem.configLightIntensity = Config.Bind<float>("Graphical", "Glow light intensity", 3f, new ConfigDescription("Intensity of the light flare from the Seed totem", new AcceptableValueRange<float>(0f, 5f)));
            SeedTotem.configFlareColor = Config.Bind<Color>("Graphical", "Glow flare color", new Color(0f, 0.8f, 0f, 0.1f), new ConfigDescription("Color of the light flare from the Seed totem"));
            SeedTotem.configFlareSize = Config.Bind<float>("Graphical", "Glow flare size", 3f, new ConfigDescription("Size of the light flare from the Seed totem", new AcceptableValueRange<float>(0f, 5f)));
            nexusID = Config.Bind<int>("General", "NexusID", 876, new ConfigDescription("Nexus mod ID for updates", new AcceptableValueList<int>(new int[] { 876 })));
       
            SeedTotemPrefabConfig.configLocation = Config.Bind("UI", "Build menu", PieceLocation.Cultivator, "In which build menu is the Seed totem located");
             
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configRadius);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configDispersionTime);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configMargin);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configDispersionCount);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configMaxRetries);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configHarvestOnHit);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configCheckCultivated);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotemPrefabConfig.configCustomRecipe);
            ConfigManager.RegisterModConfigVariable(PluginName, SeedTotem.configCheckBiome);
       
            SeedTotem.configGlowColor.SettingChanged += SettingsChanged;
            SeedTotem.configLightColor.SettingChanged += SettingsChanged;
            SeedTotem.configLightIntensity.SettingChanged += SettingsChanged;
            SeedTotem.configFlareColor.SettingChanged += SettingsChanged;
            SeedTotem.configFlareSize.SettingChanged += SettingsChanged;
       
            SeedTotemPrefabConfig.configLocation.SettingChanged += UpdatePieceLocation; 

            
        }

        public void OnDestroy()
        { 
            harmony?.UnpatchAll(PluginGUID); 
        }

        private void AddCustomPrefabs(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            seedTotemPrefabConfig = new SeedTotemPrefabConfig();

            GameObject seedTotemPrefab = PrefabManager.Instance.CreateClonedPrefab(SeedTotemPrefabConfig.prefabName, "guard_stone");
            seedTotemPrefabConfig.UpdateCopiedPrefab(seedTotemPrefab);

            orig(self, other);
        }
         

        private void UpdatePieceLocation(object sender, EventArgs e)
        {
            seedTotemPrefabConfig.UpdatePieceLocation();
        }

        private void SettingsChanged(object sender, EventArgs e)
        {
            SeedTotem.SettingsUpdated();
        }

        public static string GetAssetPath(string assetName, bool isDirectory = false)
        {
            string text = Path.Combine(BepInEx.Paths.PluginPath, "SeedTotem", assetName);
            if (isDirectory)
            {
                if (!Directory.Exists(text))
                {
                    Assembly assembly = typeof(SeedTotemMod).Assembly;
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
                Assembly assembly = typeof(SeedTotemMod).Assembly;
                text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                if (!File.Exists(text))
                {
                    logger.LogWarning($"Could not find asset ({assetName}).");
                    return null;
                }
            }
            return text;
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            { // Set a breakpoint here to break on F9 key press
                Jotunn.Logger.LogInfo("Right here");
            }
        }
#endif
    }
}