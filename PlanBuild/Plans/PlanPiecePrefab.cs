using Jotunn;
using Jotunn.Entities;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    internal class PlanPiecePrefab : CustomPiece
    {
        public const string PlannedSuffix = "_planned";
        public Piece OriginalPiece;
        public int OriginalHash;

        public PlanPiecePrefab(Piece piece) : base(piece.gameObject.name + PlannedSuffix, piece.gameObject.name, PlanHammerPrefab.PieceTableName)
        {
            OriginalPiece = piece;
            OriginalHash = piece.name.GetStableHashCode();

            Piece.m_name = Localization.instance.Localize("$item_plan_piece_name", OriginalPiece.m_name);
            Piece.m_description = Localization.instance.Localize("$item_plan_piece_description", OriginalPiece.m_name);
            Piece.m_resources = Array.Empty<Piece.Requirement>();
            Piece.m_craftingStation = null;
            Piece.m_placeEffect.m_effectPrefabs = Array.Empty<EffectList.EffectData>();
            Piece.m_comfort = 0;
            Piece.m_canBeRemoved = true;
            Piece.m_randomTarget = false;
            Piece.m_primaryTarget = false;

            Piece.m_category = OriginalPiece.m_category == Piece.PieceCategory.Max ? Piece.PieceCategory.Misc : OriginalPiece.m_category;
            Piece.m_groundOnly = OriginalPiece.m_groundOnly;
            Piece.m_groundPiece = OriginalPiece.m_groundPiece;
            Piece.m_icon = OriginalPiece.m_icon;
            Piece.m_inCeilingOnly = OriginalPiece.m_inCeilingOnly;
            Piece.m_isUpgrade = OriginalPiece.m_isUpgrade;
            Piece.m_haveCenter = OriginalPiece.m_haveCenter;
            Piece.m_dlc = OriginalPiece.m_dlc;
            Piece.m_allowAltGroundPlacement = OriginalPiece.m_allowAltGroundPlacement;
            Piece.m_allowedInDungeons = OriginalPiece.m_allowedInDungeons;

            PieceTable = PlanHammerPrefab.PieceTableName;

            WearNTear wearNTear = PiecePrefab.GetOrAddComponent<WearNTear>();
            wearNTear.m_noSupportWear = true;
            wearNTear.m_noRoofWear = false;
            wearNTear.m_autoCreateFragments = false;
            wearNTear.m_supports = true;
            wearNTear.m_hitEffect = new EffectList();
            wearNTear.m_hitNoise = 0f;
            wearNTear.m_destroyedEffect = new EffectList();
            wearNTear.m_destroyNoise = 0f;

            PlanPiece planPieceScript = PiecePrefab.AddComponent<PlanPiece>();
            planPieceScript.originalPiece = OriginalPiece;
            DisablePiece(PiecePrefab);
        }

        private static readonly List<Type> TypesToKeepInChildren = new List<Type>()
        {
            typeof(ZNetView),
            typeof(WearNTear),
            typeof(Piece),
            typeof(Interactable),
            typeof(Hoverable),
            typeof(PlanPiece)
        };

        public static int PlanLayer = LayerMask.NameToLayer("piece_nonsolid");
        public static int PlaceRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");

        public void DisablePiece(GameObject gameObject)
        {
            // Set our layer
            gameObject.layer = PlanLayer;

            // Remove all GOs with an effect area
            foreach (var component in gameObject.GetComponentsInChildren<EffectArea>())
            {
                Object.DestroyImmediate(component.gameObject);
            }

            // A Ghost doesn't need fancy scripts
            foreach (var component in gameObject.GetComponentsInChildren<MonoBehaviour>())
            {
                if (!TypesToKeepInChildren.Any(x => x.IsSameOrSubclass(component.GetType())))
                {
                    Object.DestroyImmediate(component);
                }
            }

            /*
            foreach (Type toDestroy in TypesToDestroyInChildren)
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

            foreach (var audioSource in gameObject.GetComponentsInChildren<AudioSource>())
            {
                audioSource.enabled = false;
            }
            foreach (var zsfx in gameObject.GetComponentsInChildren<ZSFX>())
            {
                zsfx.enabled = false;
            }
            Windmill windmill = gameObject.GetComponentInChildren<Windmill>();
            if (windmill != null)
            {
                windmill.enabled = false;
            }
            foreach (var particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                particleSystem.gameObject.SetActive(value: false);
            }*/
        }
    }
}