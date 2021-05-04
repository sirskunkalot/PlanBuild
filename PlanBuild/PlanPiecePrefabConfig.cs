using BepInEx.Logging;
using HarmonyLib;
using JotunnLib.Entities;
using JotunnLib.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild
{
    public class PlanPiecePrefabConfig : PrefabConfig
    {
        public static ManualLogSource logger;
        public const string plannedSuffix = "_planned";
        public Piece originalPiece;
        public static bool logPiece = true;
        public static bool logComponents = false;
        public static readonly Dictionary<Piece, Piece> planToOriginalMap = new Dictionary<Piece, Piece>();
        public PlanPiecePrefabConfig(Piece piece) : base(piece.name + plannedSuffix, piece.name)
        {
            this.originalPiece = piece;
        }

        private string pieceName;
        public Piece planPiece;

        public override void Register()
        {
            logger.LogInfo("Creating planned version of " + BasePrefabName);
            planPiece = Prefab.GetComponent<Piece>();
            planPiece.m_name = Localization.instance.Localize("$item_plan_piece_name", planPiece.m_name);
            planPiece.m_description = Localization.instance.Localize("$item_plan_piece_description", originalPiece.m_name);
            planPiece.m_resources = new Piece.Requirement[0];
            planPiece.m_craftingStation = null;
            planPiece.m_placeEffect.m_effectPrefabs = new EffectList.EffectData[0];


            WearNTear wearNTear = Prefab.GetComponent<WearNTear>();
            if (wearNTear == null)
            {
                logger.LogInfo("Piece without WearNTear, adding for planned version");
                wearNTear = Prefab.AddComponent<WearNTear>();
            }
            wearNTear.m_noSupportWear = true;
            wearNTear.m_noRoofWear = false;
            wearNTear.m_autoCreateFragments = false;
            wearNTear.m_supports = true;
            wearNTear.m_hitEffect = new EffectList();

            PlanPiece planPieceScript = Prefab.AddComponent<PlanPiece>();
            planPieceScript.originalPiece = originalPiece;
            planToOriginalMap.Add(planPiece, originalPiece);
            if (logComponents)
            {
                StringBuilder sb = new StringBuilder("Components in prefab: " + Prefab.name + "\n");
                sb.Append("Components in prefab: " + Prefab.name + "\n");
                sb.Append($" Prefab: {Prefab.name} -> {Prefab.gameObject}\n");
                foreach (Component component in Prefab.GetComponents<Component>())
                {
                    sb.Append($" {component.GetType()} -> {component.name}\n");
                }
                logger.LogWarning(sb.ToString());
            }

            DisablePiece(Prefab);
        }

        private static readonly List<Type> typesToDestroyInChildren = new List<Type>()
            {
                typeof(GuidePoint),
                typeof(Light),
                typeof(LightLod),
                 typeof(Smelter),
                typeof(Interactable),
                typeof(Hoverable)
            };

        public static int m_planLayer = LayerMask.NameToLayer("piece_nonsolid");
        public static int m_placeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");

        public void DisablePiece(GameObject gameObject)
        { 
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
          
        internal void RegisterPiece()
        {
            PieceManager.Instance.RegisterPiece(PlanHammerPrefabConfig.pieceTableName, planPiece.name);
        }
    }

    [HarmonyPatch(declaringType: typeof(Player), methodName: "HaveRequirements", argumentTypes: new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
    class Player_HaveRequirements_Patch
    {

        static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (PlanBuildMod.showAllPieces.Value)
            {
                return true;
            }
            if (PlanPiecePrefabConfig.planToOriginalMap.TryGetValue(piece, out Piece originalPiece))
            {
                __result = __instance.HaveRequirements(originalPiece, Player.RequirementMode.IsKnown);
                return false;
            }
            return true;
        }

    }

}
