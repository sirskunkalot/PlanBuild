using HarmonyLib;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.ModCompat
{
    internal class PatcherAzuCraftyBoxes
    {
        internal class AzuCraftyBoxesInventory : PlanPiece.IInventory
        {
            private AzuCraftyBoxes.IContainers.IContainer container;

            // AzuCraftyBoxes uses the prefab name for access filtering, so we need to get those from resource names.
            // Not ideal, but it works.
            internal static Dictionary<string, string> resource_prefab_mapping = new Dictionary<string, string>();
            internal static string GetResourcePrefabName(string resourceName) {
                string prefabName;
                if (!resource_prefab_mapping.TryGetValue(resourceName, out prefabName)) {
                    prefabName = null;
                    foreach (GameObject prefab in ObjectDB.instance.m_items)
                    {
                        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                        if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared.m_name == resourceName)
                        {
                            prefabName = global::Utils.GetPrefabName(prefab);
                            break;
                        }
                    }
                    resource_prefab_mapping.Add(resourceName, prefabName);
                }
                return prefabName;
            }

            public AzuCraftyBoxesInventory(AzuCraftyBoxes.IContainers.IContainer container) {
                this.container = container;
            }
            public int CountItems(string resourceName)
            {
                return container.ItemCount(resourceName);
            }

            public bool HaveItem(string resourceName)
            {
                string prefabName = GetResourcePrefabName(resourceName);
                return AzuCraftyBoxes.API.CanItemBePulled(container.GetPrefabName(), prefabName) && container.ItemCount(resourceName) > 0;
            }

            public void RemoveItem(string resourceName, int amount)
            {
                container.RemoveItem(resourceName, amount);
            }
        }

        [HarmonyPatch(typeof(PlanPiece), "GetInventories")]
        [HarmonyPostfix]
        private static void PlanPiece_GetInventories_Postfix(Humanoid player, ref List<PlanPiece.IInventory> __result)
        {
            // Check if the plugin is active, and append any nearby container inventories to the list
            if ((int)AzuCraftyBoxes.AzuCraftyBoxesPlugin.ModEnabled.Value == 1) {
                foreach (AzuCraftyBoxes.IContainers.IContainer container in AzuCraftyBoxes.API.GetNearbyContainers(player, AzuCraftyBoxes.AzuCraftyBoxesPlugin.mRange.Value)) {
                    __result.Add(new AzuCraftyBoxesInventory(container));
                }
            }
        }
    }
}