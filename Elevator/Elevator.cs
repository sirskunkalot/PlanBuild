using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Elevator
{
    public class Elevator: Ship
    {

        public static readonly KeyValuePair<int, int> ElevatorBaseParentHash = ZDO.GetHashZDOID("ElevatorBaseParent"); 
        public static readonly int ElevatorBasePositionHash = "ElevatorBasePosition".GetStableHashCode(); 
        public static readonly int ElevatorBaseRotationHash = "ElevatorBaseRotation".GetStableHashCode();
        public ElevatorControlls m_elevatorControlls;

        public new void Awake()
        {
            m_sailObject = new GameObject("fake_sail");
            m_controlGuiPos = transform.Find("ControlGui");
            m_nview = GetComponent<ZNetView>();
            m_body = GetComponent<Rigidbody>();
            WearNTear component = GetComponent<WearNTear>();
            if ((bool)component)
            {
                component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
            }
            if (m_nview.GetZDO() == null)
            {
                base.enabled = false;
            }
            m_elevatorControlls = GetComponentInChildren<ElevatorControlls>();
            m_shipControlls = m_elevatorControlls;
        }

        public new void ApplyMovementControlls(Vector3 direction)
        {
            base.ApplyMovementControlls(direction);
        }

        public new void UpdateSail(float dt)
        {
            //Nothing to do
        }

        public new void UpdateSailSize(float dt)
        {
            //Nothing to do
        }

        public new void OnTriggerEnter(Collider collider)
        {
            base.OnTriggerEnter(collider);
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
            Vector3 positionChange = Vector3.zero;
            switch(m_speed)
            {
                case Speed.Stop:
                    return;
                case Speed.Half:
                case Speed.Full:
                    m_speed = Speed.Slow;
                    goto case Speed.Slow;
                case Speed.Slow:
                    positionChange.y += m_rudderSpeed * Time.fixedDeltaTime;
                    break;
                case Speed.Back:
                    positionChange.y -= m_rudderSpeed * Time.fixedDeltaTime;
                    break; 
            }
            m_body.MovePosition(transform.position + positionChange);
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
