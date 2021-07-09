using Jotunn.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    internal class PlanPiecePrefab : CustomPiece
    {
        public const string PlannedSuffix = "_planned";
        public const string PieceTableName = "_planHammerPieceTable";
        public Piece originalPiece;
        public static bool logPiece = true;
        public static bool logComponents = false;
        public static readonly Dictionary<Piece, Piece> planToOriginalMap = new Dictionary<Piece, Piece>();

        public PlanPiecePrefab(Piece piece) : base(piece.gameObject.name + PlannedSuffix, piece.gameObject.name, PieceTableName)
        {
            this.originalPiece = piece;
            Piece.m_name = Localization.instance.Localize("$item_plan_piece_name", originalPiece.m_name);
            Piece.m_description = Localization.instance.Localize("$item_plan_piece_description", originalPiece.m_name);
            Piece.m_resources = new Piece.Requirement[0];
            Piece.m_craftingStation = null;
            Piece.m_placeEffect.m_effectPrefabs = new EffectList.EffectData[0];
            Piece.m_comfort = 0;
            Piece.m_canBeRemoved = true;

            Piece.m_category = originalPiece.m_category;
            Piece.m_groundOnly = originalPiece.m_groundOnly;
            Piece.m_groundPiece = originalPiece.m_groundPiece;
            Piece.m_icon = originalPiece.m_icon;
            Piece.m_inCeilingOnly = originalPiece.m_inCeilingOnly;
            Piece.m_isUpgrade = originalPiece.m_isUpgrade;
            Piece.m_haveCenter = originalPiece.m_haveCenter;
            Piece.m_dlc = originalPiece.m_dlc;
            Piece.m_allowAltGroundPlacement = originalPiece.m_allowAltGroundPlacement;
            Piece.m_allowedInDungeons = originalPiece.m_allowedInDungeons;
            Piece.m_randomTarget = false;
            Piece.m_primaryTarget = false;

            this.PieceTable = PieceTableName;

            WearNTear wearNTear = PiecePrefab.GetComponent<WearNTear>();
            if (wearNTear == null)
            {
                wearNTear = PiecePrefab.AddComponent<WearNTear>();
            }

            wearNTear.m_noSupportWear = true;
            wearNTear.m_noRoofWear = false;
            wearNTear.m_autoCreateFragments = false;
            wearNTear.m_supports = true;
            wearNTear.m_hitEffect = new EffectList();
            wearNTear.m_destroyedEffect = new EffectList();
            wearNTear.m_destroyNoise = 0f;
            wearNTear.m_hitNoise = 0f;

            PlanPiece planPieceScript = PiecePrefab.AddComponent<PlanPiece>();
            planPieceScript.originalPiece = originalPiece;
            planToOriginalMap.Add(Piece, originalPiece);
            DisablePiece(PiecePrefab);
        }

        private static readonly List<Type> typesToDestroyInChildren = new List<Type>()
            {
                typeof(GuidePoint),
                typeof(Light),
                typeof(LightLod),
                typeof(LightFlicker),
                typeof(Smelter),
                typeof(Interactable),
                typeof(Hoverable)
            };

        public static int planLayer = LayerMask.NameToLayer("piece_nonsolid");
        public static int m_placeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");

        public void DisablePiece(GameObject gameObject)
        {
            gameObject.layer = planLayer;
            Transform playerBaseTransform = gameObject.transform.Find("PlayerBase");
            if (playerBaseTransform)
            {
                Object.Destroy(playerBaseTransform.gameObject);
            }

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
        }
    }
}