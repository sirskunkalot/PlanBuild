using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Utils;
using JotunnLib.Entities;
using JotunnLib.Managers;
using JotunnLib.Utils;
using UnityEngine;

namespace PlanBuild
{
    class PlanCrystalPrefabConfig : CustomItem
    {
        public static ManualLogSource logger;

        public const string prefabName = "PlanCrystal";
        private Recipe recipe; 
        private ItemDrop.ItemData itemData;
        private ItemDrop.ItemData.SharedData sharedData;
        private const string localizationName = "plan_crystal";
        private string iconPath = "icons/plan_crystal.png";
        public static GameObject startPlanCrystalEffectPrefab;
        public static GameObject stopPlanCrystalEffectPrefab;

        public PlanCrystalPrefabConfig() : base(prefabName, "Ruby")
        {
            
        } 

        public void PrefabCreated()
        {
            logger.LogDebug("Configuring item drop for PlanCrystal"); 
            itemData = ItemDrop.m_itemData;
            
            ShaderHelper.UpdateTextures(itemData.m_dropPrefab, ShaderHelper.ShaderState.Supported);

            sharedData = itemData.m_shared;
             
            sharedData.m_name = "$item_" + localizationName;
            sharedData.m_description = "$item_" + localizationName + "_description";
            Texture2D texture = AssetUtils.LoadTexture(PlanBuild.GetAssetPath(iconPath));
            StatusEffect statusEffect = ScriptableObject.CreateInstance(typeof(StatusEffect)) as StatusEffect;
            statusEffect.m_icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            statusEffect.m_startMessageType = MessageHud.MessageType.Center;
            statusEffect.m_startMessage = "$message_plan_crystal_start";
            statusEffect.m_stopMessageType = MessageHud.MessageType.Center;
            statusEffect.m_stopMessage = "$message_plan_crystal_stop";
            statusEffect.m_startEffects = new EffectList
            {
                m_effectPrefabs = new EffectList.EffectData[] {
                new EffectList.EffectData() {
                    m_enabled = true,
                    m_prefab = startPlanCrystalEffectPrefab,
                    m_attach = true
                }
            }
            };
            statusEffect.m_stopEffects = new EffectList
            {
                m_effectPrefabs = new EffectList.EffectData[] {
                new EffectList.EffectData() {
                    m_enabled = true,
                    m_prefab = stopPlanCrystalEffectPrefab,
                    m_attach = true
                }
            }
            };
            sharedData.m_equipStatusEffect = statusEffect;
            sharedData.m_itemType = ItemDrop.ItemData.ItemType.Utility;
            sharedData.m_maxStackSize = 1;
            sharedData.m_centerCamera = true;
            if (texture == null)
            {
                logger.LogWarning($"planHammer icon not found at {iconPath}");
            }
            else
            {
                sharedData.m_icons[0] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
            }
            sharedData.m_maxQuality = 1; 
            ObjectManager.Instance.RegisterItem(Prefab);
        }

        internal void RegisterRecipe()
        {
            recipe = new RecipeConfig()
            {
                Item = prefabName, 
                CraftingStation = "piece_workbench",
                Requirements = new PieceRequirementConfig[] {
                                       new PieceRequirementConfig()
                                       {
                                           Item = "Ruby",
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

    class StartPlanCrystalStatusEffect: MonoBehaviour
    {

        public void Awake()
        { 
            bool attachedPlayer = gameObject.GetComponent<ZNetView>().IsOwner(); 
            if(attachedPlayer)
            {
                PlanBuildMod.logger.LogDebug("Triggering real textures");
                PlanBuildMod.showRealTextures = true;
                PlanBuildMod.UpdateAllPlanPieceTextures();
            }
        }

    }

    class StartPlanCrystalStatusEffectPrefabConfig : PrefabConfig
    {
        private const string PrefabName = PlanCrystalPrefabConfig.prefabName + "StartEffect";

        public StartPlanCrystalStatusEffectPrefabConfig() : base(PrefabName, "vfx_blocked")
        {
        }

        public override void Register()
        {
            PlanCrystalPrefabConfig.startPlanCrystalEffectPrefab = Prefab; 
            Prefab.AddComponent<StartPlanCrystalStatusEffect>();
        }
    }


    class StopPlanCrystalStatusEffect : MonoBehaviour
    {

        public void Awake()
        { 
            bool attachedPlayer = gameObject.GetComponent<ZNetView>().IsOwner(); 
            if (attachedPlayer)
            {
                PlanBuildMod.logger.LogDebug("Removing real textures");
                PlanBuildMod.showRealTextures = false;
                PlanBuildMod.UpdateAllPlanPieceTextures();
            }
        }

    }

    class StopPlanCrystalStatusEffectPrefabConfig : PrefabConfig
    {
        private const string PrefabName = PlanCrystalPrefabConfig.prefabName + "StopEffect";

        public StopPlanCrystalStatusEffectPrefabConfig() : base(PrefabName, "vfx_blocked")
        {
        }

        public override void Register()
        {
            PlanCrystalPrefabConfig.stopPlanCrystalEffectPrefab = Prefab; 
            Prefab.AddComponent<StopPlanCrystalStatusEffect>();
        }
    }
}
