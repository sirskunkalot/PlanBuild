using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static Piece;
using Object = UnityEngine.Object;

namespace PlanBuild
{
    public class PlanPiece : MonoBehaviour, Interactable, Hoverable
    {
        public static ManualLogSource logger;
    
        public const string zdoPlanPiece = "PlanPiece";
        public const string zdoPlanResource = "PlanResource";

        public static bool checkInventory = true;

        private ZNetView m_nView;
        private WearNTear m_wearNTear;

        public string m_hoverText = "";
        public Piece originalPiece;
        public GameObject originalPrefab;

        //GUI 
        public static bool m_forceDisableInit;
           
        public void Awake()
        {
            if (m_forceDisableInit)
            {
                Object.Destroy(this);
                return;
            }

            if (!originalPiece)
            {
                InvalidPlanPiece();
                return;
            }

            originalPrefab = GetPrefabPiece(originalPiece.name);
            if (!originalPrefab)
            {
                InvalidPlanPiece();
            }
            //logger.LogInfo("Prefab loaded for " + name + " -> " + originalPrefab.name);
            DisablePiece(gameObject);

            //logger.LogInfo("PlanPiece awake: " + gameObject.GetInstanceID());
            m_wearNTear = GetComponent<WearNTear>();
            m_nView = GetComponent<ZNetView>();
            if(m_nView.IsOwner())
            {
                m_nView.GetZDO().Set("support", 0f);
            }
            m_nView.Register<bool>("Refund", RPC_Refund);
            m_nView.Register<string, int>("AddResource", RPC_AddResource);
            m_nView.Register("SpawnPieceAndDestroy", RPC_SpawnPieceAndDestroy);
            UpdateHoverText(); 
        }

        private void RPC_Refund(long sender, bool all)
        {
            if (m_nView.IsOwner())
            {
                Refund(all);
            }
        }

        private bool hasSupport = false;

        public void Update()
        {
            if(m_nView.IsValid())
            {
                bool haveSupport = m_nView.GetZDO().GetFloat("support") >= m_wearNTear.GetMinSupport();
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
                    Object.Destroy(subComponent);
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

        /// <summary>
        /// Destroy this gameObject because of invalid state detected
        /// </summary>
        private void InvalidPlanPiece()
        {
            logger.LogWarning("Invalid PlanPiece , destroying self: " + name + " @ " + gameObject.transform.position);
            ZNetScene.instance.Destroy(base.gameObject);
            Destroy(this.gameObject);
        }

        internal void UpdateTextures()
        {
            ShaderHelper.UpdateTextures(gameObject, GetShaderState());
        }

        private ShaderHelper.ShaderState GetShaderState()
        { 
            if(PlanBuild.showRealTextures)
            {
                return ShaderHelper.ShaderState.Skuld;
            }
            if(hasSupport)
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
        private float m_holdRepeatInterval = 1f; 

        public string GetHoverText()
        {
            if (Time.time - m_lastLookedTime > 0.2f)
            {
                m_lastLookedTime = Time.time;
                // Debug.Log("Setting up piece info");
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
                    Piece.Requirement req = piece.m_resources[j];
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

        public static bool SetupRequirement(Transform elementRoot, Piece.Requirement req, int currentAmount)
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

                int playerAmount = Player.m_localPlayer.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
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


        public bool Interact(Humanoid user, bool hold)
        {
            if (hold)
            { 
                if (Time.time - m_lastUseTime < m_holdRepeatInterval)
                {
                    return false;
                }
                m_lastUseTime = Time.time;

                return AddAllMaterials(user);
            }

            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                if (checkInventory && !user.GetInventory().HaveItem(resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                if (currentCount < req.m_amount)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, 1);
                    user.GetInventory().RemoveItem(resourceName, 1);
                    UpdateHoverText();
                    return true;
                }
            }
            if (!HasAllResources())
            {
                user.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                return false;
            } 
            if (user.GetInventory().GetItem("$item_hammer") == null
                && user.GetInventory().GetItem(PlanHammerPrefabConfig.itemName) == null)
            {
                user.Message(MessageHud.MessageType.Center, "$message_plan_piece_need_hammer");
                return false;
            }
            if ((bool)originalPiece.m_craftingStation)
            {
                CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(originalPiece.m_craftingStation.m_name, user.transform.position);
                if(!craftingStation)
                {
                    user.Message(MessageHud.MessageType.Center, "$msg_missingstation");
                    return false;
                }
            }  
            if (!hasSupport)
            {
                user.Message(MessageHud.MessageType.Center, "$message_plan_piece_not_enough_support");
                return false;
            }
            m_nView.InvokeRPC("Refund", false);
            m_nView.InvokeRPC("SpawnPieceAndDestroy");
            return false;
        }

        private bool AddAllMaterials(Humanoid user)
        {
            bool added = false;
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                if (checkInventory && !user.GetInventory().HaveItem(resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                int remaining = req.m_amount - currentCount;
                int amountToAdd = Math.Min(remaining, user.GetInventory().CountItems(resourceName));
                if (amountToAdd > 0)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, amountToAdd);
                    user.GetInventory().RemoveItem(resourceName, amountToAdd);
                    UpdateHoverText();
                    added = true;

                }
            }
            return added;
        }

        private static string GetResourceName(Requirement req)
        {
            return req.m_resItem.m_itemData.m_shared.m_name;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                if (checkInventory && !user.GetInventory().HaveItem(resourceName))
                {
                    continue;
                }
                int currentCount = GetResourceCount(resourceName);
                int remaining = req.m_amount - currentCount; 
                if (remaining > 0)
                {
                    m_nView.InvokeRPC("AddResource", resourceName, 1);
                    user.GetInventory().RemoveItem(resourceName, 1);
                    UpdateHoverText();
                    return true;
                }
            }
            return false;
        }

        private void Refund(bool all)
        {
            foreach (Piece.Requirement req in originalPiece.m_resources)
            {
                string resourceName = GetResourceName(req);
                int currentCount = GetResourceCount(resourceName);
                if(!all)
                {
                    currentCount -= req.m_amount;
                }

               
                while (currentCount > 0)
                {
                    ItemDrop.ItemData itemData = req.m_resItem.m_itemData.Clone();
                    int dropCount = Mathf.Min(currentCount, itemData.m_shared.m_maxStackSize);
                    itemData.m_stack = dropCount;
                    currentCount -= dropCount;

    //                    logger.LogDebug("Dropping " + itemData.m_stack + " " + itemData.m_shared.m_name);
                    Object.Instantiate(req.m_resItem.gameObject, base.transform.position + Vector3.up, Quaternion.identity)
                        .GetComponent<ItemDrop>().m_itemData = itemData;
                }
            }

        }

        private bool HasAllResources()
        {
            foreach (Piece.Requirement req in originalPiece.m_resources)
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

        private static GameObject GetPrefabPiece(string pieceName)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(pieceName);
            if (!prefab)
            {
                logger.LogWarning("No prefab found for " + pieceName);
                return null;
            }
            return prefab;
        }

        public void UpdateHoverText()
        {
            StringBuilder builder = new StringBuilder();
            foreach (Requirement requirement in originalPiece.m_resources)
            {
                builder.Append(requirement.m_resItem.m_itemData.m_shared.m_name + ": " + GetResourceCount(requirement.m_resItem.m_itemData.m_shared.m_name) + "/" + requirement.m_amount + "\n");
            }
            m_hoverText = builder.ToString();
        }

        private void RPC_SpawnPieceAndDestroy(long sender)
        {
            if (!m_nView.IsOwner())
            {
                return;
            }
            GameObject actualPiece = Object.Instantiate(originalPrefab.gameObject, gameObject.transform.position, gameObject.transform.rotation);
            WearNTear wearNTear = actualPiece.GetComponent<WearNTear>();
            if (wearNTear)
            {
                wearNTear.OnPlaced();
            }
            logger.LogDebug("Plan spawn actual piece: " + actualPiece + " -> Destroying self");
            ZNetScene.instance.Destroy(this.gameObject);
            Destroy(this.gameObject);
        }
          
        [HarmonyPatch(typeof(WearNTear), "Highlight")]
        class WearNTear_HighLight_Patch
        {

            static bool Prefix(WearNTear __instance)
            {
                if (__instance.GetComponent<PlanPiece>())
                {
                    foreach (MeshRenderer renderer in __instance.GetComponentsInChildren<MeshRenderer>())
                    {
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            material.SetColor("_EmissionColor", Color.black);
                        }
                    }
                    return false;
                }
                return true;
            }
        }
          
        [HarmonyPatch(typeof(WearNTear), "Damage")]
        class WearNTear_Damage_Patch
        { 
            static bool Prefix(WearNTear __instance)
            {
                if (__instance.GetComponent<PlanPiece>())
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WearNTear), "GetSupport")]
        class WearNTear_GetSupport_Patch
        {

            static bool Prefix(WearNTear __instance, ref float __result)
            {
                if (__instance.GetComponent<PlanPiece>())
                {
                    __result = 0f;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
        class WearNTear_HaveSupport_Patch
        { 
            static bool Prefix(WearNTear __instance, ref bool __result)
            {
                if (__instance.GetComponent<PlanPiece>())
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
          
        [HarmonyPatch(typeof(WearNTear), "Destroy")]
        class WearNTear_Destroy_Patch
        {

            static bool Prefix(WearNTear __instance)
            {
                PlanPiece planPiece = __instance.GetComponent<PlanPiece>();
                if (planPiece && planPiece.m_nView.IsOwner())
                {
                    //Don't
                    // create noise
                    // create fragments
                    // play destroyed effects
                    planPiece.Refund(all: true);
                    ZNetScene.instance.Destroy(__instance.gameObject);
                    return false;
                }
                return true;
            }
        }
          
        [HarmonyPatch(typeof(Player), "CheckCanRemovePiece")]
        class Player_CheckCanRemovePiece_Patch
        {
            static bool Prefix(Piece piece, ref bool __result)
            {
                PlanPiece PlanPiece = piece.GetComponent<PlanPiece>();
                if (PlanPiece)
                {
                    __result = true;
                    return false;
                }
                return true;
            }

        }

    }
}

