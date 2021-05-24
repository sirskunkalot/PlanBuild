using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanBuild.PlanBuild
{
    class PlanTotem : Container
    {
        public float m_radius = 10f;
        private GameObject m_areaMarker;
        private readonly List<PlanPiece> m_connectedPieces = new List<PlanPiece>();
        private readonly Dictionary<string, int> m_remainingRequirements = new Dictionary<string, int>();

        #region Container Override
        static PlanTotem() {
            On.Container.GetHoverText += OnContainerHoverText; 
        }
         
        private static string OnContainerHoverText(On.Container.orig_GetHoverText orig, Container self)
        {
            PlanTotem planTotem = self as PlanTotem;
            if(planTotem)
            {
                return planTotem.GetHoverText();
            }
            return orig(self);
        } 
        #endregion

        public new void Awake()
        {
            base.Awake();
            InvokeRepeating("UpdatePlanTotem", 1f, 1f);
            m_areaMarker = GetComponentInChildren<CircleProjector>().gameObject;
        }

        public List<PlanPiece> FindPlanPiecesInRange()
        {
            Dictionary<PlanPiece, float> result = new Dictionary<PlanPiece, float>();
            foreach (var piece in Piece.m_allPieces)
            {
                float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(piece.transform.position.x, piece.transform.position.z));
                if (distance < m_radius)
                {
                    PlanPiece planPiece = piece.GetComponent<PlanPiece>();
                    if (planPiece)
                    {
                        result.Add(planPiece, distance);
                    }
                }
            }
            return result.AsEnumerable()
                .OrderBy(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList();
        }
          
        public void UpdatePlanTotem()
        {
            if(!m_nview || !m_nview.IsValid())
            {
                return;
            }
            m_connectedPieces.Clear();
            m_remainingRequirements.Clear(); 
            foreach (var planPiece in FindPlanPiecesInRange())
            {
                if (m_nview.IsOwner() && planPiece.hasSupport)
                {
                    planPiece.AddAllMaterials(m_inventory);
                    if (planPiece.HasAllResources())
                    {
                        Jotunn.Logger.LogInfo("Auto Building " + planPiece);
                        planPiece.Build(m_piece.m_creator);
                        continue;
                    }
                }

                m_connectedPieces.Add(planPiece);
                Dictionary<string, int> remaining = planPiece.GetRemaining();
                foreach (string resourceName in remaining.Keys)
                {
                    int resourceCount = remaining[resourceName];
                    if (m_remainingRequirements.TryGetValue(resourceName, out int currentCount))
                    {
                        resourceCount += currentCount;
                    }
                    m_remainingRequirements[resourceName] = resourceCount;
                }
            }
        }
 
        public void ShowAreaMarker()
        {
            if (m_areaMarker)
            {
                m_areaMarker.SetActive(value: true);
                CancelInvoke("HideMarker");
                Invoke("HideMarker", 0.5f); 
            }
        }

        public void HideMarker()
        {
            m_areaMarker.SetActive(value: false);
        }

        public new string GetHoverText()
        {
            ShowAreaMarker();
            string text = $"{m_connectedPieces.Count} connected plans\n";
            foreach (string resourceName in m_remainingRequirements.Keys)
            {
                text += $"{resourceName}: {m_remainingRequirements[resourceName]} remaining\n";
            }
            return Localization.instance.Localize(text);
        } 
    }
}
