using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    internal class PlanTotem : Container
    {
        public static readonly List<PlanTotem> m_allPlanTotems = new List<PlanTotem>();

        private CircleProjector m_areaMarker;
        private GameObject m_activeMarker;
        private MeshRenderer m_model;
        private int m_supportedPieces = 0;
        static internal GameObject m_connectionPrefab;
        private Bounds m_chestBounds;
        private List<KeyValuePair<string, int>> m_sortedRequired;
        private readonly List<PlanPiece> m_connectedPieces = new List<PlanPiece>();
        private readonly Dictionary<string, int> m_remainingRequirements = new Dictionary<string, int>();
        private readonly HashSet<string> m_missingCraftingStations = new HashSet<string>();

        #region Container Override

        static PlanTotem()
        {
            On.Container.GetHoverText += OnContainerHoverText;
            On.Container.Interact += OnContainerInteract;
        }

        private static bool OnContainerInteract(On.Container.orig_Interact orig, Container self, Humanoid character, bool hold, bool alt)
        {
            PlanTotem planTotem = self as PlanTotem;
            if (planTotem && !hold && ZInput.GetButton("Crouch") && !self.IsInUse())
            {
                planTotem.m_nview.InvokeRPC("ToggleEnabled");
                return true;
            }
            return orig(self, character, hold, alt);
        }

        private static string OnContainerHoverText(On.Container.orig_GetHoverText orig, Container self)
        {
            PlanTotem planTotem = self as PlanTotem;
            if (planTotem)
            {
                return planTotem.GetHoverText();
            }
            return orig(self);
        }

        #endregion Container Override

        public new void Awake()
        {
            base.Awake(); 
            StartCoroutine(UpdatePlanTotem());
            m_areaMarker = GetComponentInChildren<CircleProjector>(true);
            m_activeMarker = transform.Find("new/pivot").gameObject;
            m_model = transform.Find("new/totem").GetComponent<MeshRenderer>();
            m_areaMarker.m_radius = PlanConfig.RadiusConfig.Value;
            m_chestBounds = transform.Find("new/chest/privatechest").GetComponent<BoxCollider>().bounds;
            m_allPlanTotems.Add(this);
            if(m_nview)
            {
                m_nview.Register("ToggleEnabled", RPC_ToggleEnabled);
            }
            HideMarker();
        }

        internal void Replace(GameObject gameObject, PlanPiecePrefab planPrefab)
        {
            Transform replaceTransform = gameObject.transform;
            string textReceiver = gameObject.GetComponent<TextReceiver>()?.GetText();
            GameObject created = PlanPiece.SpawnPiece(gameObject, m_piece.m_creator, replaceTransform.position, replaceTransform.rotation, planPrefab.PiecePrefab, textReceiver);
            TriggerConnection(created.transform.position);
        }
         
        public void OnDestroy()
        {
            m_allPlanTotems.Remove(this);
        }

        public bool InRange(GameObject go)
        {
            return GetDistanceTo(go) <= PlanConfig.RadiusConfig.Value;
        }

        private float GetDistanceTo(GameObject go)
        {
            Vector3 pieceCenter = GetCenter(go);
            float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(pieceCenter.x, pieceCenter.z));
            return distance;
        }

        private List<PlanPiece> FindPlanPiecesInRange()
        {
            Dictionary<PlanPiece, float> result = new Dictionary<PlanPiece, float>();
            foreach (var piece in Piece.m_allPieces)
            {
                try
                {
                    float distance = GetDistanceTo(piece.gameObject);
                    if (distance <= PlanConfig.RadiusConfig.Value)
                    {
                        PlanPiece planPiece = piece.GetComponent<PlanPiece>();
                        if (planPiece)
                        {
                            result.Add(planPiece, distance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"Exception caught while collecting plan piece {piece}: {ex}");
                }
            }
            return result.AsEnumerable()
                .OrderByDescending(pair => pair.Key.m_maxSupport)
                .ThenBy(pair => pair.Key.transform.position.y)
                .ThenBy(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList();
        }

        public IEnumerator<YieldInstruction> UpdatePlanTotem()
        {
            if (!m_nview || !m_nview.IsValid())
            {
                yield break;
            }

            while(true)
            {
                yield return new WaitForSeconds(3f);
                if(!GetEnabled())
                {
                    continue;
                }

                int newSupportedPieces = 0;
                List<PlanPiece> connectedPieces = new List<PlanPiece>();
                Dictionary<string, int> newRemainingRequirements = new Dictionary<string, int>();
                HashSet<string> newMissingCraftingStations = new HashSet<string>();

                m_supportedPieces = 0;
                m_connectedPieces.Clear();
                
                m_missingCraftingStations.Clear();

                List<PlanPiece> planPieces = FindPlanPiecesInRange();
                foreach (var planPiece in planPieces)
                {

                    if (planPiece.HasSupport())
                    {
                        m_supportedPieces++;
                    }

                    if (m_nview.IsOwner() && planPiece.HasSupport())
                    {
                        if (m_inventory.m_inventory.Count != 0)
                        {
                            planPiece.AddAllMaterials(m_inventory);
                        }
                        if (planPiece.HasAllResources())
                        {
                            if (planPiece.HasRequiredCraftingStationInRange())
                            {
                                if (PlanConfig.ShowParticleEffects.Value)
                                {
                                    TriggerConnection(GetCenter(planPiece.gameObject));
                                }
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
                       .Select(pair =>
                       {
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

        internal void RPC_ToggleEnabled(long sender)
        {
            if(m_nview.IsOwner())
            {
                SetEnabled(!GetEnabled());
            }
        }

        private void SetEnabled(bool enabled)
        {
            if(m_nview && m_nview.IsValid())
            {
                m_nview.GetZDO().Set("enabled", enabled);
            }
            SetActive(enabled);
        }

        internal bool GetEnabled()
        {
            if (m_nview && m_nview.IsValid())
            {
                return m_nview.GetZDO().GetBool("enabled", true);
            }
            return false;
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
            bool enabled = GetEnabled();
            StringBuilder sb = new StringBuilder($"$piece_plan_totem {(enabled ? "" : "(<color=red>$piece_plan_totem_disabled</color>)")}\n" +
                $"[<color=yellow>$KEY_Use</color>] $piece_container_open\n" +
                $"[<color=yellow>$KEY_Crouch + $KEY_Use</color>] {(enabled ? "$piece_plan_totem_disable" : "$piece_plan_totem_enable")}\n"+ 
                $"\n");
            if (m_missingCraftingStations.Count > 0)
            {
                sb.Append($"$piece_plan_totem_missing \n");
                foreach (string missingStation in m_missingCraftingStations)
                {
                    sb.Append($"<color=red>{missingStation}</color>\n");
                }
            }
            sb.Append(Localization.instance.Localize("$piece_plan_totem_connected \n", m_connectedPieces.Count.ToString(), m_supportedPieces.ToString()));

            if (m_remainingRequirements.Count > 0)
            {
                sb.Append("$piece_plan_totem_required\n");
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
            timedDestruction.Trigger(vector.magnitude);
            m_connection.transform.position = center;
            m_connection.transform.rotation = Quaternion.LookRotation(vector.normalized);
            m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
        }
    }
}