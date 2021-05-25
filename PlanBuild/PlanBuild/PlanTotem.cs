using BepInEx.Configuration;
using PlanBuild.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.PlanBuild
{
    class PlanTotem : Container
    {
        public static readonly List<PlanTotem> m_allPlanTotems = new List<PlanTotem>();
         
        private CircleProjector m_areaMarker;
        private GameObject m_activeMarker;
        private MeshRenderer m_model;
        private int m_supportedPieces = 0;
        static internal GameObject m_connectionPrefab;
        private Bounds m_chestBounds;
        internal static ConfigEntry<float> radiusConfig;
        private List<KeyValuePair<string, int>> m_sortedRequired;
        private readonly List<PlanPiece> m_connectedPieces = new List<PlanPiece>();
        private readonly Dictionary<string, int> m_remainingRequirements = new Dictionary<string, int>();
        private readonly HashSet<string> m_missingCraftingStations = new HashSet<string>();

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
            m_areaMarker = GetComponentInChildren<CircleProjector>(true);
            m_activeMarker = transform.Find("new/pivot").gameObject;
            m_model = transform.Find("new/totem").GetComponent<MeshRenderer>();
            m_areaMarker.m_radius = radiusConfig.Value;
            m_chestBounds = transform.Find("new/chest/privatechest").GetComponent<BoxCollider>().bounds;
            m_allPlanTotems.Add(this);
            HideMarker();
        }

        public void OnDestroy()
        {
            m_allPlanTotems.Remove(this);
        }

        public List<PlanPiece> FindPlanPiecesInRange()
        {
            Dictionary<PlanPiece, float> result = new Dictionary<PlanPiece, float>();
            foreach (var piece in Piece.m_allPieces)
            {
                Vector3 pieceCenter = GetCenter(piece.gameObject);
                float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(pieceCenter.x, pieceCenter.z));
                if (distance <= radiusConfig.Value)
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
            m_missingCraftingStations.Clear();
            foreach (var planPiece in FindPlanPiecesInRange())
            {
                if (m_nview.IsOwner() && planPiece.hasSupport)
                {
                    planPiece.AddAllMaterials(m_inventory);
                    if (planPiece.HasAllResources())
                    {
                        if (planPiece.HasRequiredCraftingStationInRange())
                        {
                            TriggerConnection(GetCenter(planPiece.gameObject));
                            planPiece.Build(m_piece.m_creator);
                            continue;
                        }
                        else
                        {
                            m_missingCraftingStations.Add(planPiece.originalPiece.m_craftingStation.m_name);
                        }
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
            m_sortedRequired = m_remainingRequirements
                   .Select(pair => {
                       int missing = pair.Value;
                       if (pair.Value > 0)
                       {
                           missing -= m_inventory.CountItems(pair.Key);
                       }
                       return new KeyValuePair<string, int>(pair.Key, missing);
                   })
                   .Where(pair => pair.Value > 0)
                   .OrderByDescending(pair => pair.Value)
                   .ToList();
            bool active = m_connectedPieces.Count > 0;
            SetActive(active);
        }

        private void SetActive(bool active)
        {
            m_activeMarker.SetActive(active);
            Material[] materials = m_model.materials;
            foreach (Material material in materials)
            {
                if (active)
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }
        }

        public void ShowAreaMarker()
        {
            if (m_areaMarker)
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

        public new string GetHoverText()
        {
            ShowAreaMarker();
            StringBuilder sb = new StringBuilder($"Plan totem\n" +
                $"[<color=yellow>$KEY_Use</color>] Open\n" +
                $"\n");
            if(m_missingCraftingStations.Count > 0)
            {
                sb.Append($"Missing crafting stations: \n");
                foreach(string missingStation in m_missingCraftingStations)
                {
                    sb.Append($"<color=red>{missingStation}</color>\n");                
                }
            }
            sb.Append($"{m_connectedPieces.Count} connected plans ({m_supportedPieces} supported)\n");
            if(m_remainingRequirements.Count > 0)
            {
                sb.Append("Required materials:\n"); 
                foreach (var pair in m_sortedRequired)
                {
                    sb.Append($" <color=yellow>{pair.Value}</color> {pair.Key}\n");
                }
            }
            return Localization.instance.Localize(sb.ToString());
        } 

        public Vector3 GetCenter(GameObject target)
        {
           Collider[] m_colliders = target.GetComponentsInChildren<Collider>();
            
           Vector3 position = target.transform.position;
           Collider[] colliders = m_colliders;
           foreach (Collider collider in colliders)
           {
                position = (position + collider.bounds.center) / 2;
           }
           return position;
        }

        public void TriggerConnection(Vector3 targetPos)
        {
            Vector3 center = m_chestBounds.center;
         
            GameObject m_connection = Object.Instantiate(m_connectionPrefab, center, Quaternion.identity);

            TimedDestruction timedDestruction = m_connection.AddComponent<TimedDestruction>();

            Vector3 vector = targetPos - center;
            Quaternion rotation = Quaternion.LookRotation(vector.normalized);
            timedDestruction.Trigger(vector.magnitude);
            m_connection.transform.position = center;
            m_connection.transform.rotation = rotation;
            m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude); 
        } 
    }
}
