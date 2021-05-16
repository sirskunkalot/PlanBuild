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
        internal ZNetView m_nview;
        private GameObject m_elevatorObject;
        private Elevator m_evelator;

        public void Awake()
        {
            m_nview = GetComponent<ZNetView>(); 
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                ZDOID elevatorID = m_nview.GetZDO().GetZDOID(ElevatorBaseHash);
                if (elevatorID != ZDOID.None)
                {
                    Jotunn.Logger.LogDebug("Looking for elevator " + elevatorID);
                    m_elevatorObject = ZNetScene.instance.FindInstance(elevatorID);
                    if(m_elevatorObject)
                    {
                        m_evelator = m_elevatorObject.GetComponent<Elevator>();
                    } else
                    {
                        Jotunn.Logger.LogWarning("ZDO stored elevator not found: " + elevatorID);
                    }
                }  
                
            }
        }

        public void SetElevatorBase(Elevator elevator)
        {
            m_nview.GetZDO().Set(ElevatorBaseHash, elevator.GetElevatorID());
            this.m_evelator = elevator;
            m_elevatorObject = elevator.gameObject; 
            AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back"); 
        }

        public void Update()
        {
            if(!m_evelator)
            {
                ZDOID elevatorID = m_nview.GetZDO().GetZDOID(ElevatorBaseHash);
                if (elevatorID != ZDOID.None)
                {
                    Jotunn.Logger.LogDebug("Looking for elevator " + elevatorID);
                    m_elevatorObject = ZNetScene.instance.FindInstance(elevatorID);
                    if (m_elevatorObject)
                    {
                        m_evelator = m_elevatorObject.GetComponent<Elevator>();
                        AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back");
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning("ZDO stored elevator not found: " + elevatorID);
                        m_nview.GetZDO().Set(ElevatorBaseHash, ZDOID.None);
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
                Transform bottomAttach = m_elevatorObject.transform.Find(pointName);
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
                rope.Update(m_evelator);
            } 
        }
    }
}
