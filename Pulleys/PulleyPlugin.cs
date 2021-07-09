// Pulleys
// a Valheim mod skeleton using Jötunn
// 
// File:    Pulleys.cs
// Project: Pulleys

using BepInEx;
using BepInEx.Configuration; 
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pulleys
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PulleyPlugin : BaseUnityPlugin
    {
        public const int MoveableBaseRootLayer = 29;
        public const string PluginGUID = "marcopogo.Pulleys";
        public const string PluginName = "Pulleys";
        public const string PluginVersion = "0.0.1";
           
        public void Awake()
        { 
            PulleyManager.Instance.Init();
			StartCoroutine("UpdateWear");
        }

		internal IEnumerator UpdateWear()
		{
			while (true)
			{
				foreach(MoveableBaseSync moveableBaseSync in MoveableBaseSync.GetAllMoveableBaseSyncs())
                {
                    moveableBaseSync.UpdateWear();
					yield return null;
                } 
				yield return new WaitForSeconds(5f);
			}
		}
	}
}