using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Utils;
using PlanBuild.Utils;
using UnityEngine;

namespace PlanBuild.Plans
{
    internal class PlanCrystalPrefab
    {
        public const string PrefabName = "PlanCrystal";
        private const string LocalizationName = "plan_crystal";
        public static GameObject StartPlanCrystalEffectPrefab;
        public static GameObject StopPlanCrystalEffectPrefab;
        private readonly Sprite CrystalIcon;
        private CustomItem PlanCrystalItem;

        public PlanCrystalPrefab(AssetBundle planbuildBundle)
        {
            CrystalIcon = planbuildBundle.LoadAsset<Sprite>("plan_crystal");
        }

        public CustomItem Create()
        {
            Jotunn.Logger.LogDebug("Creating PlanCrystal item");

            PlanCrystalItem = new CustomItem(PrefabName, "Ruby", new ItemConfig() {
                Name = $"$item_{LocalizationName}",
                Description = $"$item_{LocalizationName}_description",
                Icons = new Sprite[]
                {
                    CrystalIcon
                },
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
            ItemDrop.ItemData.SharedData sharedData = PlanCrystalItem.ItemDrop.m_itemData.m_shared;
            StatusEffect statusEffect = ScriptableObject.CreateInstance(typeof(StatusEffect)) as StatusEffect;
            statusEffect.m_icon = CrystalIcon;
            statusEffect.m_startMessageType = MessageHud.MessageType.Center;
            statusEffect.m_startMessage = "$message_plan_crystal_start";
            statusEffect.m_stopMessageType = MessageHud.MessageType.Center;
            statusEffect.m_stopMessage = "$message_plan_crystal_stop";
            statusEffect.m_startEffects = new EffectList
            {
                m_effectPrefabs = new EffectList.EffectData[] {
                new EffectList.EffectData() {
                    m_enabled = true,
                    m_prefab = StartPlanCrystalEffectPrefab,
                    m_attach = true
                }
            }
            };
            statusEffect.m_stopEffects = new EffectList
            {
                m_effectPrefabs = new EffectList.EffectData[] {
                new EffectList.EffectData() {
                    m_enabled = true,
                    m_prefab = StopPlanCrystalEffectPrefab,
                    m_attach = true
                }
            }
            };
            sharedData.m_equipStatusEffect = statusEffect;
            sharedData.m_itemType = ItemDrop.ItemData.ItemType.Utility;
            sharedData.m_maxStackSize = 1;
            sharedData.m_centerCamera = true;
            sharedData.m_maxQuality = 1;

            return PlanCrystalItem;
        }

        public void FixShader()
        {
            ShaderHelper.UpdateTextures(PlanCrystalItem.ItemDrop.m_itemData.m_dropPrefab, ShaderHelper.ShaderState.Supported);
        }
    }

    public class StartPlanCrystalStatusEffect : MonoBehaviour
    {
        public void Awake()
        {
            bool attachedPlayer = gameObject.GetComponent<ZNetView>().IsOwner();
            if (attachedPlayer)
            {
#if DEBUG
                Jotunn.Logger.LogDebug("Triggering real textures");
#endif
                PlanBuildPlugin.ShowRealTextures = true;
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
                PlanBuildPlugin.ShowRealTextures = false;
                PlanBuildPlugin.UpdateAllPlanPieceTextures();
            }
        }
    }
}