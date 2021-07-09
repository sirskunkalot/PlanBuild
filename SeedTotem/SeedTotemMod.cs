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
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SeedTotem
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major)]
    internal class SeedTotemMod : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.SeedTotem";
        public const string PluginName = "Seed Totem";
        public const string PluginVersion = "1.1.6";
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
            CreateConfiguration();
            ItemManager.OnVanillaItemsAvailable += AddCustomPrefabs;

            SeedTotem.configGlowColor.SettingChanged += SettingsChanged;
            SeedTotem.configLightColor.SettingChanged += SettingsChanged;
            SeedTotem.configLightIntensity.SettingChanged += SettingsChanged;
            SeedTotem.configFlareColor.SettingChanged += SettingsChanged;
            SeedTotem.configFlareSize.SettingChanged += SettingsChanged;

            SeedTotemPrefabConfig.configLocation.SettingChanged += UpdatePieceLocation;

            PieceManager.OnPiecesRegistered += OnPiecesRegistered;
        }

        private void CreateConfiguration()
        {
            //server configs
            SeedTotem.configRadius = Config.Bind("Server", "Dispersion Radius", defaultValue: 5f, new ConfigDescription("Dispersion radius of the Seed Totem.", new AcceptableValueRange<float>(2f, 20f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configDispersionTime = Config.Bind("Server", "Dispersion time", defaultValue: 10f, new ConfigDescription("Time (in seconds) between each dispersion", new AcceptableValueRange<float>(10f, 3600f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configMargin = Config.Bind("Server", "Space requirement margin", defaultValue: 0.1f, new ConfigDescription("Extra distance to make sure plants have enough space", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configDispersionCount = Config.Bind("Server", "Dispersion count", defaultValue: 5, new ConfigDescription("Maximum number of plants to place when dispersing", new AcceptableValueRange<int>(1, 20), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configMaxRetries = Config.Bind("Server", "Max retries", defaultValue: 8, new ConfigDescription("Maximum number of placement tests on each dispersion", new AcceptableValueRange<int>(1, 20), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configHarvestOnHit = Config.Bind("Server", "Harvest on hit", defaultValue: true, new ConfigDescription("Should the Seed totem send out a wave to pick all pickables in radius when hit?", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configCheckCultivated = Config.Bind("Server", "Check for cultivated ground", defaultValue: true, new ConfigDescription("Should the Seed totem also check for cultivated land?", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configCheckBiome = Config.Bind("Server", "Check for correct biome", defaultValue: true, new ConfigDescription("Should the Seed totem also check for the correct biome?", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configCustomRecipe = Config.Bind("Server", "Custom piece requirements", false, new ConfigDescription("Load custom piece requirements from " + SeedTotemPrefabConfig.requirementsFile + "?", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeedTotem.configMaxSeeds = Config.Bind("Server", "Max seeds in totem (0 is no limit)", defaultValue: 0, new ConfigDescription("Maximum number of seeds in each totem, 0 is no limit", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            //client configs
            SeedTotem.configShowQueue = Config.Bind<bool>("UI", "Show queue", defaultValue: true, new ConfigDescription("Show the current queue on hover"));
            SeedTotem.configGlowColor = Config.Bind<Color>("Graphical", "Glow lines color", new Color(0f, 0.8f, 0f, 1f), new ConfigDescription("Color of the glowing lines on the Seed totem"));
            SeedTotem.configLightColor = Config.Bind<Color>("Graphical", "Glow light color", new Color(0f, 0.8f, 0f, 0.05f), new ConfigDescription("Color of the light from the Seed totem"));
            SeedTotem.configLightIntensity = Config.Bind<float>("Graphical", "Glow light intensity", 3f, new ConfigDescription("Intensity of the light flare from the Seed totem", new AcceptableValueRange<float>(0f, 5f)));
            SeedTotem.configFlareColor = Config.Bind<Color>("Graphical", "Glow flare color", new Color(0f, 0.8f, 0f, 0.1f), new ConfigDescription("Color of the light flare from the Seed totem"));
            SeedTotem.configFlareSize = Config.Bind<float>("Graphical", "Glow flare size", 3f, new ConfigDescription("Size of the light flare from the Seed totem", new AcceptableValueRange<float>(0f, 5f)));
            nexusID = Config.Bind<int>("General", "NexusID", 876, new ConfigDescription("Nexus mod ID for updates", new AcceptableValueList<int>(new int[] { 876 })));

            SeedTotemPrefabConfig.configLocation = Config.Bind("UI", "Build menu", PieceLocation.Hammer, "In which build menu is the Seed totem located");
        }

        private void OnPiecesRegistered()
        {
            seedTotemPrefabConfig.UpdatePieceLocation();
        }

        public void OnDestroy()
        {
            harmony?.UnpatchAll(PluginGUID);
        }

        private void AddCustomPrefabs()
        {
            try
            {
                seedTotemPrefabConfig = new SeedTotemPrefabConfig();

                var seedTotemPrefab = PrefabManager.Instance.CreateClonedPrefab(SeedTotemPrefabConfig.prefabName, "guard_stone");
                seedTotemPrefabConfig.UpdateCopiedPrefab(seedTotemPrefab);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding cloned item: {ex.Message}");
            }
            finally
            {
                ItemManager.OnVanillaItemsAvailable -= AddCustomPrefabs;
            }
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