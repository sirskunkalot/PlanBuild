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
        internal ZNetView m_nview;
        private GameObject m_pulleyObject;
        private Pulley m_pulley;
        private Transform m_pivot;  

        public void Awake()
        { 
            m_pivot = transform.Find("New/pivot");
            m_nview = GetComponent<ZNetView>();
            WearNTear wearNTear = GetComponent<WearNTear>();
            wearNTear.m_onDestroyed += OnDestroyed;
        }

        private void OnDestroyed()
        {
            m_pulley?.SupportDestroyed(this);
        }

        public void SetPulleyBase(Pulley pulley)
        {
            this.m_pulley = pulley;
            m_pulleyObject = pulley.gameObject;
            AttachRopes();
        }

        private void AttachRopes()
        {
            AttachRopes("rope_attach_left_front", "rope_attach_left_back", "rope_attach_right_front", "rope_attach_right_back");
        }

        internal void PulleyBaseDestroyed(Pulley pulley)
        {
            if(m_pulley != pulley)
            {
                Jotunn.Logger.LogWarning("Invalid callback from " + pulley.GetZDOID() + " to " + this.GetZDOID() + ", expected " + m_pulley.GetZDOID());
                return;
            }
            m_pulley = null;
            RemoveRopes();
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
            float z = transform.rotation.eulerAngles.y - m_pulleyObject.transform.rotation.eulerAngles.y;
            m_pivot.localEulerAngles = new Vector3(0f, 0f, z); 
            foreach (string pointName in pointNames)
            {
                Transform topAttach = gameObject.transform.Find("New/pivot/" + pointName);
                Transform bottomAttach = m_pulleyObject.transform.Find(pointName);
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
