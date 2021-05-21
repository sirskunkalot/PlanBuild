using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Pulleys
{
    public class Pulley : MonoBehaviour
    {
        private const float PulleyUpdateTime = 2f;
        public static readonly KeyValuePair<int, int> PulleySupportHash = ZDO.GetHashZDOID("marcopogo.PulleySupport");  
        public PulleyControlls m_pulleyControlls;
        internal PulleySupport m_support;
        internal Transform pivotLeft;
        internal Transform pivotRight;
        internal Transform planet1;
        internal Transform planet2;
        internal Transform planet3;
        internal Transform planet4;
        internal Transform crank;
        internal float rotation;
        public Transform m_controlGuiPos;
        internal ZNetView m_nview;
        internal MoveableBaseSync m_baseSync;

        private int m_supportRayMask;

        public void Awake()
        {
            m_supportRayMask = LayerMask.GetMask("piece");
            m_nview = GetComponent<ZNetView>();
            
            if(!m_nview || !m_nview.IsValid())
            {
                return;
            } 

            WearNTear wearNTear = GetComponent<WearNTear>();
            wearNTear.m_onDestroyed += OnDestroyed;
            m_pulleyControlls = transform.Find("wheel_collider").gameObject.AddComponent<PulleyControlls>();

            pivotLeft = transform.Find("New/pivot_left");
            pivotRight = transform.Find("New/pivot_right");
            planet1 = pivotRight.Find("planet_1");
            planet2 = pivotRight.Find("planet_2");
            planet3 = pivotRight.Find("planet_3");
            planet4 = pivotRight.Find("planet_4");
            crank = transform.Find("New/crank");

            m_controlGuiPos = transform.Find("ControlGui");
            
            m_support = FindPulleySupport();
            if (!m_support)
            { 
                InvokeRepeating("UpdateLookForSupport", PulleyUpdateTime, PulleyUpdateTime);
                return;
            }
          
            m_support.SetPulleyBase(this);
            UpdateRotation();
        }

        private void OnDestroyed()
        {
            m_support?.PulleyBaseDestroyed(this);
            m_baseSync.RemovePulley(this);
        }

        internal void SupportDestroyed(PulleySupport pulleySupport)
        {
            if(m_support != pulleySupport)
            {
                Jotunn.Logger.LogWarning("Invalid callback from " + pulleySupport.GetZDOID() + " to " + this.GetZDOID() + ", expected " + m_support.GetZDOID());
                return;
            }
            m_support = null;
            m_nview.GetZDO().Set(PulleySupportHash, ZDOID.None);
            InvokeRepeating("UpdateLookForSupport", PulleyUpdateTime, PulleyUpdateTime);
        }

        internal void SetSupport(PulleySupport pulleySupport)
        {
#if DEBUG
            Jotunn.Logger.LogWarning(GetInstanceID() + ": Setting support for pulley @ " + this.transform.position + " " + gameObject.GetInstanceID() + ": " + pulleySupport.GetInstanceID());
#endif
            m_nview.GetZDO().Set(PulleySupportHash, pulleySupport.m_nview.m_zdo.m_uid);
            m_support = pulleySupport;
            CancelInvoke("UpdateLookForSupport");
        }

        public void UpdateLookForSupport()
        {
            if (!m_support)
            {
               PulleySupport pulleySupport = FindPulleySupport();
                if (pulleySupport)
                {
                    SetSupport(pulleySupport);
                }
            }
        }
         
        internal ZDOID GetZDOID()
        {
            return m_nview.m_zdo.m_uid ;
        }

        internal void UpdateRotation(float defaultRopeLength = 0f)
        {
            float ropeLength = GetRopeLength(defaultRopeLength);
            
            const float diameter = 1.270749f * Mathf.PI;

            rotation = ropeLength % diameter / diameter * 360f;

            pivotLeft.localRotation = Quaternion.Euler(rotation, 0f, 0f);
            pivotRight.localRotation = Quaternion.Euler(-rotation, 0f, 0f);
            planet1.localRotation = Quaternion.Euler(-rotation, 0f, 0f);
            planet2.localRotation = Quaternion.Euler(-rotation, 0f, 0f);
            planet3.localRotation = Quaternion.Euler(-rotation, 0f, 0f);
            planet4.localRotation = Quaternion.Euler(-rotation, 0f, 0f);
            crank.localRotation = Quaternion.Euler(rotation, 0f, 0f);
        }

        private PulleySupport FindPulleySupport()
        {
            ZDOID pulleySupportID = m_nview.GetZDO().GetZDOID(PulleySupportHash);
            if(pulleySupportID == ZDOID.None)
            {
                if(Physics.Raycast(transform.position + 2*transform.up , transform.up, out var hitInfo,  2000f, m_supportRayMask)) {
                    PulleySupport pulleySupport = hitInfo.collider.GetComponentInParent<PulleySupport>();
                    if(pulleySupport && pulleySupport.IsConnected())
                    {
                        return null;
                    }
                    return pulleySupport;
                } 
                return null;
            }
            GameObject pulleySupportObject = ZNetScene.instance.FindInstance(pulleySupportID);
            if(!pulleySupportObject)
            {
                Jotunn.Logger.LogWarning("Stored Pulley Support not found!: " + pulleySupportID);
                m_nview.GetZDO().Set(PulleySupportHash, ZDOID.None);
                return null;
            }
            return pulleySupportObject.GetComponent<PulleySupport>();
        }

        internal bool IsConnected()
        {
            return m_support != null;
        }

        internal float GetRopeLength(float defaultRopeLength = 0f)
        { 
            if(!m_support)
            {
                return defaultRopeLength;
            }

            return m_support.transform.position.y - transform.position.y;
        }

        internal bool CanBeRemoved()
        {
            if(!m_baseSync)
            {
                //In case of error, allow removal
#if DEBUG
                Jotunn.Logger.LogWarning(GetZDOID() + " Removed pulley was invalid, no m_baseSync!?");
#endif 
                return true;
            }
            if(!IsConnected())
            {
                //Unconnected pulleys don't support anything
                return true;
            }

            return m_baseSync.CanBeRemoved(this);
        }
    }
}
