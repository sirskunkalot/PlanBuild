using HarmonyLib;
using PlanBuild.Blueprints;
using PlanBuild.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Piece;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    public class PlanPiece : MonoBehaviour, Interactable, Hoverable
    {
        public const string zdoBlueprintID = "BlueprintID";
        public const string zdoPlanResource = "PlanResource";
        public const string zdoAdditionalInfo = "AdditionalText";
        public const string zdoBlueprintPiece = "BlueprintPiece";

        internal static readonly List<PlanPiece> m_planPieces = new List<PlanPiece>();

        private Piece m_piece;
        private ZNetView m_nView;
        internal WearNTear m_wearNTear;

        public Piece originalPiece;

        //GUI
        public static bool m_forceDisableInit;

        public void Awake()
        {
            if (m_forceDisableInit)
            {
                Destroy(this);
                return;
            }

            if (!originalPiece)
            {
                InvalidPlanPiece();
                return;
            }

            if (originalPiece.TryGetComponent(out WearNTear wearNTear))
            {
                m_minSupport = wearNTear.GetMinSupport();
                m_maxSupport = wearNTear.GetMaxSupport();
            }

            m_planPieces.Add(this);

            m_wearNTear = GetComponent<WearNTear>();
            m_nView = GetComponent<ZNetView>();
            m_piece = GetComponent<Piece>();

            DisablePiece(gameObject);

            m_wearNTear.m_onDestroyed += OnDestroyed;
            if (m_nView.IsOwner())
            {
                m_nView.GetZDO().Set("support", 0f);
            }
            m_nView.Register<bool>("Refund", RPC_Refund);
            m_nView.Register<string, int>("AddResource", RPC_AddResource);
            m_nView.Register<long>("SpawnPieceAndDestroy", RPC_SpawnPieceAndDestroy);
        }


        private void OnDestroyed()
        {
            BlueprintManager.Instance.PlanPieceRemovedFromBlueprint(this);
            if (m_nView.IsOwner())
            {
                Refund(true);
            }
        }

        public void OnDestroy()
        {
            m_planPieces.Remove(this);
        }

        private void RPC_Refund(long sender, bool all)
        {
            if (m_nView.IsOwner())
            {
                Refund(all);
            }
        }

        private bool hasSupport = false;

        internal bool CalculateSupported()
        {
            return m_nView.GetZDO().GetFloat("support") >= m_minSupport;
        }

        public void Update()
        {
            if (m_nView.IsValid())
            {
                bool haveSupport = CalculateSupported();
                if (haveSupport != hasSupport)
                {
                    hasSupport = haveSupport;
                    UpdateTextures();
                }
            }
        }

        private static readonly List<Type> typesToDestroyInChildren = new List<Type>()
            {
                typeof(Joint),
                typeof(Rigidbody),
                typeof(TerrainModifier),
                typeof(GuidePoint),
                typeof(Light),
                typeof(LightLod),
                typeof(Interactable),
                typeof(Hoverable)
            };

        internal void Highlight()
        {
            m_wearNTear.Highlight(Config.UnsupportedColorConfig.Value, BlueprintManager.HighlightTimeout + 0.1f);
        }

        public static int m_planLayer = LayerMask.NameToLayer("piece_nonsolid");
        public static int m_placeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");

        public void DisablePiece(GameObject gameObject)
        {
            foreach (Type toDestroy in typesToDestroyInChildren)
            {
                Component[] componentsInChildren = gameObject.GetComponentsInChildren(toDestroy);
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    Component subComponent = componentsInChildren[i];
                    if (subComponent.GetType() == typeof(PlanPiece))
                    {
                        continue;
                    }
                    Destroy(subComponent);
                }
            }

            Collider[] componentsInChildren3 = gameObject.GetComponentsInChildren<Collider>();
            foreach (Collider collider in componentsInChildren3)
            {
                if (((1 << collider.gameObject.layer) & m_placeRayMask) == 0)
                {
                    collider.gameObject.layer = m_planLayer;
                }
            }
            Transform[] componentsInChildren4 = gameObject.GetComponentsInChildren<Transform>();
            int layer = m_planLayer;
            Transform[] array = componentsInChildren4;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].gameObject.layer = layer;
            }

            AudioSource[] componentsInChildren8 = gameObject.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < componentsInChildren8.Length; i++)
            {
                componentsInChildren8[i].enabled = false;
            }
            ZSFX[] componentsInChildren9 = gameObject.GetComponentsInChildren<ZSFX>();
            for (int i = 0; i < componentsInChildren9.Length; i++)
            {
                componentsInChildren9[i].enabled = false;
            }
            Windmill componentInChildren2 = gameObject.GetComponentInChildren<Windmill>();
            if ((bool)componentInChildren2)
            {
                componentInChildren2.enabled = false;
            }
            ParticleSystem[] componentsInChildren10 = gameObject.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < componentsInChildren10.Length; i++)
            {
                componentsInChildren10[i].gameObject.SetActive(value: false);
            }

            UpdateTextures();
        }

        internal bool HasRequiredCraftingStationInRange()
        {
            if (originalPiece.m_craftingStation)
            {
                return CraftingStation.HaveBuildStationInRange(originalPiece.m_craftingStation.m_name, transform.position);
            }
            return true;
        }

        /// <summary>
        /// Destroy this gameObject because of invalid state detected
        /// </summary>
        private void InvalidPlanPiece()
        {
            Jotunn.Logger.LogWarning("Invalid PlanPiece , destroying self: " + name + " @ " + gameObject.transform.position);
            ZNetScene.instance.Destroy(gameObject);
        }

        internal void UpdateTextures()
        {
            bool selected = Selection.Instance.Contains(m_piece);
            if (selected)
            {
                Selection.Instance.Unhighlight(m_piece.GetZDOID().Value, gameObject);
            }
            ShaderHelper.UpdateTextures(gameObject, GetShaderState());
            if (selected)
            {
                Selection.Instance.Highlight(m_piece.GetZDOID().Value, gameObject);
            }
        }

        private ShaderHelper.ShaderState GetShaderState()
        {
            if (PlanCrystalPrefab.ShowRealTextures)
            {
                return ShaderHelper.ShaderState.Skuld;
            }
            if (HasSupport())
            {
                return ShaderHelper.ShaderState.Supported;
            }
            return ShaderHelper.ShaderState.Floating;
        }

        public string GetHoverName()
        {
            return "Planned " + originalPiece.m_name;
        }

        private float m_lastLookedTime = -9999f;
        private float m_lastUseTime = -9999f;
        private readonly float m_holdRepeatInterval = 1f;
        private float m_minSupport = 0f;
        internal float m_maxSupport;

        public string GetHoverText()
        {
            if (Time.time - m_lastLookedTime > 0.2f)
            {
                m_lastLookedTime = Time.time;
                SetupPieceInfo(originalPiece);
            }
            Hud.instance.m_buildHud.SetActive(true);
            if (!HasAllResources())
            {
                return Localization.instance.Localize("" +
                    "[<color=yellow>$KEY_Use</color>] [<color=yellow>1-8</color>] $plan_piece_hover_add_material\n" +
                    "[$plan_piece_hover_hold <color=yellow>$KEY_Use</color>] $plan_piece_hover_add_all_materials");
            }
            return Localization.instance.Localize("[<color=yellow>$KEY_Use</color>] $plan_piece_hover_build");
        }

        private void SetupPieceInfo(Piece piece)
        {
            Player localPlayer = Player.m_localPlayer;
            Hud.instance.m_buildSelection.text = Localization.instance.Localize(piece.m_name);
            Hud.instance.m_pieceDescription.text = Localization.instance.Localize(piece.m_description);
            Hud.instance.m_buildIcon.enabled = true;
            Hud.instance.m_buildIcon.sprite = piece.m_icon;
            GameObject[] uiRequirementPanels = Hud.instance.m_requirementItems;
            for (int j = 0; j < uiRequirementPanels.Length; j++)
            {
                if (j < piece.m_resources.Length)
                {
                    Requirement req = piece.m_resources[j];
                    uiRequirementPanels[j].SetActive(value: true);
                    SetupRequirement(uiRequirementPanels[j].transform, req, GetResourceCount(GetResourceName(req)));
                }
                else
                {
                    uiRequirementPanels[j].SetActive(value: false);
                }
            }
            if ((bool)piece.m_craftingStation)
            {
                CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, localPlayer.transform.position);
                GameObject obj = uiRequirementPanels[piece.m_resources.Length];
                obj.SetActive(value: true);
                Image component = obj.transform.Find("res_icon").GetComponent<Image>();
                Text component2 = obj.transform.Find("res_name").GetComponent<Text>();
                Text component3 = obj.transform.Find("res_amount").GetComponent<Text>();
                UITooltip component4 = obj.GetComponent<UITooltip>();
                component.sprite = piece.m_craftingStation.m_icon;
                component2.text = Localization.instance.Localize(piece.m_craftingStation.m_name);
                component4.m_text = piece.m_craftingStation.m_name;
                if (craftingStation != null)
                {
                    craftingStation.ShowAreaMarker();
                    component.color = Color.white;
                    component3.text = "";
                    component3.color = Color.white;
                }
                else
                {
                    component.color = Color.gray;
                    component3.text = "None";
                    component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
                }
            }
        }

        public bool SetupRequirement(Transform elementRoot, Requirement req, int currentAmount)
        {
            Image imageResIcon = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            Text textResName = elementRoot.transform.Find("res_name").GetComponent<Text>();
            Text textResAmount = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            UITooltip uiTooltip = elementRoot.GetComponent<UITooltip>();
            if (req.m_resItem != null)
            {
                imageResIcon.gameObject.SetActive(value: true);
                textResName.gameObject.SetActive(value: true);
                textResAmount.gameObject.SetActive(value: true);
                imageResIcon.sprite = req.m_resItem.m_itemData.GetIcon();
                imageResIcon.color = Color.white;

                uiTooltip.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
                textResName.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);

                int requiredAmount = req.GetAmount(0);

                int playerAmount = PlayerGetResourceCount(Player.m_localPlayer, req.m_resItem.m_itemData.m_shared.m_name);
                int remaining = requiredAmount - currentAmount;

                textResAmount.text = currentAmount + "/" + requiredAmount;
                if (remaining > 0 && playerAmount == 0)
                {
                    imageResIcon.color = Color.gray;
                    textResAmount.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
                }
                else
                {
                    imageResIcon.color = Color.white;
                    textResAmount.color = Color.white;
                }
            }
            return true;
        }

        //Hooks for Harmony patches
        public List<Inventory> GetInventories(Humanoid player)
        {
            //List to support extended inventory from Equipment & Quick Slots
            return new List<Inventory> { player.GetInventory() };
        }

        [Obsolete]
        public bool PlayerHaveResource(Humanoid player, string resourceName)
        {
            return player.GetInventory().HaveItem(resourceName);
        }

        [Obsolete]
        public int PlayerGetResourceCount(Humanoid player, string resourceName)
        {
            return player.GetInventory().CountItems(resourceName);
        }
        
        [Obsolete]
        public void PlayerRemoveResource(Humanoid player, string resourceName, int amount)
        {
            player.GetInventory().RemoveItem(resourceName, amount);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                if (Time.time - m_lastUseTime < m_holdRepeatInterval)
                {
                    return false;
                }
                m_lastUseTime = Time.time;

                bool added = false;
                foreach (Inventory inventory in GetInventories(user))
                {
                    added |= AddAllMaterials(inventory);
                }

                return added;
            }

            foreach (Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);

                if (!PlayerHaveResource(user, resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                if (currentCount < req.m_amount)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, 1);
                    user.GetInventory().RemoveItem(resourceName, 1);
                    return true;
                }
            }
            if (!HasAllResources())
            {
                user.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return false;
            }
            if (user.GetInventory().GetItem(PlanHammerPrefab.PlanHammerItemName) == null)
            {
                user.Message(MessageHud.MessageType.Center, "$message_plan_piece_need_hammer");
                return false;
            }
            if ((bool)originalPiece.m_craftingStation)
            {
                CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(originalPiece.m_craftingStation.m_name, user.transform.position);
                if (!craftingStation)
                {
                    user.Message(MessageHud.MessageType.Center, "$msg_missingstation");
                    return false;
                }
            }
            if (!HasSupport())
            {
                user.Message(MessageHud.MessageType.Center, "$message_plan_piece_not_enough_support");
                return false;
            }
            long playerID = (user as Player).GetPlayerID();
            Build(playerID);
            return false;
        }

        public bool HasSupport()
        {
            return hasSupport;
        }

        internal ZDOID GetPlanPieceID()
        {
            return m_nView.m_zdo.m_uid;
        }

        public void Build(long playerID)
        {
            m_nView.InvokeRPC("Refund", false);
            m_nView.InvokeRPC("SpawnPieceAndDestroy", playerID);
        }

        public bool AddAllMaterials(Inventory inventory)
        {
            bool added = false;
            bool finished = true;
            foreach (Requirement req in originalPiece.m_resources)
            {
                bool reqFinished = true;
                string resourceName = GetResourceName(req);
                int remaining = GetRemaining(req);
                reqFinished &= remaining == 0;

                if (inventory.HaveItem(resourceName))
                {
                    int amountToAdd = Math.Min(remaining, inventory.CountItems(resourceName));

                    if (amountToAdd > 0)
                    {
                        m_nView.InvokeRPC("AddResource", resourceName, amountToAdd);
                        inventory.RemoveItem(resourceName, amountToAdd);
                        added = true;
                        reqFinished = remaining == amountToAdd;
                    }

                    finished &= reqFinished;
                }
            }
            return added;
        }

        public Dictionary<string, int> GetRemaining()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (Requirement req in originalPiece.m_resources)
            {
                result.Add(GetResourceName(req), GetRemaining(req));
            }
            return result;
        }

        private int GetRemaining(Requirement req)
        {
            int currentCount = GetResourceCount(req);
            int remaining = req.m_amount - currentCount;
            return remaining;
        }

        private int GetResourceCount(Requirement req)
        {
            return GetResourceCount(GetResourceName(req));
        }

        private static string GetResourceName(Requirement req)
        {
            return req.m_resItem.m_itemData.m_shared.m_name;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            foreach (Requirement req in originalPiece.m_resources)
            {
                if (req.m_resItem.m_itemData.m_shared.m_name != item.m_shared.m_name)
                {
                    continue;
                }
                string resourceName = GetResourceName(req);
                if (!PlayerHaveResource(user, resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                int remaining = req.m_amount - currentCount;
                if (remaining > 0)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, 1);
                    PlayerRemoveResource(user, resourceName, 1);
                    return true;
                }
            }
            return false;
        }

        private void Refund(bool all)
        {
            foreach (Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                int currentCount = GetResourceCount(resourceName);
                if (!all)
                {
                    currentCount -= req.m_amount;
                }

                while (currentCount > 0)
                {
                    ItemDrop.ItemData itemData = req.m_resItem.m_itemData.Clone();
                    int dropCount = Mathf.Min(currentCount, itemData.m_shared.m_maxStackSize);
                    itemData.m_stack = dropCount;
                    currentCount -= dropCount;

                    Instantiate(req.m_resItem.gameObject, transform.position + Vector3.up, Quaternion.identity)
                        .GetComponent<ItemDrop>().SetStack(dropCount);
                }
            }
        }

        internal void Remove()
        {
            m_wearNTear.Remove();
        }

        public bool HasAllResources()
        {
            foreach (Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                int currentCount = GetResourceCount(resourceName);
                if (currentCount < req.m_amount)
                {
                    return false;
                }
            }
            return true;
        }

        public void RPC_AddResource(long sender, string resource, int amount)
        {
            if (m_nView.IsOwner())
            {
                AddResource(resource, amount);
            }
        }

        private void AddResource(string resource, int amount)
        {
            int current = GetResourceCount(resource);
            SetResourceCount(resource, current + amount);
        }

        private void SetResourceCount(string resource, int count)
        {
            m_nView.GetZDO().Set(zdoPlanResource + "_" + resource, count);
        }

        private int GetResourceCount(string resource)
        {
            if (!m_nView.IsValid())
            {
                return 0;
            }
            return m_nView.GetZDO().GetInt(zdoPlanResource + "_" + resource);
        }

        private void RPC_SpawnPieceAndDestroy(long sender, long creatorID)
        {
            if (!m_nView.IsOwner())
            {
                return;
            }
            GameObject actualPiece = SpawnPiece(gameObject, creatorID, transform.position, transform.rotation, originalPiece.gameObject, m_nView.GetZDO().GetString(zdoAdditionalInfo));
#if DEBUG
            Jotunn.Logger.LogDebug("Plan spawn actual piece: " + actualPiece + " -> Destroying self");
#endif
            BlueprintManager.Instance.PlanPieceRemovedFromBlueprint(this);
            ZNetScene.instance.Destroy(gameObject);
        }

        internal static GameObject SpawnPiece(GameObject originatingObject, long creatorID, Vector3 position, Quaternion rotation, GameObject prefab, string textReceiverInput)
        {
            GameObject actualPiece = Instantiate(prefab, position, rotation);
            OnPieceReplaced(originatingObject, actualPiece);
            // Register special effects
            if (creatorID == Player.m_localPlayer?.GetPlayerID())
            {
                CraftingStation craftingStation = actualPiece.GetComponentInChildren<CraftingStation>();
                if (craftingStation)
                {
                    Player.m_localPlayer.AddKnownStation(craftingStation);
                }
                PrivateArea privateArea = actualPiece.GetComponent<PrivateArea>();
                if (privateArea)
                {
                    privateArea.Setup(Game.instance.GetPlayerProfile().GetName());
                }
                if (actualPiece.TryGetComponent(out Piece newPiece))
                {
                    newPiece.m_placeEffect.Create(actualPiece.transform.position, actualPiece.transform.rotation, actualPiece.transform, 1f);
                }

                // Count up player builds
                Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
            }
            WearNTear wearntear = actualPiece.GetComponent<WearNTear>();
            if (wearntear)
            {
                wearntear.OnPlaced();
            }
            TextReceiver textReceiver = actualPiece.GetComponent<TextReceiver>();
            if (textReceiver != null)
            {
                textReceiver.SetText(textReceiverInput);
            }
            actualPiece.GetComponent<Piece>().SetCreator(creatorID);
            return actualPiece;
        }

        internal static void OnPieceReplaced(GameObject originatingPiece, GameObject placedPiece)
        {

        }

        [HarmonyPatch(typeof(WearNTear), "Damage")]
        [HarmonyPrefix]
        private static bool WearNTear_Damage_Prefix(WearNTear __instance)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WearNTear), "GetSupport")]
        [HarmonyPrefix]
        private static bool WearNTear_GetSupport_Prefix(WearNTear __instance, ref float __result)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
        [HarmonyPrefix]
        private static bool WearNTear_HaveSupport_Prefix(WearNTear __instance, ref bool __result)
        {
            if (__instance.GetComponent<PlanPiece>())
            {
                __result = true;
                return false;
            }
            return true;
        }

        internal ZDOID GetBlueprintID()
        {
            if (!m_nView.IsValid())
            {
                return ZDOID.None;
            }
            return m_nView.GetZDO().GetZDOID(zdoBlueprintID);
        }
    }
}