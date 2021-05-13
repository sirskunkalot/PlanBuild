using UnityEngine;

namespace Elevator
{
    public class MoveableBaseElevatorSync: MonoBehaviour
    {
		public MoveableBaseRoot m_baseRoot;

		public Rigidbody m_rigidbody;

		public ZNetView m_nview;

		public GameObject m_baseRootObject;

		public void OnDestroy()
		{
			if ((bool)m_baseRoot)
			{
				m_baseRoot.CleanUp();
				Object.Destroy(m_baseRoot.gameObject);
			}
		}

	}
}