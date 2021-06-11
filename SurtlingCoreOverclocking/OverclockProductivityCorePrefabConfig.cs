using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Utils;
using UnityEngine;

namespace SurtlingCoreOverclocking
{
    public partial class SurtlingCoreOverclockingMod
    {
        private class OverclockProductivityCorePrefabConfig : CustomItem
        {
            private readonly Sprite sprite;
            private ItemDrop.ItemData.SharedData sharedData;

            public OverclockProductivityCorePrefabConfig() : base(SurtlingCoreOverclocking.oldProductivityCoreKey, "SurtlingCore")
            {
                Texture2D texture = AssetUtils.LoadTexture(SurtlingCoreOverclockingMod.GetAssetPath("icons/productivity_core.png"));
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
                Recipe = new CustomRecipe(new RecipeConfig()
                {
                    Item = SurtlingCoreOverclocking.oldProductivityCoreKey,
                    CraftingStation = "forge",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig() { Item = "SurtlingCore", Amount = 1 },
                        new RequirementConfig() { Item = "SharpeningStone", Amount = 2 }
                    }
                });
            }

            public void PrefabCreated()
            {
                Debug.Log("Configuring item drop for OverclockProductivityCore");

                SurtlingCoreOverclocking.dropTable["$" + SurtlingCoreOverclocking.productivityCoreKey] = ItemDrop;
                sharedData = ItemDrop.m_itemData.m_shared;

                sharedData.m_name = "$" + SurtlingCoreOverclocking.productivityCoreKey;
                sharedData.m_description = "$" + SurtlingCoreOverclocking.productivityCoreKey + "_description";
                sharedData.m_icons[0] = sprite;
            }

            private string descriptionTemplate;

            public void UpdateDescription()
            {
                if (descriptionTemplate == null)
                {
                    descriptionTemplate = Localization.instance.Localize("$" + SurtlingCoreOverclocking.productivityCoreKey + "_description");
                }
                Localization.instance.AddWord(
                    SurtlingCoreOverclocking.productivityCoreKey + "_description",
                    InsertWords(descriptionTemplate,
                         SurtlingCoreOverclocking.GetPercentageString(SurtlingCoreOverclocking.m_productivityCoreProductivityBonus.Value),
                         SurtlingCoreOverclocking.GetPercentageString(SurtlingCoreOverclocking.m_productivityCoreSpeedPenalty.Value),
                         SurtlingCoreOverclocking.GetPercentageString(SurtlingCoreOverclocking.m_productivityCoreEfficiencyPenalty.Value)
                    )
                );
            }
        }
    }
}