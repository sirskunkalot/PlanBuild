// AirShips
// a Valheim mod skeleton using Jötunn
// 
// File:    AirShips.cs
// Project: AirShips

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AirShips
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("BepIn.Sarcen.ValheimRAFT")]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class AirshipsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.Airships";
        public const string PluginName = "AirShips";
        public const string PluginVersion = "0.0.1";
        private CustomPiece airshipPiece;
        private int pieceLayer;
        private int itemLayer;
        private int vehicleLayer;
        private Harmony harmony;

        private void Awake()
        {
            pieceLayer = LayerMask.NameToLayer("piece");
            itemLayer = LayerMask.NameToLayer("item");
            vehicleLayer = LayerMask.NameToLayer("vehicle");

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(AirshipControlls));
            harmony.PatchAll(typeof(AirshipsPlugin));

            ItemManager.OnVanillaItemsAvailable += AddCustomPrefabs;
            PieceManager.OnPiecesRegistered += UpdatePrefabs;
        }

        public void OnDestroy()
        {
            harmony?.UnpatchAll(PluginGUID);
        }

        private void UpdatePrefabs()
        {
            try
            { 
                var stoneGolemTrophyPrefab = PrefabManager.Instance.GetPrefab("TrophySGolem");
                var surtlingCorePrefab = PrefabManager.Instance.GetPrefab("SurtlingCore");
                var raftPrefab = PrefabManager.Instance.GetPrefab("Raft");
                Ship raftShip = raftPrefab.GetComponent<Ship>();
                ShipControlls raftShipControlls = raftPrefab.GetComponentInChildren<ShipControlls>();
             
                GameObject shipObject = Object.Instantiate(new GameObject("ship"), airshipPiece.PiecePrefab.transform);
                GameObject collidersObject = Object.Instantiate(new GameObject("colliders"), shipObject.transform);
                collidersObject.layer = vehicleLayer;
                var colliderObject = airshipPiece.PiecePrefab.transform.Find("collider").gameObject;
                colliderObject.transform.SetParent(collidersObject.transform);
                colliderObject.name = "Cube";
                colliderObject.layer = vehicleLayer;
                BoxCollider boxCollider = colliderObject.GetComponent<BoxCollider>();
                boxCollider.material = raftPrefab.transform.Find("ship/colliders/Cube").GetComponent<BoxCollider>().material;
                
                WearNTear wearNTear = airshipPiece.Piece.GetComponent<WearNTear>();
                wearNTear.m_noSupportWear = false;
                wearNTear.m_supports = true;

                airshipPiece.Piece.m_icon = stoneGolemTrophyPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];
                 
                airshipPiece.PiecePrefab.AddComponent<AirshipBase>();
                Airship airship = airshipPiece.PiecePrefab.AddComponent<Airship>();
                airship.m_controlGuiPos = raftShip.m_controlGuiPos;
                AddVisualPiece(stoneGolemTrophyPrefab, "crystal1", new Vector3(1f, 0f, -1f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
                var surtlingCore = AddVisualPiece(surtlingCorePrefab, "surtlingCore", new Vector3(1f, 1f, 0f), Quaternion.Euler(0f, 0f, 0f), Vector3.one);
                AirshipControlls airshipControlls = surtlingCore.AddComponent<AirshipControlls>();
                airshipControlls.m_attachAnimation = raftShipControlls.m_attachAnimation;
                airshipControlls.m_maxUseRange = raftShipControlls.m_maxUseRange;
                
                AddVisualPiece(stoneGolemTrophyPrefab, "crystal2", new Vector3(1f, 0f, 1f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            } finally
            {
                PieceManager.OnPiecesRegistered -= UpdatePrefabs;
            }
        }

        private GameObject AddVisualPiece(GameObject stoneGolemTrophyPrefab , string snapPointName, Vector3 location,  Quaternion rotation, Vector3 scale)
        {
            var visualPiece = Object.Instantiate(stoneGolemTrophyPrefab, airshipPiece.PiecePrefab.transform);
            visualPiece.transform.localPosition = location;
            visualPiece.transform.rotation = rotation;
            visualPiece.transform.localScale = scale;
            Object.DestroyImmediate(visualPiece.GetComponent<ItemDrop>());
            Object.DestroyImmediate(visualPiece.GetComponent<ParticleSystem>());
            Object.DestroyImmediate(visualPiece.GetComponent<Rigidbody>());
            Object.DestroyImmediate(visualPiece.GetComponent<ZNetView>());
            Object.DestroyImmediate(visualPiece.GetComponent<ZSyncTransform>());
            Transform dustTransform = visualPiece.transform.Find("attach/dust");
            if(dustTransform != null)
            {
                Object.DestroyImmediate(dustTransform.gameObject);
            }
            Transform particleSystemTransform = visualPiece.transform.Find("attach/Particle System");
            if(particleSystemTransform != null)
            {
                ParticleSystem particleSystem = particleSystemTransform.GetComponent<ParticleSystem>();
                ParticleSystem.ShapeModule shape = particleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Cone; 
                particleSystemTransform.localRotation = Quaternion.Euler(225f, 0f, 180f);
            }
            visualPiece.name = snapPointName;
            visualPiece.layer = pieceLayer;
            foreach (Transform childTransform in visualPiece.GetComponentsInChildren<Transform>())
            {
                if(childTransform.gameObject.layer == itemLayer)
                {
                    childTransform.gameObject.layer = pieceLayer;
                }
            }
            return visualPiece;
        }
         
        private void AddCustomPrefabs()
        {
            try
            { 
                airshipPiece = new CustomPiece("AirshipBase", "wood_floor", "Hammer");
                airshipPiece.Piece.m_category = Piece.PieceCategory.Misc;
                airshipPiece.Piece.m_name = "$item_airshipbase";
                airshipPiece.Piece.m_description = "$item_airshipbase_desc";
                airshipPiece.Piece.m_waterPiece = true; 
                PieceManager.Instance.AddPiece(airshipPiece);
            } finally
            {
                ItemManager.OnVanillaItemsAvailable -= AddCustomPrefabs;
            }
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            { // Set a breakpoint here to break on F9 key press
                Jotunn.Logger.LogInfo("Right here");
            }
        }
#endif
    }
}