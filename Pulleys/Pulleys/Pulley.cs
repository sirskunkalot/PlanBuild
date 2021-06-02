using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pulleys
{
    public class Pulley : MonoBehaviour
    {
        


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
        internal MoveableBaseRoot m_baseRoot;

        public void Awake()
        {
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
        }

        private void OnDestroyed()
        {
            m_support?.PulleyBaseDestroyed(this);
        }

        internal void SupportDestroyed(PulleySupport pulleySupport)
        {
            if(m_support != pulleySupport)
            {
                Jotunn.Logger.LogWarning("Invalid callback from " + pulleySupport.GetZDOID() + " to " + this.GetZDOID() + ", expected " + m_support.GetZDOID());
                return;
            }
            m_support = null; 
        }

        internal void SetSupport(PulleySupport pulleySupport)
        {
#if DEBUG
            Jotunn.Logger.LogWarning(GetInstanceID() + ": Setting support for pulley @ " + this.transform.position + " " + gameObject.GetInstanceID() + ": " + pulleySupport.GetInstanceID());
#endif
            m_support = pulleySupport;
            if(!m_baseRoot)
            {
                GameObject baseSyncObject = Object.Instantiate(PrefabManager.Instance.GetPrefab(PulleyManager.MoveableBaseSyncName), transform.position, transform.rotation);
                MoveableBaseSync moveableBaseSync = baseSyncObject.GetComponent<MoveableBaseSync>();
                moveableBaseSync.AddNewPiece(GetComponent<Piece>());

            }
        }

        internal void SetMoveableBase(MoveableBaseRoot m_baseRoot)
        {  
            m_pulleyControlls.SetMoveableBase(m_baseRoot);
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
            if(!m_baseRoot)
            {
                return true;
            }
            if(!IsConnected())
            {
                //Unconnected pulleys don't support anything
                return true;
            }

            return m_baseRoot.CanBeRemoved(this);
        }
    }
}
