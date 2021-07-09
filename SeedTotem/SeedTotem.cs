using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SeedTotem
{
    internal class SeedTotem : MonoBehaviour, Interactable, Hoverable
    {
        public static ManualLogSource logger;

        private const string m_name = "Seed totem";

        private const string ZDO_queued = "queued";
        private const string ZDO_total = "total";
        private const string ZDO_restrict = "restrict";
        private const string messageSeedGenericPlural = "$message_seed_totem_seed_generic_plural";
        private const string messageSeedGenericSingular = "$message_seed_totem_seed_generic";
        private const string messageAll = "$message_seed_totem_all";

        internal static ConfigEntry<float> configFlareSize;
        internal static ConfigEntry<Color> configFlareColor;
        internal static ConfigEntry<Color> configLightColor;
        internal static ConfigEntry<float> configLightIntensity;
        public static ConfigEntry<Color> configGlowColor;
        internal static ConfigEntry<bool> configShowQueue;
        internal static ConfigEntry<float> configRadius;
        internal static ConfigEntry<float> configDispersionTime;
        internal static ConfigEntry<float> configMargin;
        internal static ConfigEntry<int> configDispersionCount;
        internal static ConfigEntry<int> configMaxRetries;
        internal static ConfigEntry<bool> configHarvestOnHit;
        internal static ConfigEntry<bool> configCheckCultivated;
        internal static ConfigEntry<bool> configCheckBiome;
        internal static ConfigEntry<bool> configCustomRecipe;
        internal static ConfigEntry<int> configMaxSeeds;

        //TODO: Keep list of previous valid plant locations, to avoid raycasting all the time

        public static Dictionary<string, ItemConversion> seedPrefabMap = new Dictionary<string, ItemConversion>();

        public class ItemConversion
        {
            public ItemDrop seedDrop;
            public Piece plantPiece;
            public Plant plant;

            public override string ToString()
            {
                return $"{seedDrop.m_itemData.m_shared.m_name} -> {plant.m_name}";
            }
        }

        private ZNetView m_nview;

        public CircleProjector m_areaMarker;
        public GameObject m_enabledEffect;

        public MeshRenderer m_model;

        public static EffectList m_disperseEffects = new EffectList();
        private static int m_spaceMask;

        public void Awake()
        {
            if (m_spaceMask == 0)
            {
                m_spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
            }

            ScanCultivator();
            m_nview = GetComponent<ZNetView>();

            WearNTear wearNTear = GetComponent<WearNTear>();
            wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(OnDestroyed));

            m_nview.Register<string, int>("AddSeed", RPC_AddSeed);
            m_nview.Register<string>("Restrict", RPC_Restrict);
            InvokeRepeating("UpdateSeedTotem", 1f, 1f);
            InvokeRepeating("DisperseSeeds", 1f, configDispersionTime.Value);

            UpdateMaterials(false);
            UpdateGlowColor(this);
        }

        private static Boolean scanningCultivator = false;

        public static void ScanCultivator()
        {
            if (scanningCultivator)
            {
                return;
            }
            scanningCultivator = true;
            var table = PieceManager.Instance.GetPieceTable("_CultivatorPieceTable");
            foreach (GameObject cultivatorRecipe in table.m_pieces)
            {
                Plant plant = cultivatorRecipe.GetComponent<Plant>(); 
                if (plant)
                {
                    Piece piece = cultivatorRecipe.GetComponent<Piece>(); 
                    Piece.Requirement[] requirements = piece.m_resources;
                    if (requirements.Length > 1)
                    {
                        logger.LogWarning("  Multiple seeds required for " + plant.m_name + "? Skipping");
                        continue;
                    }

                    Piece.Requirement requirement = requirements[0];
                    ItemDrop itemData = requirement.m_resItem;
                    if (!seedPrefabMap.ContainsKey(itemData.m_itemData.m_shared.m_name))
                    {
                        logger.LogDebug("Looking for Prefab of " + itemData.m_itemData.m_shared.m_name + " -> " + itemData.gameObject.name);
                        ItemConversion conversion = new ItemConversion
                        {
                            seedDrop = requirement.m_resItem,
                            plantPiece = piece,
                            plant = plant
                        };
                        logger.LogDebug("Registering seed type: " + conversion);
                        seedPrefabMap.Add(itemData.m_itemData.m_shared.m_name, conversion);
                    }
                }
            }
            scanningCultivator = false;
        }

        [HarmonyPatch(typeof(WearNTear), "Damage")]
        private class WearNTear_RPC_Damage_Patch
        {
            private static bool Prefix(WearNTear __instance, HitData hit)
            {
                if (hit.GetTotalDamage() > 0)
                {
                    SeedTotem seedTotem = __instance.GetComponent<SeedTotem>();
                    if (seedTotem)
                    {
                        seedTotem.OnDamaged(hit.GetAttacker() as Player);
                        return false;
                    }
                }

                return true;
            }
        }

        private void OnDamaged(Player player)
        {
            if (configHarvestOnHit.Value)
            {
                if (player == null)
                {
                    logger.LogWarning("Not sure who hit us? Credit the local player");
                    player = Player.m_localPlayer;
                }
                Collider[] array = Physics.OverlapSphere(transform.position, configRadius.Value + configMargin.Value, m_spaceMask);
                for (int i = 0; i < array.Length; i++)
                {
                    Pickable component = array[i].GetComponent<Pickable>();
                    if (component)
                    {
                        component.Interact(player, false);
                    }
                }
            }
        }

        internal static void CopyPrivateArea(SeedTotem seedTotem, PrivateArea privateArea)
        {
            seedTotem.m_areaMarker = privateArea.m_areaMarker;

            seedTotem.m_enabledEffect = privateArea.m_enabledEffect;

            seedTotem.m_model = privateArea.m_model;

            seedTotem.m_areaMarker.gameObject.SetActive(value: false);
            seedTotem.m_areaMarker.m_radius = configRadius.Value;
            seedTotem.m_areaMarker.m_nrOfSegments = 10;
            UpdateGlowColor(seedTotem);
        }

        private static Color brown = new Color(0.574f, 0.386f, 0.208f, 1f);

        public static void UpdateGlowColor(SeedTotem seedTotem)
        {
            logger.LogDebug("Updating color of SeedTotem at " + seedTotem.transform.position);
            Material[] materials = seedTotem.m_model.materials;
            Color color = configGlowColor.Value;
            foreach (Material material in materials)
            {
                string lookFor = "Guardstone_OdenGlow_mat";
                if (material.name.StartsWith(lookFor))
                {
                    logger.LogInfo("Updating color");
                    material.SetColor("_EmissionColor", color);
                }
            }

            foreach (EffectList.EffectData effectData in m_disperseEffects.m_effectPrefabs)
            {
                ParticleSystem particleSystem = effectData.m_prefab.GetComponent<ParticleSystem>();
                ParticleSystem.MainModule psMain = particleSystem.main;

                ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.clear, 0.6f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 0.6f)
                    });
                colorOverLifetime.color = gradient;
            }

            GameObject wayEffectGameObject = seedTotem.transform.Find("WayEffect").gameObject;
            GameObject sparcsGameObject = wayEffectGameObject.transform.Find("sparcs").gameObject;
            ParticleSystem sparcs = sparcsGameObject.GetComponent<ParticleSystem>();

            ParticleSystem.ShapeModule sparcsShape = sparcs.shape;
            Vector3 sparcsScale = sparcsShape.scale;
            sparcsScale.x = configRadius.Value;
            sparcsScale.z = configRadius.Value;
            sparcsScale.y = 0.5f;
            ParticleSystem.MainModule sparcsMain = sparcs.main;
            sparcsMain.startColor = new ParticleSystem.MinMaxGradient(color, color * 0.2f);

            GameObject pointLightObject = wayEffectGameObject.transform.Find("Point light").gameObject;
            Light light = pointLightObject.GetComponent<Light>();
            light.color = configLightColor.Value;
            light.intensity = configLightIntensity.Value;
            light.range = configRadius.Value;

            GameObject flareGameObject = wayEffectGameObject.transform.Find("flare").gameObject;
            ParticleSystem flare = flareGameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule flareMain = flare.main;
            flareMain.startColor = new ParticleSystem.MinMaxGradient(configFlareColor.Value);
            flareMain.startSize = new ParticleSystem.MinMaxCurve(configFlareSize.Value);
        }

        public void UpdateSeedTotem()
        {
            if (!m_nview.IsValid())
            {
                return;
            }

            bool flag = IsEnabled();
            UpdateMaterials(flag);
            UpdateHoverText();
        }

        private void UpdateMaterials(bool flag)
        {
            m_enabledEffect.SetActive(flag);
            Material[] materials = m_model.materials;
            foreach (Material material in materials)
            {
                if (flag)
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }
        }

        private bool IsEnabled()
        {
            return GetQueueSize() > 0;
        }

        public string GetHoverName()
        {
            return m_name;
        }

        private string m_hoverText = "";

        public string GetHoverText()
        {
            if (!m_nview.IsValid()
                || Player.m_localPlayer == null)
            {
                return "";
            }
            ShowAreaMarker();
            return Localization.instance.Localize(m_hoverText);
        }

        public void ShowAreaMarker()
        {
            if ((bool)m_areaMarker)
            {
                m_areaMarker.gameObject.SetActive(value: true);
                CancelInvoke("HideMarker");
                Invoke("HideMarker", 0.5f);
            }
        }

        public void HideMarker()
        {
            m_areaMarker.gameObject.SetActive(value: false);
        }

        private void UpdateHoverText()
        {
            StringBuilder sb = new StringBuilder(GetHoverName() + " (" + GetTotalSeedCount());
            if (configMaxSeeds.Value > 0)
            {
                sb.Append("/" + configMaxSeeds.Value);
            }
            sb.Append(")\n");

            string restrict = GetRestrict();
            string seedName = messageSeedGenericSingular;
            string seedNamePlural = messageSeedGenericPlural;
            if (restrict != "")
            {
                seedName = restrict;
                seedNamePlural = restrict;
            }

            if (restrict != "")
            {
                sb.Append("<color=grey>$message_seed_totem_restricted_to</color> <color=green>" + restrict + "</color>\n");
            }

            sb.Append("[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add " + seedName + "\n");
            sb.Append("[Hold <color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add " + messageAll + " " + seedNamePlural + "\n");
            sb.Append("[<color=yellow><b>1-8</b></color>] $message_seed_totem_restrict");

            if (configShowQueue.Value)
            {
                sb.Append("\n\n");
                for (int queuePosition = 0; queuePosition < GetQueueSize(); queuePosition++)
                {
                    string queuedSeed = GetQueuedSeed(queuePosition);
                    int queuedAmount = GetQueuedSeedCount(queuePosition);
                    PlacementStatus status = GetQueuedStatus(queuePosition);

                    sb.Append(queuedAmount + " " + queuedSeed);
                    switch (status)
                    {
                        case PlacementStatus.Init:
                            //Show nothing for new seeds
                            break;

                        case PlacementStatus.NoRoom:
                            sb.Append(" <color=grey>[</color><color=green>$message_seed_totem_status_looking_for_space</color><color=grey>]</color>");
                            break;

                        case PlacementStatus.Planting:
                            sb.Append(" <color=grey>[</color><color=green>$message_seed_totem_status_planting</color><color=grey>]</color>");
                            break;

                        case PlacementStatus.WrongBiome:
                            sb.Append(" <color=grey>[</color><color=red>$message_seed_totem_status_wrong_biome</color><color=grey>]</color>");
                            break;
                    }
                    sb.Append("\n");
                }
            }

            m_hoverText = sb.ToString();
        }

        private int GetTotalSeedCount()
        {
            return this.m_nview.GetZDO().GetInt(ZDO_total);
        }

        private void DropSeeds(string seedName, int amount)
        {
            if (seedName == null)
            {
                return;
            }

            logger.LogDebug("Dropping instances of " + seedName);

            if (!seedPrefabMap.ContainsKey(seedName))
            {
                logger.LogWarning("Skipping unknown key " + seedName);
            }
            else
            {
                ItemDrop seedDrop = seedPrefabMap[seedName].seedDrop;

                GameObject seed = seedDrop.gameObject;
                if (seed == null)
                {
                    logger.LogWarning("No seed found for " + seedName);
                    return;
                }

                int remainingItems = amount;
                int maxStackSize = seedDrop.m_itemData.m_shared.m_maxStackSize;

                logger.LogDebug("Dropping " + remainingItems + " in stacks of " + maxStackSize);

                do
                {
                    Vector3 position = transform.position + Vector3.up + Random.insideUnitSphere * 0.3f;
                    Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);

                    ItemDrop droppedSeed = Instantiate(seed, position, rotation).GetComponent<ItemDrop>();
                    int itemsToDrop = (remainingItems > maxStackSize) ? maxStackSize : remainingItems;

                    if (amount != 0)
                    {
                        droppedSeed.m_itemData.m_stack = itemsToDrop;
                    }

                    remainingItems -= itemsToDrop;

                    logger.LogDebug("Dropped " + itemsToDrop + ", " + remainingItems + " left to go");
                } while (remainingItems > 0);
            }
        }

        private void DropAllSeeds()
        {
            while (GetQueueSize() > 0)
            {
                string seedName = GetQueuedSeed();
                int amount = GetQueuedSeedCount();

                DropSeeds(seedName, amount);

                ShiftQueueDown();
            }
        }

        public string FindSeed(string restrict, Inventory inventory)
        {
            if (restrict == "")
            {
                foreach (string seedName in seedPrefabMap.Keys)
                {
                    logger.LogDebug("Looking for seed " + seedName);
                    if (inventory.HaveItem(seedName))
                    {
                        logger.LogDebug("Found seed!");
                        return seedName;
                    }
                }
            }
            else
            {
                logger.LogDebug("Looking for seed " + restrict);
                if (inventory.HaveItem(restrict))
                {
                    logger.LogDebug("Found seed!");
                    return restrict;
                }
            }
            return null;
        }

        private float m_holdRepeatInterval = 1f;
        private float m_lastUseTime;

        public bool Interact(Humanoid user, bool hold)
        {
            if (Player.m_localPlayer.InPlaceMode())
            {
                return false;
            }

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

                return AddAllSeeds(user);
            }

            m_lastUseTime = Time.time;

            string restrict = GetRestrict();
            string seedName = FindSeed(restrict, user.GetInventory());

            if (seedName == null)
            {
                string missing;
                if (restrict == "")
                {
                    missing = messageSeedGenericPlural;
                }
                else
                {
                    missing = restrict;
                }

                user.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + missing);
                return false;
            }

            if (configMaxSeeds.Value > 0)
            {
                int currentSeeds = GetTotalSeedCount();
                if (currentSeeds >= configMaxSeeds.Value)
                {
                    user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                    return false;
                }
            }

            user.GetInventory().RemoveItem(seedName, 1);
            m_nview.InvokeRPC("AddSeed", seedName, 1);

            user.Message(MessageHud.MessageType.Center, "$msg_added " + seedName);

            return true;
        }

        private bool AddAllSeeds(Humanoid user)
        {
            string restrict = GetRestrict();
            StringBuilder builder = new StringBuilder();
            bool added = false;
            foreach (string seedName in seedPrefabMap.Keys)
            {
                if (restrict != "" && restrict != seedName)
                {
                    continue;
                }

                logger.LogDebug("Looking for seed " + seedName);
                int amount = user.GetInventory().CountItems(seedName);
                if (configMaxSeeds.Value > 0)
                {
                    int currentSeedsCount = GetTotalSeedCount();
                    int spaceLeft = configMaxSeeds.Value - currentSeedsCount;
                    if (spaceLeft < 0)
                    {
                        if (added)
                        {
                            return true;
                        }
                        else
                        {
                            user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
                            return false;
                        }
                    }
                    amount = Math.Min(spaceLeft, amount);
                }
                if (amount > 0)
                {
                    m_nview.InvokeRPC("AddSeed", seedName, amount);
                    user.GetInventory().RemoveItem(seedName, amount);
                    if (builder.Length > 0)
                    {
                        builder.Append("\n");
                    }
                    builder.Append("$msg_added " + amount + " " + seedName);
                }
            }

            string message = builder.ToString();
            if (message.Length > 0)
            {
                user.Message(MessageHud.MessageType.Center, builder.ToString());
                return true;
            }
            else
            {
                string missing;
                if (restrict == "")
                {
                    missing = messageSeedGenericPlural;
                }
                else
                {
                    missing = restrict;
                }

                user.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + missing);
                return false;
            }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (item.m_shared.m_buildPieces)
            {
                //Build tool
                return false;
            }

            if (Player.m_localPlayer.InPlaceMode())
            {
                return false;
            }

            string seedName = item.m_shared.m_name;
            if (!seedPrefabMap.ContainsKey(seedName))
            {
                if (GetRestrict() != "")
                {
                    //Unrestrict
                    seedName = "";
                    user.Message(MessageHud.MessageType.Center, "$message_seed_totem_unrestricted");
                }
                else
                {
                    user.Message(MessageHud.MessageType.Center, "$message_seed_totem_not_a_seed");
                    return false;
                }
            }

            logger.LogDebug("Restricting to " + seedName);
            m_nview.InvokeRPC("Restrict", seedName);
            return true;
        }

        public enum PlacementStatus
        {
            Init,
            Planting,
            NoRoom,
            WrongBiome
        }

        private PlacementStatus TryPlacePlant(ItemConversion conversion, int maxRetries)
        {
            int tried = 0;
            PlacementStatus result = PlacementStatus.Planting;
            do
            {
                tried++;
                Vector3 position = transform.position + Vector3.up + Random.onUnitSphere * configRadius.Value;
                float groundHeight = ZoneSystem.instance.GetGroundHeight(position);
                position.y = groundHeight;

                if (conversion.plant.m_biome != 0 && configCheckBiome.Value && !IsCorrectBiome(position, conversion.plant.m_biome))
                {
                    result = PlacementStatus.WrongBiome;
                    continue;
                }

                if (conversion.plant.m_needCultivatedGround && configCheckCultivated.Value && !IsCultivated(position))
                {
                    result = PlacementStatus.NoRoom;
                    continue;
                }

                if (!HasGrowSpace(position, conversion.plant.m_growRadius))
                {
                    result = PlacementStatus.NoRoom;
                    continue;
                }

                logger.LogDebug("Placing new plant " + conversion.plantPiece + " at " + position);

                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
                GameObject placedPlant = Instantiate(conversion.plantPiece.gameObject, position, rotation);
                if (placedPlant)
                {
                    result = PlacementStatus.Planting;
                    conversion.plantPiece.m_placeEffect.Create(position, rotation, placedPlant.transform);
                    RemoveOneSeed();
                    break;
                }
                else
                {
                    logger.LogWarning("No object returned?");
                }
                break;
            } while (tried <= maxRetries);

            logger.LogDebug("Max retries reached, result " + result);

            return result;
        }

        private bool IsCorrectBiome(Vector3 p, Heightmap.Biome biome)
        {
            Heightmap heightmap = Heightmap.FindHeightmap(p);
            return heightmap &&
                (heightmap.GetBiome(p) & biome) != 0;
        }

        private bool IsCultivated(Vector3 p)
        {
            Heightmap heightmap = Heightmap.FindHeightmap(p);
            return heightmap && heightmap.IsCultivated(p);
        }

        private void DumpQueueDetails()
        {
            StringBuilder builder = new StringBuilder("QueueDetails:");
            for (int queuePosition = 0; queuePosition < GetQueueSize(); queuePosition++)
            {
                string queuedSeed = GetQueuedSeed(queuePosition);
                int queuedAmount = GetQueuedSeedCount(queuePosition);
                PlacementStatus queuedStatus = GetQueuedStatus(queuePosition);
                builder.AppendLine("Position " + queuePosition + " -> " + queuedSeed + " -> " + queuedAmount + " -> " + queuedStatus);
            }

            logger.LogWarning(builder.ToString());
        }

        internal void DisperseSeeds()
        {
            if (!m_nview ||
                !m_nview.IsOwner() ||
                !m_nview.IsValid()
               || Player.m_localPlayer == null)
            {
                return;
            }

            bool dispersed = false;
            int totalPlaced = 0;
            int maxRetries = configMaxRetries.Value;
            while (GetQueueSize() > 0)
            {
                string currentSeed = GetQueuedSeed();
                int currentCount = GetQueuedSeedCount();
                if (!seedPrefabMap.ContainsKey(currentSeed))
                {
                    logger.LogWarning("Key '" + currentSeed + "' not found in seedPrefabMap");
                    DumpQueueDetails();
                    logger.LogWarning("Shifting queue to remove invalid entry");
                    ShiftQueueDown();
                    return;
                }

                ItemConversion conversion = seedPrefabMap[currentSeed];
                PlacementStatus status = TryPlacePlant(conversion, maxRetries);
                SetStatus(status);
                if (status == PlacementStatus.Planting)
                {
                    totalPlaced += 1;
                    dispersed = true;
                }
                else if (status == PlacementStatus.WrongBiome)
                {
                    logger.LogDebug("Wrong biome deteced, moving " + currentSeed + " to end of queue");

                    MoveToEndOfQueue(currentSeed, currentCount, status);
                    break;
                }
                else if (status == PlacementStatus.NoRoom)
                {
                    break;
                }
                if (totalPlaced >= configDispersionCount.Value)
                {
                    break;
                }
            };

            if (dispersed)
            {
                m_disperseEffects.Create(transform.position, Quaternion.Euler(0f, Random.Range(0, 360), 0f), transform, configRadius.Value / 5f);
            }
        }

        private void MoveToEndOfQueue(string currentSeed, int currentCount, PlacementStatus status)
        {
            logger.LogDebug("Moving " + currentSeed + " to end of queue");
            DumpQueueDetails();

            ShiftQueueDown();
            QueueSeed(currentSeed, currentCount, status);

            logger.LogDebug("After move");
            DumpQueueDetails();
        }

        private bool HasGrowSpace(Vector3 position, float m_growRadius)
        {
            if (m_spaceMask == 0)
            {
                m_spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
            }
            Collider[] array = Physics.OverlapSphere(position, m_growRadius + configMargin.Value, m_spaceMask);
            for (int i = 0; i < array.Length; i++)
            {
                Plant component = array[i].GetComponent<Plant>();
                if (!component || (!(component == this)))
                {
                    return false;
                }
            }
            return true;
        }

        public void OnDestroyed()
        {
            logger.LogInfo("SeedTotem destroyed, dropping all seeds");

            DropAllSeeds();
            CancelInvoke("UpdateSeedTotem");
        }

        private int GetCurrentCount(string seedName)
        {
            return m_nview.GetZDO().GetInt(seedName);
        }

        private int GetQueueSize()
        {
            return m_nview.GetZDO().GetInt(ZDO_queued);
        }

        private string GetQueuedSeed(int queuePosition = 0)
        {
            if (GetQueueSize() == 0)
            {
                return null;
            }
            return m_nview.GetZDO().GetString("item" + queuePosition, null);
        }

        private void SetStatus(PlacementStatus status)
        {
            m_nview.GetZDO().Set("item0status", (int)status);
        }

        private PlacementStatus GetQueuedStatus(int queuePosition = 0)
        {
            if (GetQueueSize() == 0)
            {
                return PlacementStatus.Init;
            }
            return (PlacementStatus)m_nview.GetZDO().GetInt("item" + queuePosition + "status", (int)PlacementStatus.Init);
        }

        private int GetQueuedSeedCount(int queuePosition = 0)
        {
            if (GetQueueSize() == 0)
            {
                return 0;
            }
            return m_nview.GetZDO().GetInt("item" + queuePosition + "count");
        }

        private void SetQueueSeedCount(int queuePosition, int count)
        {
            m_nview.GetZDO().Set("item" + queuePosition + "count", count);
        }

        private void RemoveOneSeed()
        {
            logger.LogDebug("--Removing 1 seed--");

            int queueSize = GetQueueSize();
            if (queueSize <= 0)
            {
                logger.LogWarning("Tried to remove a seed when none are queued");
                DumpQueueDetails();
                return;
            }

            int currentCount = GetQueuedSeedCount();

            logger.LogDebug("Current count " + currentCount);

            if (currentCount > 1)
            {
                SetQueueSeedCount(0, currentCount - 1);
            }
            else
            {
                ShiftQueueDown();
            }
            m_nview.GetZDO().Set(ZDO_total, GetTotalSeedCount() - 1);
        }

        private void ShiftQueueDown()
        {
            int queueSize = GetQueueSize();
            if (queueSize == 0)
            {
                logger.LogError("Invalid ShiftQueueDown, queue is empty");
                DumpQueueDetails();
                return;
            }

            for (int i = 0; i < queueSize; i++)
            {
                string seedName = m_nview.GetZDO().GetString("item" + (i + 1));
                m_nview.GetZDO().Set("item" + i, seedName);
                int seedCount = m_nview.GetZDO().GetInt("item" + (i + 1) + "count");
                m_nview.GetZDO().Set("item" + i + "count", seedCount);
                int seedStatus = m_nview.GetZDO().GetInt("item" + (i + 1) + "status");
                m_nview.GetZDO().Set("item" + i + "status", seedStatus);
            }

            queueSize--;

            m_nview.GetZDO().Set(ZDO_queued, queueSize);
        }

        private void QueueSeed(string name, int amount = 1, PlacementStatus status = PlacementStatus.Init)
        {
            int queueSize = GetQueueSize();
            string currentQueued = GetQueuedSeed(queueSize - 1);
            if (currentQueued == name)
            {
                SetQueueSeedCount(queueSize - 1, GetQueuedSeedCount(queueSize - 1) + amount);
            }
            else
            {
                m_nview.GetZDO().Set("item" + queueSize, name);
                m_nview.GetZDO().Set("item" + queueSize + "count", amount);
                m_nview.GetZDO().Set("item" + queueSize + "status", (int)status);
                m_nview.GetZDO().Set(ZDO_queued, queueSize + 1);
            }

            m_nview.GetZDO().Set(ZDO_total, m_nview.GetZDO().GetInt(ZDO_total) + amount);
        }

        private void RPC_DropSeeds(long sender)
        {
            if (m_nview.IsOwner())
            {
                DropAllSeeds();
            }
        }

        private void RPC_AddSeed(long sender, string seedName, int amount)
        {
            if (m_nview.IsOwner())
            {
                QueueSeed(seedName, amount);
            }
        }

        private void RPC_Restrict(long sender, string restrict)
        {
            if (m_nview.IsOwner())
            {
                SetRestrict(restrict);
                int queueSize = GetQueueSize();
                int removed = 0;
                for (int i = 0; i < queueSize; i++)
                {
                    string queuedSeed = GetQueuedSeed();
                    int queuedAmount = GetQueuedSeedCount();
                    if (queuedSeed != restrict)
                    {
                        DropSeeds(queuedSeed, queuedAmount);
                        ShiftQueueDown();
                        removed++;
                    }
                    else
                    {
                        MoveToEndOfQueue(queuedSeed, queuedAmount, GetQueuedStatus());
                    }
                }
                logger.LogDebug("Restricted to " + restrict);
            }
        }

        private void SetRestrict(string seedName)
        {
            m_nview.GetZDO().Set(ZDO_restrict, seedName);
        }

        public string GetRestrict()
        {
            return m_nview.GetZDO().GetString(ZDO_restrict);
        }

        internal static void SettingsUpdated()
        {
            logger.LogInfo("Updating settings for all SeedTotem");
            foreach (SeedTotem seedTotem in FindObjectsOfType<SeedTotem>())
            {
                UpdateGlowColor(seedTotem);
                seedTotem.m_areaMarker.m_radius = configRadius.Value;
                seedTotem.CancelInvoke("DisperseSeeds");
                seedTotem.InvokeRepeating("DisperseSeeds", 1f, configDispersionTime.Value);
            }
        }

        public DestructibleType GetDestructibleType()
        {
            return DestructibleType.Default;
        }
    }
}