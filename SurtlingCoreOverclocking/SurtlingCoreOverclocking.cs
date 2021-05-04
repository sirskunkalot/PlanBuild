using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurtlingCoreOverclocking
{
    class SurtlingCoreOverclocking : MonoBehaviour, Interactable, Hoverable
    {
        public const string oldSpeedCoreKey = "$overclockSpeedCore";
        public const string oldProductivityCoreKey = "$overclockProductivityCore";
        public const string oldEfficiencyCoreKey = "$overclockEfficiencyCore";
        public const string oldCoreSlotKey = "$overclockCoreSlot";

        public const string speedCoreKey = "item_overclock_core_speed";
        public const string productivityCoreKey = "item_overclock_core_efficiency";
        public const string efficiencyCoreKey = "item_overclock_core_productivity";
        public const string coreSlotKey = "item_overclock_core_slot";

        public const string noSlotsAvailable_addSlots = "message_overclock_core_no_slots_available";
        public const string noSlotsAvailable_maxSlots = "message_overclock_core_no_slots_available_max_reached";
        public const string maxSlotsReached = "message_overclock_core_max_slots_reached";

        private ZNetView m_nview;
        string m_hoverText = "";

        public static ConfigEntry<double> m_speedCoreSpeedMultiplier;
        public static ConfigEntry<double> m_speedCoreEfficiencyPenalty;

        public static ConfigEntry<double> m_efficiencyCoreEfficiencyBonus;
        public static ConfigEntry<double> m_efficiencyCoreSpeedPenalty;

        public static ConfigEntry<double> m_productivityCoreProductivityBonus;
        public static ConfigEntry<double> m_productivityCoreSpeedPenalty;
        public static ConfigEntry<double> m_productivityCoreEfficiencyPenalty;

        public static ConfigEntry<int> m_defaultMaxOverclockCores;
        public static ConfigEntry<int> m_maxAdditionalOverclockCores;

        public static Dictionary<string, ItemDrop> dropTable = new Dictionary<string, ItemDrop>();

        private float original_m_secPerProduct;

        public void Awake()
        {

            m_nview = GetComponent<ZNetView>();
            m_nview.Register<string>("AddCore", RPC_AddCore);
            m_nview.Register("DropCores", RPC_DropCores);

            InvokeRepeating("UpdateStatus", 1f, 1f);

            smelter = GetComponent<Smelter>();
            if (!smelter)
            {
                Debug.LogError("No smelter attached?! @ " + base.transform.position);

            }
            else
            {
                original_m_secPerProduct = smelter.m_secPerProduct;
            }
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                MigrateKeys();
                UpdateSmelterValues();
            }
        }

        private void MigrateKeys()
        {
            MigrateKey(oldCoreSlotKey, coreSlotKey);
            MigrateKey(oldSpeedCoreKey, speedCoreKey);
            MigrateKey(oldEfficiencyCoreKey, efficiencyCoreKey);
            MigrateKey(oldProductivityCoreKey, productivityCoreKey);
        }

        private void MigrateKey(string oldKey, string newKey)
        {
            int oldCount = GetCoreCount(oldKey);
            if(oldCount > 0)
            {
                int newCount = GetCoreCount("$" + newKey);
                newCount += oldCount;
                SetCoreCount("$" + newKey, newCount);
                SetCoreCount(oldKey, 0);
            }
        }

        private void RPC_DropCores(long sender)
        {
            if (m_nview.IsOwner())
            {
                Debug.Log("Drop all cores");

                foreach (string coreName in dropTable.Keys)
                {
                    DropCores(dropTable[coreName], GetCoreCount(coreName));
                    SetCoreCount(coreName, 0);
                }
                UpdateSmelterValues();
            }
        }

        private void DropCores(ItemDrop itemDrop, int amount)
        {
            while (amount > 0)
            {
                int amountToDrop = Math.Min(itemDrop.m_itemData.m_shared.m_maxStackSize, amount);
                Object.Instantiate(itemDrop.m_itemData.m_dropPrefab, smelter.m_outputPoint.position, smelter.m_outputPoint.rotation)
                    .GetComponent<ItemDrop>().m_itemData.m_stack = amountToDrop;
                amount -= amountToDrop;
            };
        }

        private int GetCoreCount(string coreName)
        {
            if (!this.m_nview.IsValid())
            {
                return 0;
            }
            return m_nview.GetZDO().GetInt("count_" + coreName);
        }

        private void SetCoreCount(string coreName, int count)
        {
            if (!this.m_nview.IsValid())
            {
                return;
            }
            m_nview.GetZDO().Set("count_" + coreName, count);
        }

        public string GetHoverName()
        {
            return GetComponent<Smelter>().m_name;
        }

        public string GetHoverText()
        {
            if (!m_nview.IsValid()
                || Player.m_localPlayer == null)
            {
                return "";
            }
            return Localization.instance.Localize(m_hoverText);
        }

        private void UpdateStatus()
        {
            UpdateHoverText();
        }

        private double GetSpeedMultiplier(int speedCores, int efficiciencyCores, int productivityCores)
        {
            double bonus = speedCores * m_speedCoreSpeedMultiplier.Value;
            double penalty = efficiciencyCores * m_efficiencyCoreSpeedPenalty.Value
                 + productivityCores * m_productivityCoreSpeedPenalty.Value;
            return (1.0 + bonus) * (1 / (1 + penalty));
        }

        private double GetEfficiencyMultiplier(int speedCores, int efficiciencyCores, int productivityCores)
        {
            double bonus = efficiciencyCores * m_efficiencyCoreEfficiencyBonus.Value;
            double penalty = speedCores * m_speedCoreEfficiencyPenalty.Value
                 + productivityCores * m_productivityCoreEfficiencyPenalty.Value;
            return (1.0 + bonus) * (1 / (1 + penalty));
        }

        private double GetProductivityMultiplier(int productivityCores)
        {
            double bonus = productivityCores * m_productivityCoreProductivityBonus.Value;
            return 1.0 + bonus;
        }
        private void UpdateHoverText()
        {
            int usedCoreSlots = GetUsedCoreSlotCount();
            if (usedCoreSlots == 0)
            {
                m_hoverText = "";
                return;
            }

            StringBuilder builder = new StringBuilder();

            builder.Append(GetHoverName() + "\n");

            int speedCoreCount = GetSpeedCoreCount();
            int efficiencyCoreCount = GetEfficiencyCoreCount();
            int productivityCoreCount = GetProductivityCoreCount();

            double speed = GetSpeedMultiplier(speedCoreCount, efficiencyCoreCount, productivityCoreCount);
            double productivity = GetProductivityMultiplier(productivityCoreCount);
            double efficiency = GetEfficiencyMultiplier(speedCoreCount, efficiencyCoreCount, productivityCoreCount);

            builder.Append(
                 "[<color=yellow><b>1-8</b></color>] Add core\n" +
                 "[Hold <color=yellow><b>$KEY_Use</b></color>] Remove cores\n");

            builder.Append("\n");

            int availableCoreSlots = GetTotalCoreSlotCount();
            int maxSlots = (m_defaultMaxOverclockCores.Value + m_maxAdditionalOverclockCores.Value);
            builder.Append("<color=grey>Overclock slots: " + usedCoreSlots + "/" + availableCoreSlots + "" + ((availableCoreSlots == maxSlots) ? " [Max]" : "") + "</color>\n" +
                           "<color=grey>Speed (" + speedCoreCount + "): </color><color=" + GetColor(speed) + ">" + GetPercentageString(speed) + "</color>\n");
            if (!IsKiln())
            {
                builder.Append("<color=grey>Efficiency (" + efficiencyCoreCount + ") </color><color=" + GetColor(efficiency) + ">" + GetPercentageString(efficiency) + "</color><color=grey> -> Fuel usage: </color><color=" + GetColor(efficiency) + ">" + GetPercentageString(1.0 / efficiency) + "</color>\n");
            }
            builder.Append("<color=grey>Productivity (" + productivityCoreCount + "): </color><color=" + GetColor(productivity) + ">" + GetPercentageString(productivity) + "</color>");
            m_hoverText = builder.ToString();
        }

        private string GetColor(double multiplier)
        {
            if (multiplier == 1)
            {
                return "grey";
            }
            else if (multiplier > 1)
            {
                return "green";
            }
            else
            {
                return "red";
            }
        }

        private void UpdateSmelterValues()
        {
            if(!this.m_nview.IsValid())
            {
                return;
            }
            Smelter smelter = GetComponent<Smelter>();

            float speedMultiplier = (float)GetSpeedMultiplier(GetSpeedCoreCount(), GetEfficiencyCoreCount(), GetProductivityCoreCount());
            Debug.Log("SpeedMultiplier: " + speedMultiplier);
            smelter.m_secPerProduct = original_m_secPerProduct / speedMultiplier;
            Debug.Log("Updated Smelter.m_secPerProduct from " + original_m_secPerProduct + " to " + smelter.m_secPerProduct);
        }

        private int GetUsedCoreSlotCount()
        {
            if (!this.m_nview.IsValid())
            {
                return 0;
            }
            return GetSpeedCoreCount() + GetEfficiencyCoreCount() + GetProductivityCoreCount();
        }

        private int GetTotalCoreSlotCount()
        {
            return m_defaultMaxOverclockCores.Value + GetCoreCount("$" + coreSlotKey);
        }

        private int GetProductivityCoreCount()
        {
            return GetCoreCount("$" + productivityCoreKey);
        }

        private int GetEfficiencyCoreCount()
        {
            return GetCoreCount("$" + efficiencyCoreKey);
        }

        private int GetSpeedCoreCount()
        {
            return GetCoreCount("$" + speedCoreKey);
        }

        public static string GetPercentageString(double ratio)
        {
            return ratio.ToString("0.0%");
        }

        private float m_holdRepeatInterval = 1f;
        private float m_lastUseTime;

        public bool Interact(Humanoid user, bool hold)
        {
            if (hold)
            {

                if (m_holdRepeatInterval <= 0f)
                {
                    return false;
                }
                if (Time.time - m_lastUseTime < m_holdRepeatInterval)
                {
                    return false;
                }
                m_lastUseTime = Time.time;

                m_nview.InvokeRPC("DropCores");
                return true;
            }

            m_lastUseTime = Time.time;
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            Debug.Log("Using item " + item.m_shared.m_name);
            if (AcceptItem(item))
            {
                if (!HasRoom(item))
                {
                    if (item.m_shared.m_name == "$" + coreSlotKey)
                    {
                        user.Message(MessageHud.MessageType.Center, "$" + maxSlotsReached);
                        return true;
                    }
                    else
                    {
                        if (MaximumSlotsReached())
                        {
                            user.Message(MessageHud.MessageType.Center, "$" + noSlotsAvailable_maxSlots);
                            return true;
                        }
                        else
                        {
                            user.Message(MessageHud.MessageType.Center, "$" + noSlotsAvailable_addSlots);
                            return true;
                        }
                    }
                }
                m_nview.InvokeRPC("AddCore", item.m_shared.m_name);
                user.GetInventory().RemoveOneItem(item);
                return true;
            }
            return false;
        }

        private bool HasRoom(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_name == "$" + coreSlotKey)
            {
                return !MaximumSlotsReached();
            }
            return GetUsedCoreSlotCount() < GetTotalCoreSlotCount();
        }

        private bool MaximumSlotsReached()
        {
            return GetTotalCoreSlotCount() >= (m_defaultMaxOverclockCores.Value + m_maxAdditionalOverclockCores.Value);
        }

        private bool AcceptItem(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_name == "$" + speedCoreKey
                || item.m_shared.m_name == "$" + productivityCoreKey
                || item.m_shared.m_name == "$" + coreSlotKey)
            {
                return true;
            }
            if (item.m_shared.m_name == "$" + efficiencyCoreKey)
            {
                return !IsKiln();
            }
            return false;
        }

        private bool IsKiln()
        {
            return GetComponent<Smelter>().m_maxFuel == 0;
        }

        private void RPC_AddCore(long sender, string coreName)
        {
            if (m_nview.IsOwner())
            { 
                SetCoreCount(coreName, GetCoreCount(coreName) + 1);
                UpdateSmelterValues();
            }

        }

        internal int OnSpawn(string ore)
        {
            int productivityCores = GetProductivityCoreCount();
            if (productivityCores == 0)
            {
                return 0;
            }
            double producticityMultiplier = GetProductivityMultiplier(productivityCores); 
            float currentPartial = GetPartial(ore);
            if (currentPartial < 0f)
            {
                currentPartial = 0f;
            }

            float additionalPartial = (float)(producticityMultiplier - 1.0f); 
            float newPartial = currentPartial + additionalPartial; 
            int additional = 0;
            while (newPartial > 1)
            {
                additional += 1;
                newPartial -= 1f;
            }
            SetPartial(ore, newPartial); 
            return additional;
        }

        private void SetPartial(string ore, float newPartial)
        {
            if (!this.m_nview.IsValid())
            {
                return;
            }
            m_nview.GetZDO().Set("overclocking_partial_" + ore, newPartial);
        }

        private float GetPartial(string ore)
        {
            if (!this.m_nview.IsValid())
            {
                return 0f;
            }
            return m_nview.GetZDO().GetFloat("overclocking_partial_" + ore);
        }

        private float m_fuelBeforeUse;
        private Smelter smelter;

        internal void OnGetFuel(float fuel)
        {
            m_fuelBeforeUse = fuel;
        }

        internal float OnSetFuel(float fuel)
        {
            float fuelUsed = m_fuelBeforeUse - fuel;
            if (fuelUsed < 0)
            {
                //Added fuel, nothing to do
                return fuel;
            }
            //  Debug.Log(" -> original new fuel level: " + fuel);
            //  Debug.Log(" -> fuel used: " + usedFuel);
            float actualFuelUsed = fuelUsed * (1f / (float)GetEfficiencyMultiplier());
            //  Debug.Log(" -> actual fuel used: " + actualFuelUsed);
            float actualFuel = m_fuelBeforeUse - actualFuelUsed;
            //  Debug.Log(" -> actual fuel level: " + actualFuel);
            if (actualFuel < 0)
            {
                actualFuel = 0;
            }
            return actualFuel;
        }

        private double GetEfficiencyMultiplier()
        {
            return GetEfficiencyMultiplier(GetSpeedCoreCount(), GetEfficiencyCoreCount(), GetProductivityCoreCount());
        }
    }
}
