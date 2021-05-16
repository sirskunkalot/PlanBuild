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
        public static readonly KeyValuePair<int, int> ElevatorBaseHash = ZDO.GetHashZDOID("ElevatorBase");
        internal static GameObject elevatorPrefab;
        internal ZNetView m_nview;
        private GameObject elevatorObject;
        private Elevator elevator;

        public void Awake()
        {
            m_nview = GetComponent<ZNetView>(); 
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                ZDOID elevatorID = m_nview.GetZDO().GetZDOID(ElevatorBaseHash);
                if (elevatorID != ZDOID.None)
                {
                    Jotunn.Logger.LogDebug("Looking for elevator " + elevatorID);
                    elevatorObject = ZNetScene.instance.FindInstance(elevatorID);
                    if(elevatorObject)
                    {
                        elevator = elevatorObject.GetComponent<Elevator>();
                    } else
                    {
                        Jotunn.Logger.LogWarning("ZDO stored elevator not found: " + elevatorID);
                    }
                } else
                {
                    Jotunn.Logger.LogDebug("Spawning elevator");
                    elevatorObject = Instantiate(elevatorPrefab, transform.position + (transform.up * -3f), transform.rotation);
                    elevator = elevatorObject.GetComponent<Elevator>();
                    Jotunn.Logger.LogDebug(GetElevatorSupportID() + ": Spawned " + elevator.GetElevatorID());
                    elevator.SetSupport(this); 
                    m_nview.GetZDO().Set(ElevatorBaseHash, elevator.GetElevatorID());
                }
                if(elevatorObject != null )
                {
                    AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back");
                }
            }
        }

        public void Update()
        {
            if(!elevator)
            {
                ZDOID elevatorID = m_nview.GetZDO().GetZDOID(ElevatorBaseHash);
                if (elevatorID != ZDOID.None)
                {
                    Jotunn.Logger.LogDebug("Looking for elevator " + elevatorID);
                    elevatorObject = ZNetScene.instance.FindInstance(elevatorID);
                    if (elevatorObject)
                    {
                        elevator = elevatorObject.GetComponent<Elevator>();
                        AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back");
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning("ZDO stored elevator not found: " + elevatorID);
                    }
                }
            }
        }

        class Rope
        { 
            public Transform top;
            public Transform bottom;
            public LineRenderer lineRenderer;

            internal void Update(Elevator elevator)
            {
                lineRenderer.enabled = elevator != null;
                if(lineRenderer.enabled)
                { 
                    lineRenderer.SetPositions(new Vector3[] { top.position, bottom.position }); 
                }
            }
        }

        private List<Rope> ropes = new List<Rope>();

        private void AttachRopes(params string[] pointNames)
        { 
            foreach (string pointName in pointNames)
            {
                Transform topAttach = gameObject.transform.Find(pointName);
                Transform bottomAttach = elevatorObject.transform.Find(pointName);
                ropes.Add(new Rope()
                {  
                    top = topAttach,
                    bottom = bottomAttach,
                    lineRenderer = topAttach.GetComponent<LineRenderer>()
                }) ;
            }
        }

        internal ZDOID GetElevatorSupportID()
        {
            return m_nview.m_zdo.m_uid;
        }

        public void LateUpdate()
        { 
            foreach(Rope rope in ropes)
            {
                rope.Update(elevator);
            } 
        }
    }
}
