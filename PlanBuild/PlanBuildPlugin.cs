// PlanBuild
// a Valheim mod skeleton using Jötunn
// 
// File:    PlanBuild.cs
// Project: PlanBuild

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints;
using PlanBuild.PlanBuild;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.0.11")]
    [BepInDependency(Patches.buildCameraGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Patches.craftFromContainersGUID, BepInDependency.DependencyFlags.SoftDependency)] 
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.2.0";
          
        public static PlanBuildPlugin Instance;
           
        public static ConfigEntry<bool> showAllPieces;
        public static ConfigEntry<bool> configTransparentGhostPlacement;
        public static ConfigEntry<bool> configBuildShare;
         
        internal PlanCrystalPrefab planCrystalPrefab;
        internal BlueprintRunePrefab blueprintRunePrefab;

        internal static bool showRealTextures;
        internal GameObject hammerPrefab;
        internal ItemDrop hammerPrefabItemDrop;

        public static readonly Dictionary<string, PlanPiecePrefab> planPiecePrefabs = new Dictionary<string, PlanPiecePrefab>();

        public void Awake()
        {
            Instance = this;

            // Init Blueprints

            Assembly assembly = typeof(PlanBuildPlugin).Assembly;
            AssetBundle blueprintsBundle = AssetUtils.LoadAssetBundleFromResources("blueprints", assembly); 

            blueprintRunePrefab = new BlueprintRunePrefab(blueprintsBundle);
            blueprintsBundle.Unload(false);
            BlueprintManager.Instance.Init();

            AssetBundle planbuildBundle = AssetUtils.LoadAssetBundleFromResources("planbuild", assembly);
            planTotemPrefab = new PlanTotemPrefab(planbuildBundle);
            planbuildBundle.Unload(false);

            // Init Shader
            ShaderHelper.planShader = Shader.Find("Lux Lit Particles/ Bumped");

            // Harmony patching
            Patches.Apply();

            // Configs
            showAllPieces = base.Config.Bind<bool>("General", "Plan unknown pieces", false, new ConfigDescription("Show all plans, even for pieces you don't know yet"));
            PlanTotem.radiusConfig = base.Config.Bind<float>("General", "Plan totem build radius", 30f, new ConfigDescription("Build radius of the Plan totem"));
            configBuildShare = base.Config.Bind<bool>("BuildShare", "Place as planned pieces", false, new ConfigDescription("Place .vbuild as planned pieces instead", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            configTransparentGhostPlacement = base.Config.Bind<bool>("Visual", "Transparent Ghost Placement", false, new ConfigDescription("Apply plan shader to ghost placement (currently placing piece)"));

            ShaderHelper.unsupportedColorConfig = base.Config.Bind<Color>("Visual", "Unsupported color", new Color(1f, 1f, 1f, 0.1f), new ConfigDescription("Color of unsupported plan pieces"));
            ShaderHelper.supportedPlanColorConfig = base.Config.Bind<Color>("Visual", "Supported color", new Color(1f, 1f, 1f, 0.5f), new ConfigDescription("Color of supported plan pieces"));
            ShaderHelper.transparencyConfig = base.Config.Bind<float>("Visual", "Transparency", 0.30f, new ConfigDescription("Additional transparency", new AcceptableValueRange<float>(0f, 1f)));

            ShaderHelper.unsupportedColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.supportedPlanColorConfig.SettingChanged += UpdateAllPlanPieceTextures;
            ShaderHelper.transparencyConfig.SettingChanged += UpdateAllPlanPieceTextures;

            BlueprintManager.allowDirectBuildConfig = base.Config.Bind("Blueprint Rune", "Allow direct build", false,
                new ConfigDescription("Allow placement of blueprints without materials", null, new object[] { new ConfigurationManagerAttributes() { IsAdminOnly = true } }));

            BlueprintManager.rayDistanceConfig = base.Config.Bind<float>("Blueprint Rune", "Place distance", 20f, 
                new ConfigDescription("Place distance while using the Blueprint Rune", new AcceptableValueRange<float>(0f, 1f)));

            PlanTotemPrefab.glowColorConfig = base.Config.Bind<Color>("Visual", "Plan totem glow color", Color.cyan, new ConfigDescription("Color of the glowing lines on the Plan totem"));

            PlanTotem.radiusConfig.SettingChanged += UpdatePlanTotem;
            PlanTotemPrefab.glowColorConfig.SettingChanged += UpdatePlanTotem;
             
            showAllPieces.SettingChanged += UpdateKnownRecipes;
             
            // Hooks
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;
            
            PrefabManager.OnPrefabsRegistered += InitialScanHammer;
            ItemManager.OnItemsRegistered += OnItemsRegistered;
            PieceManager.OnPiecesRegistered += LateScanHammer;
           
             
        }

        private void UpdatePlanTotem(object sender, EventArgs e)
        {
            planTotemPrefab.SettingsUpdated();
            foreach(PlanTotem planTotem in PlanTotem.m_allPlanTotems)
            {
                PlanTotemPrefab.UpdateGlowColor(planTotem.gameObject);
            }
        }

        public void OnDestroy()
        {
            Patches.Remove();
        }
         
        private void AddClonedItems()
        {
            try
            { 
                PlanCrystalPrefab.startPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.prefabName + "StartEffect", "vfx_blocked");
                PlanCrystalPrefab.startPlanCrystalEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                PlanCrystalPrefab.stopPlanCrystalEffectPrefab = PrefabManager.Instance.CreateClonedPrefab(PlanCrystalPrefab.prefabName + "StopEffect", "vfx_blocked");
                PlanCrystalPrefab.stopPlanCrystalEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();
                 
                planCrystalPrefab = new PlanCrystalPrefab();
                planCrystalPrefab.Setup();
                ItemManager.Instance.AddItem(planCrystalPrefab);


            }
            finally
            {
                ItemManager.OnVanillaItemsAvailable -= AddClonedItems;
            }
        }

        private void OnItemsRegistered()
        { 
            planCrystalPrefab.FixShader();
            hammerPrefab = PrefabManager.Instance.GetPrefab("Hammer");
            hammerPrefabItemDrop = hammerPrefab.GetComponent<ItemDrop>();
        }

        internal bool addedHammer = false;
        private PlanTotemPrefab planTotemPrefab;

        internal void InitialScanHammer()
        {
            try
            {
                this.ScanHammer(false);
            }
            finally
            {
                PieceManager.OnPiecesRegistered -= InitialScanHammer;
            }
        }

        internal void LateScanHammer()
        {
            ScanHammer(true);
        }

        internal bool ScanHammer(bool lateAdd)
        {
            Jotunn.Logger.LogDebug("Scanning Hammer PieceTable for Pieces");
            PieceTable hammerPieceTable = PieceManager.Instance.GetPieceTable("Hammer");
            if (!hammerPieceTable)
            {
                return false;
            }
            bool addedPiece = false;
            foreach (GameObject piecePrefab in hammerPieceTable.m_pieces)
            {
                if (!piecePrefab)
                {
                    Jotunn.Logger.LogWarning("Invalid prefab in Hammer PieceTable");
                    continue;
                }
                Piece piece = piecePrefab.GetComponent<Piece>();

                if (piece.name == "piece_repair")
                {
                    if (!addedHammer)
                    {
                        PieceTable planHammerPieceTable = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
                        if (planHammerPieceTable != null)
                        {
                            planHammerPieceTable.m_pieces.Add(piecePrefab);
                            addedHammer = true;
                        }
                    }
                    continue;
                }
                if (planPiecePrefabs.ContainsKey(piece.name))
                {
                    continue;
                }
                if (!piece.m_enabled
                    || piece.GetComponent<Ship>() != null
                    || piece.GetComponent<Plant>() != null
                    || piece.GetComponent<TerrainModifier>() != null
                    || piece.m_resources.Length == 0)
                {
                    continue;
                }
                PlanPiecePrefab planPiece = new PlanPiecePrefab(piece);
                PieceManager.Instance.AddPiece(planPiece);
                planPiecePrefabs.Add(piece.name, planPiece);
                PrefabManager.Instance.RegisterToZNetScene(planPiece.PiecePrefab);
                if (lateAdd)
                {
                    PieceTable pieceTable = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
                    if (!pieceTable.m_pieces.Contains(planPiece.PiecePrefab))
                    {
                        pieceTable.m_pieces.Add(planPiece.PiecePrefab);
                        addedPiece = true;
                    }
                }
            }
            if(addedPiece)
            {
                MoveBlueprintsToEnd(PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName));
            }
            return addedPiece;
        }

        private void MoveBlueprintsToEnd(PieceTable hammerPieceTable)
        { 
            List<GameObject> blueprints = hammerPieceTable.m_pieces.FindAll(piece => piece.name.StartsWith(Blueprint.BlueprintPrefabName) || piece.name.StartsWith(BlueprintRunePrefab.MakeBlueprintName));
            hammerPieceTable.m_pieces.RemoveAll(blueprints.Contains);
            hammerPieceTable.m_pieces.AddRange(blueprints); 
        }

        private void UpdateKnownRecipes(object sender, EventArgs e)
        {
            UpdateKnownRecipes();
        }

        private void UpdateKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (!showAllPieces.Value)
            {
                foreach (PlanPiecePrefab planPieceConfig in planPiecePrefabs.Values)
                {
                    if (!player.HaveRequirements(planPieceConfig.originalPiece, Player.RequirementMode.IsKnown))
                    {
#if DEBUG
                        Jotunn.Logger.LogInfo("Removing planned piece from m_knownRecipes: " + planPieceConfig.Piece.m_name);
#endif
                        player.m_knownRecipes.Remove(planPieceConfig.Piece.m_name);
                    }
#if DEBUG
                    else
                    {
                        Jotunn.Logger.LogDebug("Player knows about " + planPieceConfig.originalPiece.m_name);
                    }
#endif
                }
            }
            player.UpdateKnownRecipesList();
            PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName)
                .UpdateAvailable(player.m_knownRecipes, player, true, false);
        }

        private string[] GetAcceptableKeyCodes()
        {
            Array keyCodes = Enum.GetValues(typeof(KeyCode));
            int i = 0;
            string[] acceptable = new string[keyCodes.Length];
            foreach (System.Object keyCode in keyCodes)
            {
                acceptable[i++] = keyCode.ToString();
            }
            return acceptable;
        }

        private void UpdateAllPlanPieceTextures(object sender, EventArgs e)
        {
            UpdateAllPlanPieceTextures();
        }

        public static void UpdateAllPlanPieceTextures()
        {
            if (showRealTextures 
                && Player.m_localPlayer.m_placementGhost
                && Player.m_localPlayer.m_placementGhost.name.StartsWith(Blueprint.BlueprintPrefabName))
            {
                ShaderHelper.UpdateTextures(Player.m_localPlayer.m_placementGhost, ShaderHelper.ShaderState.Skuld);
            }
            foreach (PlanPiece planPiece in Object.FindObjectsOfType<PlanPiece>())
            {
                planPiece.UpdateTextures();
            }
        } 

        private bool CheckInput()
        {
            return (!Chat.instance || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !InventoryGui.IsVisible()
                && !StoreGui.IsVisible()
                && !Menu.IsVisible()
                && !Minimap.IsOpen()
                && !Player.m_localPlayer.InCutscene();
        }
         
        public static string GetAssetPath(string assetName, bool isDirectory = false)
        {
            string text = Path.Combine(BepInEx.Paths.PluginPath, PluginName, assetName);
            if (isDirectory)
            {
                if (!Directory.Exists(text))
                {
                    Assembly assembly = typeof(PlanBuildPlugin).Assembly;
                    text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                    if (!Directory.Exists(text))
                    {
                        Jotunn.Logger.LogWarning($"Could not find directory ({assetName}).");
                        return null;
                    }
                }
                return text;
            }
            if (!File.Exists(text))
            {
                Assembly assembly = typeof(PlanBuildPlugin).Assembly;
                text = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                if (!File.Exists(text))
                {
                    Jotunn.Logger.LogWarning($"Could not find asset ({assetName}).");
                    return null;
                }
            }
            return text;
        }

    }
}