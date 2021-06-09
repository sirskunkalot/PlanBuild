using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pulleys
{
	public class MoveableBaseRoot : Ship
	{
		public const string zdoPieceSet = "marcopogo.MBPieces";

		public readonly HashSet<Pulley> m_pulleys = new HashSet<Pulley>();
		 
        // public Rigidbody m_syncRigidbody;

        //public List<RudderComponent> m_rudderPieces = new List<RudderComponent>();


		//public Bounds m_bounds;

		//public BoxCollider m_blockingcollider;
		  
		//public BoxCollider m_onboardcollider;
		  
		public bool m_statsOverride;

		private float highestFloor;
        private int m_supportRayMask;
        private GameObject m_baseRootObject;
        private Rigidbody m_rigidbody;
		internal MoveableBaseSync m_baseSync;

        public new void Awake()
		{
			m_supportRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
			Heightmap.GetHeight(transform.position, out highestFloor);
			m_body = GetComponent<Rigidbody>(); 
			InvokeRepeating("UpdateHeightMap", 10f, 10f); 
		}

		public void UpdateHeightMap()
		{
			Heightmap.GetHeight(transform.position, out highestFloor);
			foreach (Piece piece in m_baseSync.m_pieces)
			{
				if (Physics.Raycast(piece.transform.position, piece.transform.up * -1f, out var hitInfo, 2000f, m_supportRayMask))
				{
					highestFloor = Math.Max(hitInfo.transform.position.y, highestFloor);
				}
				else
				{
					if (Heightmap.GetHeight(piece.transform.position, out float floorHeight))
					{
						highestFloor = Math.Max(floorHeight, highestFloor);
					}
				}
			}

#if DEBUG
			Jotunn.Logger.LogInfo("Updated max floor height to: " + highestFloor);
#endif
		}

        internal void SetBaseSync(MoveableBaseSync moveableBaseSync)
        {
			m_baseSync = moveableBaseSync;
			m_nview = moveableBaseSync.m_nview;
        }

        internal bool CanBeRemoved(Pulley pulleyToRemove)
		{  
			int supportingPulleyCount = m_pulleys.Count(pulley => pulley.IsConnected());
			int pieceCount = m_baseSync.GetPieceCount();
			if (pieceCount == 0)
			{
				//Last pulley can be removed whenever
				return true;
			}
			if (supportingPulleyCount > 1)
			{
				//Other pulleys can still support
				return true;
			}

			//Last support but pieces remain
			return false;
		}

		internal void AddPulley(Pulley pulley)
        {
#if DEBUG
			Jotunn.Logger.LogInfo(GetZDOID() + " AddPulley(" + pulley.m_nview.m_zdo.m_uid + ")");
#endif
			m_pulleys.Add(pulley); 
			pulley.SetMoveableBase(this);
			if (!m_shipControlls)
            {
				SetActiveControll(pulley.m_pulleyControlls);
            }
        }

        internal void RemovePulley(Pulley m_pulley)
        {
			m_pulleys.Remove(m_pulley);
			if(m_shipControlls == m_pulley.m_pulleyControlls)
            {
				if(m_pulleys.Count == 0)
                {
#if DEBUG
					Jotunn.Logger.LogWarning("Last pulley removed, destroying MoveableBaseRoot");
#endif
					Object.Destroy(gameObject);
					return;
                }
#if DEBUG
                Jotunn.Logger.LogWarning("Active pulley controlls removed, selecting random remaining as active");
#endif
				SetActiveControll(m_pulleys.First().m_pulleyControlls);
            }
        }

        internal void SetActiveControll(PulleyControlls pulleyControlls)
        {
#if DEBUG
            Jotunn.Logger.LogInfo(GetZDOID() + " Setting active control: " + pulleyControlls.m_nview.m_zdo.m_uid);
#endif
            m_shipControlls = pulleyControlls;
            m_controlGuiPos = pulleyControlls.m_pulley.m_controlGuiPos;
			pulleyControlls.SetMoveableBase(this);
        }

        private ZDOID GetZDOID()
        {
            return m_nview.m_zdo.m_uid;
        }

		public void UpdateStats()
		{
			 
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

		public new void UpdateControlls(float dt)
		{
			if(!m_nview || !m_nview.IsValid())
            {
				return;
            }
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set("forward", (int)m_speed);
				return;
			}
			m_speed = (Speed)m_nview.GetZDO().GetInt("forward");
		}

		public float GetShortestRope()
        {
			float shortest = float.MaxValue;
			foreach(Pulley pulley in m_pulleys)
            {
				if(pulley.IsConnected())
                {
					shortest = Math.Min(shortest, pulley.GetRopeLength());
                }
            }
			return shortest;
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

			float ropeLength = GetShortestRope(); 
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

        private void UpdateRotation(float ropeLength = -1f)
        {
			foreach (Pulley pulley in m_pulleys)
			{
				if (pulley.IsConnected() || m_shipControlls == pulley.m_pulleyControlls)
				{
					pulley.UpdateRotation(ropeLength);
				}
			}
		}
		 
        //public void EncapsulateBounds(Piece piece)
        //{
        //	List<Collider> allColliders = piece.GetAllColliders();
        //	Door componentInChildren = piece.GetComponentInChildren<Door>();
        //	if (!componentInChildren)
        //	{
        //		m_bounds.Encapsulate(piece.transform.localPosition);
        //	}
        //	for (int i = 0; i < allColliders.Count; i++)
        //	{
        //		Physics.IgnoreCollision(allColliders[i], m_blockingcollider, ignore: true); 
        //		Physics.IgnoreCollision(allColliders[i], m_onboardcollider, ignore: true);
        //	}
        //	//m_blockingcollider.size = new Vector3(m_bounds.size.x, 3f, m_bounds.size.z);
        //	//m_blockingcollider.center = new Vector3(m_bounds.center.x, -0.2f, m_bounds.center.z); 
        //	//m_onboardcollider.size = m_bounds.size;
        //	//m_onboardcollider.center = m_bounds.center;
        //}

	}
}
