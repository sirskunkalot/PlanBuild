using HarmonyLib;
using JotunnLib.Entities;
using JotunnLib.Managers;
using JotunnLib.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlanBuild.PlanBuildMod;

namespace PlanBuild
{
    public class PlanHammerPrefabConfig : PrefabConfig
    {
        public const string planHammerName = "PlanHammer";
        private const string localizationName = "plan_hammer";
        private const string iconPath = "icons/plan_hammer.png";
        public const string pieceTableName = "_planHammerPieceTable";
        private Recipe recipe;
        public static ItemDrop itemDrop; 
        public static PieceTable planPieceTable;
        public ItemDrop.ItemData itemData;
        private ItemDrop.ItemData.SharedData sharedData;

        public PlanHammerPrefabConfig() : base(planHammerName, "Hammer")
        {
            
        }
         
        public override void Register()
        {  
            ShaderHelper.UpdateTextures(Prefab, ShaderHelper.ShaderState.Supported);
        }

        public void RegisterItem()
        { 
            itemDrop = Prefab.GetComponent<ItemDrop>();

            itemData = itemDrop.m_itemData;
            sharedData = itemData.m_shared; 

            itemData.m_dropPrefab = Prefab;
            sharedData.m_name = "$item_" + localizationName;
            sharedData.m_description = "$item_" + localizationName + "_description";
            sharedData.m_useDurability = false;
            sharedData.m_durabilityDrain = 0f;
            sharedData.m_useDurabilityDrain = 0f;
            Texture2D texture = AssetUtils.LoadTexture(PlanBuildMod.GetAssetPath(iconPath));
            if (texture == null)
            {
                logger.LogWarning($"PlanHammer icon not found at {iconPath}");
            }
            else
            {
                sharedData.m_icons[0] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
            }
            sharedData.m_maxQuality = 1; 
            ObjectManager.Instance.RegisterItem(Prefab);
        }

        [HarmonyPatch(typeof(PieceManager), "RegisterPieceTable")]
        class PieceManager_RegisterPieceTable_Patch
        {
            static void Postfix(Dictionary<string, PieceTable> ___pieceTables)
            {
                if(planPieceTable != null)
                {
                    return;
                }
                planPieceTable = ___pieceTables[pieceTableName];
            }

        }
        public void RegisterPieceTable(GameObject repairRecipe, List<PlanPiecePrefabConfig> planPiecePrefabConfigs)
        {      
            planPieceTable.name = pieceTableName;
            planPieceTable.m_pieces = new List<GameObject>(planPiecePrefabConfigs.Count())
            {
                repairRecipe
            };
            foreach (PlanPiecePrefabConfig pieceConfig in planPiecePrefabConfigs)
            {
                //logger.LogDebug($"Adding planPiece {pieceConfig.Prefab} to PieceTable " + planPieceTable.name);
                planPieceTable.m_pieces.Add(pieceConfig.Prefab);
            }
        
            sharedData.m_buildPieces = planPieceTable;
         
          //  logger.LogInfo(DebugUtils.ToString(itemDrop));
        
        }

        internal void RegisterRecipe()
        {
            recipe = new RecipeConfig()
            {
                Item = planHammerName,
                
                CraftingStation = "piece_workbench",
                Requirements = new PieceRequirementConfig[] {
                                       new PieceRequirementConfig()
                                       {
                                           Item = "Hammer",
                                           Amount = 1
                                       } ,
                                        new PieceRequirementConfig()
                                       {
                                           Item = "GreydwarfEye",
                                           Amount = 1
                                       }
                                   }
            }.GetRecipe();
            ObjectManager.Instance.RegisterRecipe(recipe);
        }
    }
}
