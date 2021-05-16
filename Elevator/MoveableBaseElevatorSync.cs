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
			m_baseRootObject = new GameObject
			{
				name = "MoveableBase",
				layer = 0
			};
			m_baseRootObject.transform.SetParent(ZNetScene.instance.m_netSceneRoot.transform);
			m_baseRootObject.transform.position = base.transform.position;
			m_baseRootObject.transform.rotation = base.transform.rotation;
			transform.SetParent(m_baseRootObject.transform);
			m_baseRoot = m_baseRootObject.AddComponent<MoveableBaseRoot>();
			activatedPendingPieces = m_baseRoot.ActivatePendingPieces(); 
			m_baseRoot.m_moveableBaseSync = this;
			m_baseRoot.m_nview = m_nview;
			m_rigidbody = m_baseRootObject.AddComponent<Rigidbody>();
            m_rigidbody.mass = 1000f;
			m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation & RigidbodyConstraints.FreezePositionX & RigidbodyConstraints.FreezePositionZ;
			m_rigidbody.useGravity = false;
			m_rigidbody.isKinematic = true;
			Elevator elevator = gameObject.AddComponent<Elevator>();
			m_baseRoot.m_elevator = elevator;
			m_baseRoot.m_id = m_nview.m_zdo.m_uid;
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