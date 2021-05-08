using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlanBuild.PlanBuild;

namespace PlanBuild
{
    public class PlanHammerPrefabConfig : CustomItem
    {
        public const string planHammerName = "PlanHammer";
        private const string localizationName = "plan_hammer";
        private const string iconPath = "icons/plan_hammer.png";
        public const string pieceTableName = "_planHammerPieceTable";
        public const string itemName = "$item_" + localizationName;
        public ItemDrop.ItemData itemData;
        private ItemDrop.ItemData.SharedData sharedData;
        public GameObject prefab;

        public PlanHammerPrefabConfig() : base(planHammerName, "Hammer")
        { 
            
        }

        public void Register()
        { 
            itemData = ItemDrop.m_itemData;
            itemData.m_shared.m_buildPieces = PieceManager.Instance.GetPieceTable(pieceTableName);
            sharedData = itemData.m_shared;

            sharedData.m_name = itemName;
            sharedData.m_description = "$item_" + localizationName + "_description";
            sharedData.m_useDurability = false;
            sharedData.m_durabilityDrain = 0f;
            sharedData.m_useDurabilityDrain = 0f;
            
            Texture2D texture = AssetUtils.LoadTexture(GetAssetPath(iconPath));
            if (texture == null)
            {
                logger.LogWarning($"PlanHammer icon not found at {iconPath}");
            }
            else
            {
                sharedData.m_icons[0] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
            }
            sharedData.m_maxQuality = 1;
        }

        public void PrefabCreated()
        {
            prefab = ItemDrop.m_itemData.m_dropPrefab;
            ShaderHelper.UpdateTextures(prefab, ShaderHelper.ShaderState.Supported);
           
        }
          

    }
}
