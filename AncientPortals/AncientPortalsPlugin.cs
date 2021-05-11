// AncientPortals
// a Valheim mod skeleton using Jötunn
// 
// File:    AncientPortals.cs
// Project: AncientPortals

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using UnityEngine;

namespace AncientPortals
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class AncientPortals : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.jotunnmodstub";
        public const string PluginName = "AncientPortals";
        public const string PluginVersion = "0.0.1";
        private GameObject ancientPortalPrefab;

        private void Awake()
        {
            // Do all your init stuff here
            // Acceptable value ranges can be defined to allow configuration via a slider in the BepInEx ConfigurationManager: https://github.com/BepInEx/BepInEx.ConfigurationManager
            Config.Bind<int>("World Generation", "Number of portal pairs", 20, new ConfigDescription("This is an example config, using a range limitation for ConfigurationManager", new AcceptableValueRange<int>(0, 100)));
             
            ItemManager.OnVanillaItemsAvailable += AddCustomPrefabs;

        }

        private void AddCustomPrefabs()
        {
            ancientPortalPrefab = PrefabManager.Instance.CreateClonedPrefab("AncientPortal", "portal_wood");

            

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