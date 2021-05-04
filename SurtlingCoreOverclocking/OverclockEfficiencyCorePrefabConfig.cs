
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Utils;
using UnityEngine;

namespace SurtlingCoreOverclocking
{
    public partial class SurtlingCoreOverclockingMod
    {
        class OverclockEfficiencyCorePrefabConfig : CustomItem
        {
            private readonly Sprite sprite;
            private ItemDrop.ItemData.SharedData sharedData;

            public OverclockEfficiencyCorePrefabConfig() : base(SurtlingCoreOverclocking.oldEfficiencyCoreKey, "SurtlingCore")
            {
                Texture2D texture = AssetUtils.LoadTexture(SurtlingCoreOverclockingMod.GetAssetPath("icons/efficiency_core.png"));
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
                Recipe = new CustomRecipe(new RecipeConfig()
                {
                    Item = SurtlingCoreOverclocking.oldEfficiencyCoreKey,
                    CraftingStation = "forge",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig() { Item = "SurtlingCore", Amount = 1 },
                        new RequirementConfig() { Item = "Guck", Amount = 1 },
                        new RequirementConfig() { Item = "Ruby", Amount = 1 }
                    }
                });
            }

            public void PrefabCreated()
            {
                Debug.Log("Configuring item drop for OverclockEfficiencyCore");

                SurtlingCoreOverclocking.dropTable["$" + SurtlingCoreOverclocking.efficiencyCoreKey] = ItemDrop;
                sharedData = ItemDrop.m_itemData.m_shared;

                sharedData.m_name = "$" + SurtlingCoreOverclocking.efficiencyCoreKey;
                sharedData.m_description = "$" + SurtlingCoreOverclocking.efficiencyCoreKey + "_description";
                sharedData.m_icons[0] = sprite;
            }

            private string descriptionTemplate;

            public void UpdateDescription()
            {
                if (descriptionTemplate == null)
                {
                    descriptionTemplate = Localization.instance.Localize("$" + SurtlingCoreOverclocking.efficiencyCoreKey + "_description");
                }
                Localization.instance.AddWord(
                    SurtlingCoreOverclocking.efficiencyCoreKey + "_description",
                    InsertWords(descriptionTemplate,
                         SurtlingCoreOverclocking.GetPercentageString(SurtlingCoreOverclocking.m_efficiencyCoreEfficiencyBonus.Value),
                         SurtlingCoreOverclocking.GetPercentageString(SurtlingCoreOverclocking.m_efficiencyCoreSpeedPenalty.Value)
                    ) 
                );
            }
             
        }
    }

}
