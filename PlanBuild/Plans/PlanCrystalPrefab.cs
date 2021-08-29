using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Utils;
using System;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace PlanBuild.Plans
{
    internal class PlanCrystalPrefab
    {
        private const string PrefabName = "PlanCrystal";
        private const string LocalizationName = "plan_crystal";

        internal static bool ShowRealTextures;

        private readonly Sprite CrystalIcon;
        private CustomItem PlanCrystalItem;

        public PlanCrystalPrefab(AssetBundle planbuildBundle)
        {
            CrystalIcon = planbuildBundle.LoadAsset<Sprite>("plan_crystal");
            ItemManager.OnVanillaItemsAvailable += CreatePlanCrytalItem;
            ItemManager.OnItemsRegistered += FixShader;
        }

        private void CreatePlanCrytalItem()
        {
            try
            {
                Logger.LogDebug("Creating PlanCrystal item");

                PlanCrystalItem = new CustomItem(PrefabName, "Ruby", new ItemConfig()
                {
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
                        },
                        new RequirementConfig()
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
                GameObject startEffectPrefab =
                    PrefabManager.Instance.CreateClonedPrefab(PrefabName + "StartEffect", "vfx_blocked");
                startEffectPrefab.AddComponent<StartPlanCrystalStatusEffect>();
                statusEffect.m_startEffects = new EffectList
                {
                    m_effectPrefabs = new EffectList.EffectData[]
                    {
                        new EffectList.EffectData()
                        {
                            m_enabled = true,
                            m_prefab = startEffectPrefab,
                            m_attach = true
                        }
                    }
                };
                GameObject stopEffectPrefab =
                    PrefabManager.Instance.CreateClonedPrefab(PrefabName + "StopEffect", "vfx_blocked");
                stopEffectPrefab.AddComponent<StopPlanCrystalStatusEffect>();
                statusEffect.m_stopEffects = new EffectList
                {
                    m_effectPrefabs = new EffectList.EffectData[]
                    {
                        new EffectList.EffectData()
                        {
                            m_enabled = true,
                            m_prefab = stopEffectPrefab,
                            m_attach = true
                        }
                    }
                };
                sharedData.m_equipStatusEffect = statusEffect;
                sharedData.m_itemType = ItemDrop.ItemData.ItemType.Utility;
                sharedData.m_maxStackSize = 1;
                sharedData.m_centerCamera = true;
                sharedData.m_maxQuality = 1;

                ItemManager.Instance.AddItem(PlanCrystalItem);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error caught while creating the PlanCrytal item: {ex}");
            }
            finally
            {
                ItemManager.OnVanillaItemsAvailable -= CreatePlanCrytalItem;
            }
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
                Logger.LogDebug("Triggering real textures");
#endif
                PlanCrystalPrefab.ShowRealTextures = true;
                PlanManager.Instance.UpdateAllPlanPieceTextures();
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
                Logger.LogDebug("Removing real textures");
#endif
                PlanCrystalPrefab.ShowRealTextures = false;
                PlanManager.Instance.UpdateAllPlanPieceTextures();
            }
        }
    }
}