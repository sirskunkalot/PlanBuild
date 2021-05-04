using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Utils;
using UnityEngine;

namespace SurtlingCoreOverclocking
{
    public partial class SurtlingCoreOverclockingMod
    {
        class OverclockCoreSlotPrefabConfig : CustomItem
        {
            private readonly Sprite sprite;
            private ItemDrop.ItemData.SharedData sharedData;

            public OverclockCoreSlotPrefabConfig() : base(SurtlingCoreOverclocking.oldCoreSlotKey, "SurtlingCore")
            {
                Texture2D texture = AssetUtils.LoadTexture(SurtlingCoreOverclockingMod.GetAssetPath("icons/core_slot.png"));
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
                Recipe = new CustomRecipe(new RecipeConfig()
                {
                    Item = SurtlingCoreOverclocking.oldCoreSlotKey,
                    CraftingStation = "piece_artisanstation",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig()
                            {
                                Item = "SurtlingCore",
                                Amount = 1
                            },
                            new RequirementConfig()
                            {
                                Item = "YmirRemains",
                                Amount = 1
                            },
                                new RequirementConfig()
                            {
                                Item = "Crystal",
                                Amount = 1
                            }
                    }
                });
            }

            public void PrefabCreated()
            {
                SurtlingCoreOverclocking.dropTable["$" + SurtlingCoreOverclocking.coreSlotKey] = ItemDrop;
                sharedData = ItemDrop.m_itemData.m_shared;

                sharedData.m_name = "$" + SurtlingCoreOverclocking.coreSlotKey;
                sharedData.m_description = "$" + SurtlingCoreOverclocking.coreSlotKey + "_description";
                sharedData.m_icons[0] = sprite;
            }

            private string descriptionTemplate;

            public void UpdateDescription()
            {
                if (descriptionTemplate == null)
                {
                    descriptionTemplate = Localization.instance.Localize("$" + SurtlingCoreOverclocking.coreSlotKey + "_description");
                }
                Localization.instance.AddWord(
                    SurtlingCoreOverclocking.coreSlotKey + "_description",
                    InsertWords(descriptionTemplate,
                        SurtlingCoreOverclocking.m_maxAdditionalOverclockCores.Value.ToString()
                    )
                );
            }
        }

    }

}
