using UnityEngine;

namespace Elevator
{
    public class MoveableBaseElevatorSync: MonoBehaviour
    {
		public MoveableBaseRoot m_baseRoot;

		public Rigidbody m_rigidbody;

		public ZNetView m_nview;

		public GameObject m_baseRootObject;
		private bool activatedPendingPieces = false;
		public void Awake()
        {
			m_nview = GetComponent<ZNetView>();
			Elevator elevator = GetComponent<Elevator>();
			m_baseRootObject = elevator.gameObject;
			m_baseRoot = m_baseRootObject.AddComponent<MoveableBaseRoot>();
			activatedPendingPieces = m_baseRoot.ActivatePendingPieces(); 
			m_baseRoot.m_moveableBaseSync = this;
			m_baseRoot.m_nview = m_nview;
			m_baseRoot.m_elevator = elevator;
			m_baseRoot.m_id = m_nview.m_zdo.m_uid;
			m_rigidbody = GetComponent<Rigidbody>();
			m_baseRoot.m_syncRigidbody = m_rigidbody;
			m_rigidbody.mass = 1000f;  

		}

		public void Update()
        {
			if(!activatedPendingPieces)
            {
				activatedPendingPieces = m_baseRoot.ActivatePendingPieces();
            }
        }

		public void OnDestroy()
		{
			if ((bool)m_baseRoot)
			{
				m_baseRoot.CleanUp();
                Destroy(m_baseRoot.gameObject);
			}
		}

	}
}