// AirShips
// a Valheim mod skeleton using Jötunn
// 
// File:    AirShips.cs
// Project: AirShips

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using UnityEngine;

namespace AirShips
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("BepIn.Sarcen.ValheimRAFT")]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class AirShips : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.AirShips";
        public const string PluginName = "AirShips";
        public const string PluginVersion = "0.0.1";

        private void Awake()
        {
            ItemManager.OnVanillaItemsAvailable += AddCustomPrefabs;

        }

        private void AddCustomPrefabs()
        {
            var airshipBasePrefab = PrefabManager.Instance.CreateClonedPrefab("AirShipBase", "TrophySGolem");
            Transform modelTransform = airshipBasePrefab.transform.Find("attach/default").transform;
            modelTransform.eulerAngles.z = 180;
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