// PickThatUp
// a Valheim mod skeleton using Jötunn
// 
// File:    PickThatUp.cs
// Project: PickThatUp

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using UnityEngine;

namespace PickThatUp
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PickThatUp : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.jotunnmodstub";
        public const string PluginName = "PickThatUp";
        public const string PluginVersion = "0.0.1";

        private void Awake()
        {
            // Do all your init stuff here
            // Acceptable value ranges can be defined to allow configuration via a slider in the BepInEx ConfigurationManager: https://github.com/BepInEx/BepInEx.ConfigurationManager
            Config.Bind<int>("Main Section", "Example configuration integer", 1, new ConfigDescription("This is an example config, using a range limitation for ConfigurationManager", new AcceptableValueRange<int>(0, 100)));

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ModStub has landed");
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