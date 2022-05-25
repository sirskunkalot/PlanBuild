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

        public static bool ShowRealTextures;

        private static Sprite CrystalIcon;
        private static CustomItem PlanCrystalItem;

        public static void Create(AssetBundle planbuildBundle)
        {
            CrystalIcon = planbuildBundle.LoadAsset<Sprite>("plan_crystal");
            PrefabManager.OnVanillaPrefabsAvailable += CreatePlanCrystalItem;
            ItemManager.OnItemsRegistered += FixShader;
        }

        private static void CreatePlanCrystalItem()
        {
            try
            {
                Logger.LogDebug("Creating PlanCrystal item");

                PlanCrystalItem = new CustomItem(PrefabName, "Ruby", new ItemConfig
                {
                    Name = $"$item_{LocalizationName}",
                    Description = $"$item_{LocalizationName}_description",
                    Icons = new []
                    {
                        CrystalIcon
                    },
                    CraftingStation = "piece_workbench",
                    Requirements = new []
                    {
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
                PrefabManager.OnVanillaPrefabsAvailable -= CreatePlanCrystalItem;
            }
        }

        private static void FixShader()
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
                PlanManager.UpdateAllPlanPieceTextures();
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
                PlanManager.UpdateAllPlanPieceTextures();
            }
        }
    }
}