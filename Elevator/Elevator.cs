using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Elevator
{
    public class Elevator : Ship
    {

        public static readonly KeyValuePair<int, int> ElevatorSupportHash = ZDO.GetHashZDOID("ElevatorSupport"); 
        public static readonly int ElevatorBasePositionHash = "ElevatorBasePosition".GetStableHashCode(); 
        public static readonly int ElevatorBaseRotationHash = "ElevatorBaseRotation".GetStableHashCode();
        public ElevatorControlls m_elevatorControlls;
        internal ElevatorSupport m_support;
        internal Transform pivotLeft;
        internal Transform pivotRight;
        internal Transform planet1;
        internal Transform planet2;
        internal Transform planet3;
        internal Transform planet4;
        internal Transform crank;
        internal float rotation;
        private int m_supportRayMask;
        private float highestFloor; 

        public new void Awake()
        {
            m_sailObject = new GameObject("fake_sail");
            m_controlGuiPos = transform.Find("ControlGui");
            m_nview = GetComponent<ZNetView>();
            m_body = GetComponentInParent<Rigidbody>();
            m_supportRayMask = LayerMask.GetMask("piece");
            WearNTear component = GetComponent<WearNTear>();
            if ((bool)component)
            {
                component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
            }
            if (m_nview.GetZDO() == null)
            {
                enabled = false;
            }
            m_elevatorControlls = GetComponentInChildren<ElevatorControlls>();
            m_shipControlls = m_elevatorControlls;

            pivotLeft = transform.Find("New/pivot_left");
            pivotRight = transform.Find("New/pivot_right");
            planet1 = pivotRight.Find("planet_1");
            planet2 = pivotRight.Find("planet_2");
            planet3 = pivotRight.Find("planet_3");
            planet4 = pivotRight.Find("planet_4");
            crank = transform.Find("New/crank");
            
            InvokeRepeating("UpdateHeightMap", 10f, 10f);

            m_support = FindElevatorSupport();
            if (!m_support)
            {
                Jotunn.Logger.LogWarning(GetInstanceID() + ": No support for elevator @ " + this.transform.position + " " + gameObject.GetInstanceID());
                InvokeRepeating("UpdateLookForSupport", 1f, 1f);
                return;
            }
            Heightmap.GetHeight(transform.position, out highestFloor); 
            m_support.SetElevatorBase(this);
            UpdateRotation();
        }

        internal ZDOID GetElevatorID()
        {
            return m_nview.m_zdo.m_uid;
        }

        public new void ApplyMovementControlls(Vector3 direction)
        {
            base.ApplyMovementControlls(direction);
        }

        public new void UpdateSail(float dt)
        {
            //Nothing to do
        }

        public new Ship.Speed GetSpeed()
        {
            //Only used by MusicMan
            return Speed.Stop;
        }

        internal void SetSupport(ElevatorSupport elevatorSupport)
        {
            Jotunn.Logger.LogWarning(GetInstanceID() + ": Setting support for elevator @ " + this.transform.position + " " + gameObject.GetInstanceID() + ": " + elevatorSupport.GetInstanceID()); 
            m_nview.GetZDO().Set(ElevatorSupportHash, elevatorSupport.m_nview.m_zdo.m_uid);
            m_support = elevatorSupport;
        }

        public new void UpdateSailSize(float dt)
        {
            //Nothing to do
        }

        public new void OnTriggerEnter(Collider collider)
        {
            base.OnTriggerEnter(collider);
        }


        public new void OnTriggerExit(Collider collider)
        {
            base.OnTriggerExit(collider);
        }

        public void UpdateLookForSupport()
        {
            if(!m_support)
            {
                m_support = FindElevatorSupport();
                if(m_support)
                {
                    m_support.SetElevatorBase(this);
                    UpdateRotation();
                    CancelInvoke("UpdateLookForSupport");
                }
            }
        }

        public new void FixedUpdate()
        {
            bool haveControllingPlayer = HaveControllingPlayer();
            UpdateControlls(Time.fixedDeltaTime);
            if (m_nview && !m_nview.IsOwner())
            {
                return;
            }
            if (m_players.Count == 0)
            {
                m_speed = Speed.Stop;
            }
            if (!haveControllingPlayer && (m_speed == Speed.Slow || m_speed == Speed.Back))
            {
                m_speed = Speed.Stop;
            }
            if (m_speed == Speed.Stop)
            {
                return;
            }
            if (!m_support)
            {
                m_support = FindElevatorSupport();
                if (!m_support)
                {
                    Jotunn.Logger.LogWarning(GetInstanceID() + ": No support for elevator @ " + this.transform.position + " " + gameObject.GetInstanceID());
                    return;
                }
                m_support.SetElevatorBase(this);
            }

            float ropeLength = m_support.transform.position.y - transform.position.y;
            Vector3 positionChange = Vector3.zero;
            switch (m_speed)
            {
                case Speed.Stop:
                    return;
                case Speed.Half:
                case Speed.Full:
                    m_speed = Speed.Slow;
                    goto case Speed.Slow;
                case Speed.Slow:
                    float ropeLeftUp = ropeLength - 3f;
                    positionChange.y += Math.Min(m_rudderSpeed * Time.fixedDeltaTime, ropeLeftUp);
                    break;
                case Speed.Back:
                    float ropeLeftDown = transform.position.y - highestFloor;
                    positionChange.y -= Math.Min(m_rudderSpeed * Time.fixedDeltaTime, ropeLeftDown);
                    break;
            }
            if (!m_body)
            {
                m_body = GetComponentInParent<Rigidbody>();
                if (!m_body)
                {
                    Jotunn.Logger.LogWarning("No rigid body!");
                    return;
                }
            }
            m_body.MovePosition(transform.position + positionChange);
            UpdateRotation(ropeLength);
        }

        public void UpdateHeightMap()
        {
            MoveableBaseRoot moveableBaseRoot = GetComponentInParent<MoveableBaseRoot>();
            Heightmap.GetHeight(transform.position, out highestFloor);
            foreach (Piece piece in moveableBaseRoot.m_pieces)
            {
                if (Heightmap.GetHeight(piece.transform.position, out float floorHeight))
                {
                    highestFloor = Math.Max(floorHeight, highestFloor);
                }
            }
#if DEBUG
            Jotunn.Logger.LogInfo("Updated max floor height to: " + highestFloor);
#endif
        }

        private void UpdateRotation(float ropeLength = -1f)
        {
            if(ropeLength == -1f)
            {
                ropeLength = m_support.transform.position.y - transform.position.y;
            }

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

        private ElevatorSupport FindElevatorSupport()
        {
            ZDOID elevatorSupportID = m_nview.GetZDO().GetZDOID(ElevatorSupportHash);
            if(elevatorSupportID == ZDOID.None)
            {
                if(Physics.Raycast(transform.position + 2*transform.up , transform.up, out var hitInfo,  2000f, m_supportRayMask)) {
                    return hitInfo.collider.GetComponentInParent<ElevatorSupport>();
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
            return elevatorSupportObject.GetComponent<ElevatorSupport>();
        }
         
        public new void UpdateControlls(float dt)
        {
            if (m_nview.IsOwner())
            {
                m_nview.GetZDO().Set("forward", (int)m_speed); 
                return;
            }
            m_speed = (Speed)m_nview.GetZDO().GetInt("forward");
        }
         
    }
}
