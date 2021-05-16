using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Elevator
{
    public class Pulley : MonoBehaviour
    {

        public static readonly KeyValuePair<int, int> ElevatorSupportHash = ZDO.GetHashZDOID("ElevatorSupport"); 
        public static readonly int ElevatorBasePositionHash = "ElevatorBasePosition".GetStableHashCode(); 
        public static readonly int ElevatorBaseRotationHash = "ElevatorBaseRotation".GetStableHashCode();
        public PulleyControlls m_elevatorControlls;
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
        private ZNetView m_nview;

        private int m_supportRayMask;

        public void Awake()
        {
            m_supportRayMask = LayerMask.GetMask("piece");
            m_nview = GetComponent<ZNetView>(); 
            m_elevatorControlls = transform.Find("wheel_collider").gameObject.AddComponent<PulleyControlls>();

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
                Jotunn.Logger.LogWarning(GetInstanceID() + ": No support for elevator @ " + this.transform.position + " " + gameObject.GetInstanceID());
                InvokeRepeating("UpdateLookForSupport", 1f, 1f);
                return;
            }
          
            m_support.SetElevatorBase(this);
            UpdateRotation();
        }

        internal void SetSupport(PulleySupport elevatorSupport)
        {
            Jotunn.Logger.LogWarning(GetInstanceID() + ": Setting support for elevator @ " + this.transform.position + " " + gameObject.GetInstanceID() + ": " + elevatorSupport.GetInstanceID());
            m_nview.GetZDO().Set(ElevatorSupportHash, elevatorSupport.m_nview.m_zdo.m_uid);
            m_support = elevatorSupport;
        }

        public void UpdateLookForSupport()
        {
            if (!m_support)
            {
                m_support = FindPulleySupport();
                if (m_support)
                {
                    m_support.SetElevatorBase(this);
                    UpdateRotation();
                    CancelInvoke("UpdateLookForSupport");
                }
            }
        }
 

        internal ZDOID GetElevatorID()
        {
            return m_nview.m_zdo.m_uid;
        }

        internal void UpdateRotation()
        {
            float ropeLength = GetRopeLength();
            
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
            ZDOID elevatorSupportID = m_nview.GetZDO().GetZDOID(ElevatorSupportHash);
            if(elevatorSupportID == ZDOID.None)
            {
                if(Physics.Raycast(transform.position + 2*transform.up , transform.up, out var hitInfo,  2000f, m_supportRayMask)) {
                    return hitInfo.collider.GetComponentInParent<PulleySupport>();
                }
                Jotunn.Logger.LogWarning("No Elevator Support found!: " + this.transform.position);
                return null;
            }
            GameObject elevatorSupportObject = ZNetScene.instance.FindInstance(elevatorSupportID);
            if(!elevatorSupportObject)
            {
                Jotunn.Logger.LogWarning("Stored Elevator Support not found!: " + elevatorSupportID);
                m_nview.GetZDO().Set(ElevatorSupportHash, ZDOID.None);
                return null;
            }
            return elevatorSupportObject.GetComponent<PulleySupport>();
        }

        internal bool IsConnected()
        {
            return m_support != null;
        }

        internal float GetRopeLength()
        { 
            return m_support.transform.position.y - transform.position.y;
        }
    }
}
