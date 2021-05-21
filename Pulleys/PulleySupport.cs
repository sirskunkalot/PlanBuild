using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pulleys
{
    class PulleySupport : MonoBehaviour
    {
        private const float PulleyUpdateTime = 2f;
        public static readonly KeyValuePair<int, int> PulleyBaseHash = ZDO.GetHashZDOID("marcopogo.PulleyBase");
        internal ZNetView m_nview; 
        private Pulley m_pulley;
        private int m_pieceRayMask;
        private Transform m_pivot;  

        public void Awake()
        {

            m_pieceRayMask = LayerMask.GetMask("piece");
            m_pivot = transform.Find("New/pivot");
            m_nview = GetComponent<ZNetView>();
            WearNTear wearNTear = GetComponent<WearNTear>();
            wearNTear.m_onDestroyed += OnDestroyed;
            if(m_nview && m_nview.IsValid())
            { 
                InvokeRepeating("LookForSupport", PulleyUpdateTime, PulleyUpdateTime);
            }
        }

        public void LookForSupport()
        {
            if (!m_pulley && m_nview && m_nview.IsValid())
            {
                ZDOID pulleyID = m_nview.GetZDO().GetZDOID(PulleyBaseHash);
                if (pulleyID != ZDOID.None)
                {
                    Jotunn.Logger.LogDebug("Looking for pulley " + pulleyID);
                    GameObject pulleyObject = ZNetScene.instance.FindInstance(pulleyID);
                    if (pulleyObject)
                    {
                        SetPulley(pulleyObject.GetComponent<Pulley>());
                    } 
                }
            }
            if (!m_pulley && Physics.Raycast(transform.position - transform.up, transform.up * -1, out var hitInfo, 2000f, m_pieceRayMask))
            {
                SetPulley(hitInfo.collider.GetComponentInParent<Pulley>());
            }
            if(m_pulley)
            {
                CancelInvoke("LookForSupport");
            }
        }

        private void OnDestroyed()
        {
            m_pulley?.SupportDestroyed(this);
        }
          
        private void AttachRopes()
        {
            AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back");
        }

        internal void PulleyBaseDestroyed(Pulley pulley)
        {
            if(m_pulley != pulley)
            {
                Jotunn.Logger.LogWarning("Invalid callback from " + pulley.GetZDOID() + " to " + this.GetZDOID() + ", expected " + m_pulley?.GetZDOID());
                return;
            }
            m_pulley = null;
            RemoveRopes();
            InvokeRepeating("LookForSupport", PulleyUpdateTime, PulleyUpdateTime);
        }
         
        public void SetPulley(Pulley pulley)
        {
            if(!pulley)
            {
                return;
            }
            m_pulley = pulley;
            AttachRopes();
            pulley.SetSupport(this);
            m_nview.GetZDO().Set(PulleyBaseHash, pulley.GetZDOID());
        }

        private void RemoveRopes()
        {
            foreach(Rope rope in ropes)
            {
                rope.Clear();
            }
            ropes.Clear();
        }


        internal bool IsConnected()
        {
            return m_pulley != null;
        }

        class Rope
        { 
            public Transform top;
            public Transform bottom;
            public LineRenderer lineRenderer;

            internal void Clear()
            {
                lineRenderer.enabled = false;
            }

            internal void Update(Pulley pulley)
            {
                lineRenderer.enabled = pulley != null;
                if(lineRenderer.enabled)
                { 
                    lineRenderer.SetPositions(new Vector3[] { top.position, bottom.position }); 
                }
            }
        }

        private readonly List<Rope> ropes = new List<Rope>();

        private void AttachRopes(params string[] pointNames)
        { 
            //Not sure why y -> z ...
            float z = transform.rotation.eulerAngles.y - m_pulley.transform.rotation.eulerAngles.y;
            m_pivot.localEulerAngles = new Vector3(0f, 0f, z); 
            foreach (string pointName in pointNames)
            {
                Transform topAttach = gameObject.transform.Find("New/pivot/" + pointName);
                Transform bottomAttach = m_pulley.transform.Find(pointName);
                ropes.Add(new Rope()
                {  
                    top = topAttach,
                    bottom = bottomAttach,
                    lineRenderer = topAttach.GetComponent<LineRenderer>()
                }) ;
            }
        }

        internal ZDOID GetZDOID()
        {
            if (!m_nview || !m_nview.IsValid())
            {
                return ZDOID.None;
            }
            return m_nview.m_zdo.m_uid;
        }

        public void LateUpdate()
        {  
            foreach(Rope rope in ropes)
            {
                rope.Update(m_pulley);
            }  
        }

        internal bool CanBeRemoved()
        {
            if(!m_pulley)
            {
                return true;
            }
            return m_pulley.CanBeRemoved();
        }
    }
}
