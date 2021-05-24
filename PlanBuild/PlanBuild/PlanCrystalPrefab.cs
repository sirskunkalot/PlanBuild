using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities; 
using Jotunn.Utils; 
using UnityEngine;

namespace PlanBuild.Plans
{
    class PlanCrystalPrefab : CustomItem
    {
        public const string prefabName = "PlanCrystal";  
        private const string localizationName = "plan_crystal";
        private string iconPath = "icons/plan_crystal.png";
        public static GameObject startPlanCrystalEffectPrefab;
        public static GameObject stopPlanCrystalEffectPrefab;

        public PlanCrystalPrefab() : base(prefabName, "Ruby")
        {
            
            Recipe = new CustomRecipe(new RecipeConfig()
            {
                Item = prefabName,
                Name = "$item_" + localizationName,
                CraftingStation = "piece_workbench",
                Requirements = new RequirementConfig[] 
                {
                    new RequirementConfig()
                    {
                        Item = "Ruby",
                        Amount = 1
                    } ,
                    new  RequirementConfig()
                    {
                        Item = "GreydwarfEye",
                        Amount = 1
                    }
                }
            });
        }
         
        public void Setup()
        {
            Jotunn.Logger.LogDebug("Configuring item drop for PlanCrystal");

            ItemDrop.ItemData.SharedData sharedData = ItemDrop.m_itemData.m_shared;
            sharedData.m_name = "$item_" + localizationName;
            sharedData.m_description = "$item_" + localizationName + "_description";
            Texture2D texture = AssetUtils.LoadTexture(PlanBuildPlugin.GetAssetPath(iconPath));
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
                Jotunn.Logger.LogWarning($"planHammer icon not found at {iconPath}");
            }
            else
            {
                sharedData.m_icons = new Sprite[] { Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero) };
            }
            sharedData.m_maxQuality = 1;
        }

         public void FixShader()
        {
           
            ShaderHelper.UpdateTextures(ItemDrop.m_itemData.m_dropPrefab, ShaderHelper.ShaderState.Supported);

        }
    }

   public  class StartPlanCrystalStatusEffect : MonoBehaviour
    {

        public void Awake()
        {
            bool attachedPlayer = gameObject.GetComponent<ZNetView>().IsOwner();
            if (attachedPlayer)
            {
#if DEBUG
                Jotunn.Logger.LogDebug("Triggering real textures");
#endif
                PlanBuildPlugin.showRealTextures = true;
                PlanBuildPlugin.UpdateAllPlanPieceTextures();
            }
        }

    }
     


    public class StopPlanCrystalStatusEffect : MonoBehaviour
    {

        public void Awake()
        {
            bool attachedPlayer = gameObject.GetComponent<ZNetView>().IsOwner();
            if (attachedPlayer)
            {
#if DEBUG
                Jotunn.Logger.LogDebug("Removing real textures");
#endif
                PlanBuildPlugin.showRealTextures = false;
                PlanBuildPlugin.UpdateAllPlanPieceTextures();
            }
        }

    }
     
}
