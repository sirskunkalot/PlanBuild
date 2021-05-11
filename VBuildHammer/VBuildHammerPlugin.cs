// VBuildHammer
// a Valheim mod skeleton using Jötunn
// 
// File:    VBuildHammer.cs
// Project: VBuildHammer

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace VBuildHammer
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class VBuildHammer : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.VBuildHammer";
        public const string PluginName = "VBuildHammer";
        public const string PluginVersion = "0.0.1";

        private void Awake()
        {

            ItemManager.OnVanillaItemsAvailable += AddCustomPrefabs;
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