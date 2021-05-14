using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elevator
{
    class ElevatorSupport : MonoBehaviour
    {
        private int zdoElevatorID = "elevatorID".GetStableHashCode();
        internal static GameObject elevatorPrefab;
        private ZNetView m_nview;
        private GameObject elevatorObject;
        private LineRenderer m_lineRenderer;

        public void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            m_lineRenderer = GetComponent<LineRenderer>();
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                int elevatorID = m_nview.GetZDO().GetInt(zdoElevatorID);
                if (elevatorID != 0)
                {
                    Jotunn.Logger.LogDebug("Looking for elevator " + elevatorID);
                    foreach (Elevator worldElevator in Object.FindObjectsOfType<Elevator>())
                    {
                        if (worldElevator.GetInstanceID() == elevatorID)
                        {
                            Jotunn.Logger.LogDebug("Found elevator");
                            elevatorObject = worldElevator.gameObject;
                            break;
                        }
                    }
                }
                if (elevatorObject == null)
                {
                    Jotunn.Logger.LogDebug("Spawning elevator");
                    elevatorObject = Object.Instantiate(elevatorPrefab, transform.position + (transform.up * -4f), transform.rotation);
                    elevatorID = elevatorObject.GetInstanceID();
                    m_nview.GetZDO().Set(zdoElevatorID, elevatorID);
                }
                AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "front_attach_right_back");
            }
        }

        private Dictionary<Transform, Transform> attachMap = new Dictionary<Transform, Transform>();

        private void AttachRopes(params string[] pointNames)
        {
            Jotunn.Logger.LogDebug("Attaching ropes");
            foreach (string pointName in pointNames)
            {
                Transform topAttach = gameObject.transform.Find(pointName);
                Transform bottomAttach = elevatorObject.transform.Find(pointName);
                attachMap.Add(topAttach, bottomAttach);
            }
        }

        public void LateUpdate()
        {
            if(elevatorObject)
            {
                m_lineRenderer.enabled = true;
                int i = 0;
                foreach(KeyValuePair<Transform, Transform> pair in attachMap)
                {
                    m_lineRenderer.SetPosition(i++, pair.Key.position);
                    m_lineRenderer.SetPosition(i++, pair.Value.position); 
                }
            } else
            {
                m_lineRenderer.enabled = false;
            }
        }
    }
}
