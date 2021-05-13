// Decompiled with JetBrains decompiler
// Type: ItemStand
// Assembly: assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F48D6A22-6962-45BF-8D82-0AAD6AFA4FDB
// Assembly location: E:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll

using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;


namespace ValheimStands.Unity
{
    
    public class VisualEquipment {
        public ItemDrop drop;
        public string prefabName;
        public GameObject gameObject;
    }

    public class ArmorStand : MonoBehaviour, Interactable, Hoverable
    {

        private static class ZDOVars {
            public const string LeftHandItem = "lh";
            public const string OneHandedWeapons = "1wep";
            public const string ChestItem = "chest";
            public const string LegsItem = "leg";
            public const string HelmItem = "helm";
            public const string ShoulderItem = "shld";
            public const string BowItem = "bow";
            public const string TwoHandedWeapon = "2wep";

            public static String[] All() {
                return new String[] {LeftHandItem, OneHandedWeapons, ChestItem, LegsItem, HelmItem, ShoulderItem, TwoHandedWeapon, BowItem};
            }
        }

        private class EquippableSlot {
            public string zdoVar;            
            public VisualEquipment visualObject;
            public ZDO zdo;
            public ArmorStand armorStand;
            public ItemDrop.ItemData.ItemType[] secondarySlots = new ItemDrop.ItemData.ItemType[0];
            public ItemDrop.ItemData.ItemType[] exclusiveWith = new ItemDrop.ItemData.ItemType[0];

            public Action<EquippableSlot> OnDestroyVisual = (slot) => {};
            public Action<EquippableSlot, ItemDrop> OnCreateVisual = (_, __) => {};
            public Func<EquippableSlot, GameObject> GetAttachPoint = (_) => null;

            public bool isFilled() {
                return !String.IsNullOrEmpty(zdo.GetString(zdoVar));
            }

            public bool bindSkeletonToStand() {
                if(visualObject?.gameObject == null) {
                    return false;
                }

                var skinned = false;
                // if we have skinned meshes we have to set up bones
                foreach (SkinnedMeshRenderer skinnedMeshRenderer in visualObject.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    skinned = true;
                    // set root bones for skinned meshes
                    //ZLog.Log($"Setting skinned mesh bones for {skinnedMeshRenderer}");
                    Transform rootBone = armorStand.chestStand.rootBone;
                                                
                    //ZLog.Log($"Setting root bone to {rootBone}");
                    if(skinnedMeshRenderer.rootBone != null) {
                        skinnedMeshRenderer.rootBone.SetPositionAndRotation(
                            rootBone.position, 
                            rootBone.rotation
                        );
                    }

                    // TODO: Make dict of bones so we can look them up faster
                    // TODO: Fix bone binding, this is a hack, should be able to assign bones not just change them
                    List<Transform> newBones = new List<Transform>();                        
                    foreach(Transform targetBone in skinnedMeshRenderer.bones) {                                
                        foreach(Transform sourceBone in armorStand.chestStand.bones) {
                            if(sourceBone.name == targetBone.name) {                                                                        
                                targetBone.SetPositionAndRotation(
                                    sourceBone.position, 
                                    sourceBone.rotation
                                );
                                                                
                                continue;
                            }
                        }
                    }
                }

                return skinned;
            }

            public bool exclusiveSlotFilled(EquippableSlotManager slotManager) {
                foreach(var exclusiveSlot in this.exclusiveWith) {
                    if(slotManager.slots[exclusiveSlot].isFilled()) {
                        return true;
                    }
                }

                return false;
            }

            public string equippedItem {
                get {
                    return zdo.GetString(zdoVar);
                }
            }

            public int equippedVariant {
                get {
                    return zdo.GetInt(zdoVariantVar);
                }
            }

            private string zdoVariantVar {
                get {
                    return zdoVar + "v";
                }
            }

            public void equip(ItemDrop.ItemData item) {
                zdo.Set(zdoVar, item.m_dropPrefab.name);
                zdo.Set(zdoVariantVar, item.m_variant);

                zdo.Set($"{zdoVar}durability", item.m_durability);
                zdo.Set($"{zdoVar}stack", item.m_stack);
                zdo.Set($"{zdoVar}quality", item.m_quality);
                zdo.Set($"{zdoVar}variant", item.m_variant);
                zdo.Set($"{zdoVar}crafterID", item.m_crafterID);
                zdo.Set($"{zdoVar}crafterName", item.m_crafterName);
            }
        
            public void unequip() {
                if(isFilled()) {
                    GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(equippedItem);
                    if ((bool)((UnityEngine.Object)itemPrefab)) {
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, this.armorStand.m_dropSpawnPoint.position, this.armorStand.m_dropSpawnPoint.rotation);
                        
                        var itemData = gameObject.GetComponent<ItemDrop>().m_itemData;
                        itemData.m_durability = zdo.GetFloat($"{zdoVar}durability", itemData.m_durability);
                        itemData.m_stack = zdo.GetInt($"{zdoVar}stack", itemData.m_stack);
                        itemData.m_quality = zdo.GetInt($"{zdoVar}quality", itemData.m_quality);
                        itemData.m_variant = zdo.GetInt($"{zdoVar}variant", itemData.m_variant);
                        itemData.m_crafterID = zdo.GetLong($"{zdoVar}crafterID", itemData.m_crafterID);
                        itemData.m_crafterName = zdo.GetString($"{zdoVar}crafterName", itemData.m_crafterName);
                        
                        this.armorStand.m_effects.Create(this.armorStand.m_dropSpawnPoint.position, Quaternion.identity, null, 1f);
                        zdo.Set(zdoVar, "");
                        zdo.Set(zdoVariantVar, 0);
                        
                    }
                }
            }

            public bool shouldRemoveVisual() {
                return String.IsNullOrEmpty(equippedItem);
            }

            public bool rendered() {
                return this.visualObject != null;
            }

            public void removeVisual() {
                if(visualObject != null) {
                    UnityEngine.Object.Destroy(visualObject.gameObject);
                    visualObject = null;
                    
                    this.OnDestroyVisual(this);                    
                }
            }

            public bool needsVisualUpdate() {
                var itemWanted = zdo.GetString(zdoVar);
                //ZLog.Log($"Checking slot with vo: {visualObject?.prefabName} and item {itemWanted}");
                if(String.IsNullOrEmpty(itemWanted)) {
                    // if the item wanted is blank and the gameobject exists, we need to clear it
                    return visualObject != null;
                }else {
                    if(visualObject == null) {
                        return true;
                    }

                    return visualObject.prefabName != itemWanted;
                }
            }
        }

        private class EquippableSlotManager {
            public Dictionary<ItemDrop.ItemData.ItemType, EquippableSlot> slots = new Dictionary<ItemDrop.ItemData.ItemType, EquippableSlot>() {
                {
                    ItemDrop.ItemData.ItemType.Helmet,
                    new EquippableSlot {
                        zdoVar = ZDOVars.HelmItem,
                        GetAttachPoint = (EquippableSlot slot) => slot.armorStand.helmetAttachPoint,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {
                            slot.armorStand.helmetStand.SetActive(true);
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {                            
                            slot.armorStand.helmetStand.SetActive(false);
                        } 
                    }
                },
                {
                    ItemDrop.ItemData.ItemType.OneHandedWeapon,
                    new EquippableSlot {
                        secondarySlots = new [] {ItemDrop.ItemData.ItemType.Shield},
                        exclusiveWith = new [] {ItemDrop.ItemData.ItemType.TwoHandedWeapon},
                        zdoVar = ZDOVars.OneHandedWeapons,
                        GetAttachPoint = (EquippableSlot slot) => slot.armorStand.rightHandAttachPoint,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {                            
                            slot.armorStand.standAnimator.SetBool("hold_right", true);                            
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {
                            slot.armorStand.standAnimator.SetBool("hold_right", false);
                        }
                    }                    
                },
                {
                    ItemDrop.ItemData.ItemType.Bow,
                    new EquippableSlot {
                        zdoVar = ZDOVars.BowItem,
                        exclusiveWith = new [] {ItemDrop.ItemData.ItemType.Shield},
                        GetAttachPoint = (EquippableSlot slot) => slot.armorStand.leftHandAttachPoint,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {                            
                            slot.armorStand.standAnimator.SetBool("hold_left", true);                            
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {
                            slot.armorStand.standAnimator.SetBool("hold_left", false);
                        }
                    }
                },
                {
                    ItemDrop.ItemData.ItemType.TwoHandedWeapon,
                    new EquippableSlot {
                        zdoVar = ZDOVars.TwoHandedWeapon,
                        exclusiveWith = new [] {ItemDrop.ItemData.ItemType.OneHandedWeapon},
                        GetAttachPoint = (EquippableSlot slot) => slot.armorStand.rightHandAttachPoint,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {                            
                            slot.armorStand.standAnimator.SetBool("hold_right", true);                            
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {
                            slot.armorStand.standAnimator.SetBool("hold_right", false);
                        }
                    }
                },
                {
                    ItemDrop.ItemData.ItemType.Shield,
                    new EquippableSlot {
                        zdoVar = ZDOVars.LeftHandItem,
                        exclusiveWith = new [] {ItemDrop.ItemData.ItemType.Bow},
                        GetAttachPoint = (EquippableSlot slot) => slot.armorStand.leftHandAttachPoint,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {                            
                            slot.armorStand.standAnimator.SetBool("hold_left", true);                            
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {
                            slot.armorStand.standAnimator.SetBool("hold_left", false);
                        }
                    }
                },
                {
                    ItemDrop.ItemData.ItemType.Shoulder,
                    new EquippableSlot {
                        zdoVar = ZDOVars.ShoulderItem
                    }
                },
                {
                    ItemDrop.ItemData.ItemType.Chest,
                    new EquippableSlot {
                        zdoVar = ZDOVars.ChestItem,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {
                            slot.armorStand.chestStand.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
                            slot.armorStand.chestStand.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
                            slot.armorStand.chestStand.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {
                            slot.armorStand.chestStand.material.SetTexture("_ChestTex", slot.armorStand.defaultBodyTexture);
                            slot.armorStand.chestStand.material.SetTexture("_ChestBumpMap", null);
                            slot.armorStand.chestStand.material.SetTexture("_ChestMetal", null);
                        }                                                
                    }
                },
                {
                    ItemDrop.ItemData.ItemType.Legs,
                    new EquippableSlot {
                        zdoVar = ZDOVars.LegsItem,
                        OnCreateVisual = delegate(EquippableSlot slot, ItemDrop component) {
                            slot.armorStand.legsStand.material.SetTexture("_LegsTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsTex"));
                            slot.armorStand.legsStand.material.SetTexture("_LegsBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsBumpMap"));
                            slot.armorStand.legsStand.material.SetTexture("_LegsMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsMetal"));
                            
                            slot.armorStand.legsStand.gameObject.SetActive(true);
                        },
                        OnDestroyVisual = delegate(EquippableSlot slot) {
                            slot.armorStand.legsStand.material.SetTexture("_LegsTex", slot.armorStand.defaultBodyTexture);
                            slot.armorStand.legsStand.material.SetTexture("_LegsBumpMap", null);
                            slot.armorStand.legsStand.material.SetTexture("_LegsMetal", null);
                                
                            slot.armorStand.legsStand.gameObject.SetActive(false);
                        }  
                    }
                }
            };

            public ItemDrop.ItemData queuedItem;

            public bool hasQueuedItem() {
                return queuedItem != null;
            }

            public EquippableSlotManager(ArmorStand armorStand) {
                foreach(var slot in this.slots.Values) {
                    slot.zdo = armorStand.zdo;
                    slot.armorStand = armorStand;
                }
            }
        }

        private class PoseManager {
            enum Poses {
                None = 0,
                ArmsIn = 1,
                ArmsDown = 2,
                ArmsCrossed = 3,
                Action1 = 4,
                DualWieldSide = 5
            }
                        
            public int poseIndex {
                get {                    
                    return _armorStand.zdo.GetInt("poseidx", 0);
                }
                set {
                    _armorStand.zdo.Set("poseidx", value);
                }
            }

            private ArmorStand _armorStand;
            public PoseManager(ArmorStand armorStand) {                
                _armorStand = armorStand;                              
            }

            private bool _hasUpdatedAtLeastOnce = false;
            public bool needsVisualUpdate() {
                if(!_hasUpdatedAtLeastOnce) {                    
                    return true;
                }
                return poseIndex != _armorStand.standAnimator.GetInteger("pose");
            }

            public void doVisualUpdate() {
                doVisualUpdate(null);
            }

            public void doVisualUpdate(int? poseIndexOverride) {
                _hasUpdatedAtLeastOnce = true;
                _armorStand.standAnimator.SetInteger("pose", poseIndexOverride == null ? poseIndex : (int)poseIndexOverride);
                _armorStand.StartCoroutine(_armorStand.rebindEquipment());
            }

            public int getNextPose() {
                var poses = Enum.GetValues(typeof(Poses));
                var nextPose = poseIndex + 1;
                if(nextPose >= poses.Length) {
                    nextPose = 0;
                }

                return nextPose;
            }

            public void setNextPose() {
                poseIndex = getNextPose();
                this.doVisualUpdate(poseIndex);                
            }
        }

        private EquippableSlotManager equipmentSlotManager;
        private PoseManager poseManager;

        public string m_name = "";
        public bool m_canBeRemoved = true;
        public List<ItemDrop.ItemData.ItemType> m_supportedTypes = new List<ItemDrop.ItemData.ItemType>();
        public List<ItemDrop> m_unsupportedItems = new List<ItemDrop>();
        public List<ItemDrop> m_supportedItems = new List<ItemDrop>();
        public EffectList m_effects = new EffectList();
        public EffectList m_destroyEffects = new EffectList();

        [Header("Guardian power")]
        public float m_powerActivationDelay = 2f;
        public EffectList m_activatePowerEffects = new EffectList();
        public EffectList m_activatePowerEffectsPlayer = new EffectList();
        
        private string m_currentItemName = "";
        public ZNetView m_netViewOverride;
        public Transform m_attachOther;
        public Transform m_dropSpawnPoint;
        public bool m_autoAttach;
        public StatusEffect m_guardianPower;
                                
        private ZNetView m_nview;
        
        public Animator standAnimator;
        public SkinnedMeshRenderer chestStand;
        public SkinnedMeshRenderer legsStand;
        public SkinnedMeshRenderer legsPole;
        public Texture2D defaultBodyTexture;

        public GameObject helmetStand;
        public GameObject helmetAttachPoint;
        public GameObject leftHandAttachPoint;
        public GameObject rightHandAttachPoint;
        public CapsuleCollider[] m_clothColliders;

        private void Awake()
        {   
            // Hopefully there's a player at this point. We need to get their material reference so the shaders look right.
            chestStand.material = ZNetScene.instance.GetPrefab("Player").GetComponent<VisEquipment>().m_bodyModel.material;
            legsStand.material = chestStand.material;
            legsPole.material = chestStand.material;

            // If items are equipped this will sort itself out and reset.            
            legsStand.gameObject.SetActive(false);
            helmetStand.SetActive(false);

            // This is the wood texture for the stand
            chestStand.material.SetTexture("_MainTex", defaultBodyTexture);
            legsStand.material.SetTexture("_MainTex", defaultBodyTexture);
            legsPole.material.SetTexture("_MainTex", defaultBodyTexture);

            this.m_nview = (UnityEngine.Object)this.m_netViewOverride ? this.m_netViewOverride : this.gameObject.GetComponent<ZNetView>();
            
            if (this.m_nview.GetZDO() == null)
                return;            

            equipmentSlotManager = new EquippableSlotManager(this);
            poseManager = new PoseManager(this);
            WearNTear component = this.GetComponent<WearNTear>();
            if ((bool)((UnityEngine.Object)component))
                component.m_onDestroyed += new Action(this.OnDestroyed);
            
            this.m_nview.Register("DropItems", new Action<long>(this.RPC_DropItems));
            this.m_nview.Register("RequestOwn", new Action<long>(this.RPC_RequestOwn));
            this.m_nview.Register("DestroyAttachment", new Action<long>(this.RPC_DestroyAttachment));
            this.m_nview.Register("SetVisualItems", new Action<long>(this.RPC_SetVisualItems));
            this.InvokeRepeating("UpdateVisual", 1f, 4f);
        }

        private ZDO zdo {
            get {
                return this.m_nview.GetZDO();
            }
        }

        private void OnDestroyed()
        {
            if (!this.m_nview.IsOwner())
                return;
            this.DropItems();
        }

        public string GetHoverText()
        {
            
            if (!(bool)((UnityEngine.Object)Player.m_localPlayer))
                return "";

            var instructions = new List<string>();

            if (this.HaveAttachment())
            {
                if (this.m_canBeRemoved)
                   instructions.Add("[<color=yellow><b>$KEY_Use</b></color>] $ceko_piece_armorstand_take");                
            }
            
            instructions.Add("[<color=yellow><b>SHIFT + $KEY_Use</b></color>] $ceko_piece_armorstand_cycle_poses");                
            instructions.Add("[<color=yellow><b>1-8</b></color>] $ceko_piece_armorstand_attach");

            return Localization.instance.Localize(String.Join("\n", instructions));
        }

        public string GetHoverName()
        {
            return this.m_name;
        }

        public bool Interact(Humanoid user, bool hold)
        {            
            if (hold)
                return false;

            if(Input.GetKey(KeyCode.LeftShift)) {       
                if (!this.m_nview.IsOwner())
                    this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
                    
                poseManager.setNextPose();
                return true;
            }else{
                if (this.HaveAttachment() && this.m_canBeRemoved) {                    
                    this.m_nview.InvokeRPC("DropItems");
                    return true;                                
                }
            }

            return false;
        }

        private bool IsGuardianPowerActive(Humanoid user)
        {
            return (user as Player).GetGuardianPowerName() == this.m_guardianPower.name;
        }

        private void DelayedPowerActivation()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            player.SetGuardianPower(this.m_guardianPower.name);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            Debug.Log($"Using item {item.m_shared.m_name}:{item.m_shared.m_itemType}");
            
            if(equipmentSlotManager.hasQueuedItem()) {
                // stop clicking so fast
                return false;
            }

            if (!this.CanAttach(item)) {
                user.Message(MessageHud.MessageType.Center, "$ceko_piece_armorstand_cantattach", 0, null);
                return true;
            }
            if (!this.m_nview.IsOwner())
                this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
            equipmentSlotManager.queuedItem = item;

            this.CancelInvoke("UpdateAttach");
            this.InvokeRepeating("UpdateAttach", 0.0f, 0.1f);
            return true;
        }

        private void RPC_DropItems(long sender)
        {
            if (!this.m_nview.IsOwner() || !this.m_canBeRemoved)
                return;
            this.DropItems();
        }
        
        public void DestroyAttachment()
        {
            this.m_nview.InvokeRPC("DestroyAttachment", Array.Empty<object>());
        }

        public void RPC_DestroyAttachment(long sender)
        {
            if (!this.m_nview.IsOwner() || !this.HaveAttachment())
                return;
                
            this.m_nview.GetZDO().Set("item", "");
            this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItems");
            this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f);
        }
        
        private void DropItems()
        {
            foreach(var slot in equipmentSlotManager.slots.Values) {
                slot.unequip();
            }            
                        
            this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItems");
        }

        private Transform GetAttach(ItemDrop.ItemData item)
        {
            return this.m_attachOther;
        }

        private void UpdateAttach()
        {
            if (!this.m_nview.IsOwner())
                return;
            this.CancelInvoke("UpdateAttach");
            Player player = Player.m_localPlayer;
            if (equipmentSlotManager.hasQueuedItem() && player != null && (player.GetInventory().ContainsItem(equipmentSlotManager.queuedItem)) && this.CanAttach(equipmentSlotManager.queuedItem))
            {
                // Users can queue one item at a time, but depending on its type is what slot it belongs to. If the slot is already filled it should
                // drop the item first.
                
                ItemDrop.ItemData itemData = equipmentSlotManager.queuedItem.Clone();
                itemData.m_stack = 1;
                
                if(equipmentSlotManager.slots.ContainsKey(itemData.m_shared.m_itemType)) {
                    EquippableSlot unfilledSlot = null;

                    var slot = equipmentSlotManager.slots[itemData.m_shared.m_itemType];                    
                    if(slot.isFilled() || slot.exclusiveSlotFilled(equipmentSlotManager)) {
                        foreach(var secondarySlot in slot.secondarySlots) {
                            var fallbackSlot = equipmentSlotManager.slots[secondarySlot];
                            if(!fallbackSlot.isFilled() && !fallbackSlot.exclusiveSlotFilled(equipmentSlotManager)) {
                                unfilledSlot = fallbackSlot;
                                break;
                            }
                        }
                    }else{
                        unfilledSlot = slot;
                    }

                    if(unfilledSlot == null) {
                        player.Message(MessageHud.MessageType.Center, "$ceko_piece_slot_filled", 0, null);
                    }else{
                        unfilledSlot.equip(itemData);
                        player.UnequipItem(equipmentSlotManager.queuedItem, true);
                        player.GetInventory().RemoveOneItem(equipmentSlotManager.queuedItem);
                        this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItems");
                        this.m_effects.Create(this.GetAttach(equipmentSlotManager.queuedItem).transform.position, Quaternion.identity, null, 1f);
                    }
                }else{
                    ZLog.Log("Queued item didn't map to a slot.");
                }
            }
            equipmentSlotManager.queuedItem = null;
        }

        private void RPC_RequestOwn(long sender)
        {
            if (!this.m_nview.IsOwner())
                return;
            this.m_nview.GetZDO().SetOwner(sender);
        }

        private void UpdateVisual()
        {
            this.SetVisualItems();
        }

        private void RPC_SetVisualItems(long sender)
        {
            this.SetVisualItems();
        }
        
        private IEnumerator rebindEquipment() {
            // wait 1 frame to start transition
            yield return 0;
            ZLog.Log("Rebinding equipment");
            var inTransition = true;
            while(inTransition) {
                yield return 0; // check for transition next frame

                inTransition = false;
                for(var i=0;i < standAnimator.layerCount; i++) {
                    if(standAnimator.IsInTransition(i)) {
                        inTransition = true;
                    }
                }
            }

            ZLog.Log("Animation transition finished, rebinding");
            foreach(var slot in equipmentSlotManager.slots.Values) {
                if(slot.isFilled()) {
                    slot.bindSkeletonToStand();
                }
            }
        }

        private void resolveSlot(EquippableSlot slot) {
            var wantedItem = slot.equippedItem;
            ZLog.Log("Resolving slot: " + wantedItem);

            if(slot.shouldRemoveVisual()) {
                slot.removeVisual();
                return;
            }

            if(slot.rendered())
                return;

            GameObject wantedPrefab = ObjectDB.instance.GetItemPrefab(wantedItem);
            if (wantedPrefab == null) {
                ZLog.LogWarning("Missing item prefab " + wantedItem);
            } else {
                ItemDrop component = wantedPrefab.GetComponent<ItemDrop>();                
                //ZLog.Log("Running onCreateVisual");
                slot.OnCreateVisual(slot, component);
                
                // This is the gameobject in the prefab that gets instantiated and attached to the stand
                GameObject wantedAttachPrefab = this.GetAttachPrefab(wantedPrefab);
                if (wantedAttachPrefab == null) {
                    // Can happen if a gameobject is not found with attach or attach_skin name, sometimes prefabs just change
                    // the player's textures and don't add any new geometry.
                    ZLog.LogWarning("Failed to get attach prefab for item " + wantedPrefab);
                    slot.visualObject = new VisualEquipment {
                        prefabName = wantedItem,
                        drop = wantedPrefab.GetComponent<ItemDrop>(),
                        gameObject = new GameObject()
                    };
                } else {                        
                    Transform attach = this.GetAttach(component.m_itemData);
                    var attachPoint = this.chestStand.transform;
                    if(slot.GetAttachPoint(slot) != null) {
                        attachPoint = slot.GetAttachPoint(slot).transform;
                    }
                    var attachedEquipment = UnityEngine.Object.Instantiate<GameObject>(wantedAttachPrefab, attachPoint.position, attachPoint.parent.rotation, attachPoint.parent);
                    
                    slot.visualObject = new VisualEquipment {
                        prefabName = wantedItem,
                        drop = wantedPrefab.GetComponent<ItemDrop>(),
                        gameObject = attachedEquipment
                    };
                    
                    var skinned = slot.bindSkeletonToStand();
                    
                    if(!skinned) {
                        // if it's not skinned, make it look the same way as the attached point
                        attachedEquipment.transform.SetPositionAndRotation(
                            attachPoint.position, 
                            attachPoint.rotation
                        );
                    }

                    foreach (Cloth cloth in attachedEquipment.GetComponentsInChildren<Cloth>()) {
                        //ZLog.Log("Setting cloth colliders");

                        if (this.m_clothColliders.Length != 0) {
                            if (cloth.capsuleColliders.Length != 0)
                            {
                                List<CapsuleCollider> list2 = new List<CapsuleCollider>(m_clothColliders);
                                list2.AddRange(cloth.capsuleColliders);
                                cloth.capsuleColliders = list2.ToArray();
                            }
                            else
                                cloth.capsuleColliders = this.m_clothColliders;
                        }
                    }
                                            
                    // attach may be disabled.
                    attachedEquipment.SetActive(true);

                    // may be item variants                    
                    ItemStyle componentInChildren = attachedEquipment.GetComponentInChildren<ItemStyle>();
                    if (componentInChildren == null)
                        return;
                    componentInChildren.Setup(slot.equippedVariant);
                }                    
            }        
        }

        private void SetVisualItems() {
            foreach(var slotMap in equipmentSlotManager.slots) {
                if(slotMap.Value.needsVisualUpdate()) {
                    ZLog.Log($"Slot {slotMap.Key} dirty, needs re-equipped.");
                    resolveSlot(slotMap.Value);
                }
                if(poseManager.needsVisualUpdate()) {
                    ZLog.Log("Pose manager needs visual update");
                    poseManager.doVisualUpdate();
                }
            }
        }

        private GameObject GetAttachPrefab(GameObject item)
        {
            // helmets will have an attach transform, chests and legs will have an attach_skin transform
            
            Transform transform = item.transform.Find("attach");
            if ((bool)((UnityEngine.Object)transform))
                return transform.gameObject;
            transform = item.transform.Find("attach_skin");
            if ((bool)((UnityEngine.Object)transform))
                return transform.gameObject;

            return null;
        }

        private bool CanAttach(ItemDrop.ItemData item)
        {   
            return this.m_supportedTypes.Contains(item.m_shared.m_itemType);
        }

        public bool IsUnsupported(ItemDrop.ItemData item)
        {
            foreach (ItemDrop itemDrop in this.m_unsupportedItems)
            {
                if (itemDrop.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    return true;
            }
            return false;
        }

        public bool IsSupported(ItemDrop.ItemData item)
        {
            if (this.m_supportedItems.Count == 0)
                return true;
            foreach (ItemDrop itemDrop in this.m_supportedItems)
            {
                if (itemDrop.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    return true;
            }
            return false;
        }

        public bool HaveAttachment()
        {
            try {    
                if (!this.m_nview.IsValid())
                    return false;

                foreach(var slot in ZDOVars.All()) {
                    if(!String.IsNullOrEmpty(zdo.GetString(slot))) {
                        return true;
                    }
                }                                            
            }catch(Exception exc) {
                ZLog.Log(exc);
            }

            return false;
        }
    }
}