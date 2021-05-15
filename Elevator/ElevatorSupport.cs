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
        public static int zdoElevatorID = "elevatorID".GetStableHashCode();
        internal static GameObject elevatorPrefab;
        private ZNetView m_nview;
        private GameObject elevatorObject;
        private Elevator elevator;

        public void Awake()
        {
            m_nview = GetComponent<ZNetView>(); 
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                int elevatorID = GetElevatorID();
                if (elevatorID != 0)
                {
                    Jotunn.Logger.LogDebug("Looking for elevator " + elevatorID);
                    foreach (Elevator worldElevator in FindObjectsOfType<Elevator>())
                    {
                        if (worldElevator.GetElevatorID() == elevatorID)
                        {
                            Jotunn.Logger.LogDebug("Found elevator");
                            elevatorObject = worldElevator.gameObject;
                            elevator = worldElevator;
                            break;
                        }
                    }
                }
                if (elevatorObject == null)
                {
                    Jotunn.Logger.LogDebug("Spawning elevator");
                    elevatorObject = Instantiate(elevatorPrefab, transform.position + (transform.up * -3f), transform.rotation);
                    Jotunn.Logger.LogDebug(GetInstanceID() + ": Spawned " + elevatorObject.GetInstanceID());
                    elevator = elevatorObject.GetComponent<Elevator>();
                    elevator.SetSupport(this);
                    elevatorID = elevatorObject.GetInstanceID();
                    m_nview.GetZDO().Set(zdoElevatorID, elevatorID);
                }
                if(elevatorObject != null )
                {
                    AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back");
                }
            }
        }

        class Rope
        {
            public Transform top;
            public Transform bottom;
            public LineRenderer lineRenderer;

            internal void Update(bool enabled)
            {
                lineRenderer.enabled = enabled;
                if(enabled)
                {
                    lineRenderer.SetPositions(new Vector3[] { top.position, bottom.position });
                }
            }
        }

        private List<Rope> ropes = new List<Rope>();

        private void AttachRopes(params string[] pointNames)
        {
            Jotunn.Logger.LogDebug("Attaching ropes");
            foreach (string pointName in pointNames)
            {
                Transform topAttach = gameObject.transform.Find(pointName);
                Transform bottomAttach = elevatorObject.transform.Find(pointName);
                ropes.Add(new Rope()
                {
                    top = topAttach,
                    bottom = bottomAttach,
                    lineRenderer = topAttach.GetComponent<LineRenderer>()
                });
            }
        }

        internal int GetElevatorID()
        {
            return m_nview.GetZDO().GetInt(zdoElevatorID);
        }

        public void LateUpdate()
        { 
            foreach(Rope rope in ropes)
            {
                rope.Update(elevatorObject != null);
            } 
        }
    }
}
