using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elevator
{
	public class MoveableBaseRoot : MonoBehaviour
	{
		public static readonly KeyValuePair<int, int> MBParentHash = ZDO.GetHashZDOID("MBParent");

		public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

		public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

		public static Dictionary<ZDOID, List<Piece>> m_pendingPieces = new Dictionary<ZDOID, List<Piece>>();

		public MoveableBaseElevatorSync m_moveableBaseSync;
		  
		public ZNetView m_nview;
		 
		public Elevator m_elevator;

		public List<Piece> m_pieces = new List<Piece>();

		// public Rigidbody m_syncRigidbody;

		//public List<RudderComponent> m_rudderPieces = new List<RudderComponent>();

		public List<Piece> m_portals = new List<Piece>();

		public Vector2i m_sector;

		//public Bounds m_bounds;

		//public BoxCollider m_blockingcollider;
		  
		//public BoxCollider m_onboardcollider;

		public ZDOID m_id;

		public float m_lastPortalUpdate;

		public bool m_statsOverride;

		public void Awake()
		{
			m_elevator = gameObject.GetComponent<Elevator>();

			//m_blockingcollider = gameObject.transform.Find("collider").GetComponent<BoxCollider>();
			//m_onboardcollider = gameObject.transform.Find("OnBoardTrigger").GetComponent<BoxCollider>();
			//m_bounds = m_onboardcollider.bounds;
			m_nview = GetComponent<ZNetView>();
		}

		public void CleanUp()
		{
			if (!ZNetScene.instance || !ZNetScene.instance.m_netSceneRoot || !(m_id != ZDOID.None))
			{
				return;
			}
			for (int i = 0; i < m_pieces.Count; i++)
			{
				Piece piece = m_pieces[i];
				if ((bool)piece)
				{ 
					AddInactivePiece(m_id, piece);
				}
			}
			List<Player> allPlayers = Player.GetAllPlayers();
			for (int j = 0; j < allPlayers.Count; j++)
			{
				if ((bool)allPlayers[j] && allPlayers[j].transform.parent == transform)
				{
					allPlayers[j].transform.SetParent(ZNetScene.instance.m_netSceneRoot.transform);
				}
			}
		}
		  
		public void LateUpdate()
		{ 
			Vector2i zone = ZoneSystem.instance.GetZone(transform.position);
			if (zone != m_sector)
			{
				m_sector = zone;
				UpdateAllPieces();
			}
			else
			{
				UpdatePortals();
			}
		}

		public void UpdatePortals()
		{
			if (!(Time.time - m_lastPortalUpdate > 0.5f))
			{
				return;
			}
			m_lastPortalUpdate = Time.time;
			for (int i = 0; i < m_portals.Count; i++)
			{
				Piece piece = m_portals[i];
				if (!piece || !piece.m_nview || piece.m_nview.m_zdo == null)
				{
					m_pieces.RemoveAt(i);
					i--;
					continue;
				}
				Vector3 position = piece.m_nview.m_zdo.GetPosition();
				if ((piece.transform.position - position).sqrMagnitude > 1f)
				{
					piece.m_nview.m_zdo.SetPosition(piece.transform.position);
				}
			}
		}

		public void UpdateAllPieces()
		{
			for (int i = 0; i < m_pieces.Count; i++)
			{
				Piece piece = m_pieces[i];
				if (!piece)
				{
					m_pieces.RemoveAt(i);
					i--;
					continue;
				}
				ZNetView component = piece.GetComponent<ZNetView>();
				if ((bool)component)
				{
					component.m_zdo.SetPosition(piece.transform.position);
				}
			}
		}

		public static void AddInactivePiece(ZDOID id, Piece piece)
		{
#if DEBUG
            Jotunn.Logger.LogInfo("Adding inactive piece: " + id + " " + piece);
#endif
			if (!m_pendingPieces.TryGetValue(id, out var value))
			{
				value = new List<Piece>();
				m_pendingPieces.Add(id, value);
			}
			value.Add(piece);
			WearNTear component = piece.GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}

		public void RemovePiece(Piece piece)
		{
			m_pieces.Remove(piece);
			//MastComponent component = piece.GetComponent<MastComponent>();
			//if ((bool)component)
			//{
			//	m_mastPieces.Remove(component);
			//}
			//RudderComponent component2 = piece.GetComponent<RudderComponent>();
			//if ((bool)component2)
			//{
			//	m_rudderPieces.Remove(component2);
			//}
			TeleportWorld component3 = piece.GetComponent<TeleportWorld>();
			if ((bool)component3)
			{
				m_portals.Remove(piece);
			}
			UpdateStats();
		}

		public void UpdateStats()
		{
			 
		}

		public bool ActivatePendingPieces()
		{
			if (!m_nview || m_nview.m_zdo == null)
			{
				return false;
			}
#if DEBUG
			Jotunn.Logger.LogInfo("Activate pending pieces for " + m_nview.m_zdo.m_uid);
#endif
			ZDOID uid = m_nview.m_zdo.m_uid;
			if (!m_pendingPieces.TryGetValue(uid, out var value))
			{
				return true;
			}
			for (int i = 0; i < value.Count; i++)
			{
				Piece piece = value[i];
				if ((bool)piece)
				{
					ActivatePiece(piece);
				}
			}
			value.Clear();
			m_pendingPieces.Remove(uid);
			return true;
		}

		public static void InitPiece(Piece piece)
		{
			Rigidbody componentInChildren = piece.GetComponentInChildren<Rigidbody>();
			if ((bool)componentInChildren && !componentInChildren.isKinematic)
			{
				return;
			}
			ZDOID zDOID = piece.m_nview.m_zdo.GetZDOID(MBParentHash);
			if (zDOID == ZDOID.None)
			{ 
				return;
			}
#if DEBUG
			Jotunn.Logger.LogInfo("Piece has Parent: " + zDOID);
#endif
			GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
			if ((bool)gameObject)
			{
				MoveableBaseElevatorSync component = gameObject.GetComponent<MoveableBaseElevatorSync>();
				if ((bool)component && (bool)component.m_baseRoot)
				{
					component.m_baseRoot.ActivatePiece(piece);
				}
			}
			else
			{
				AddInactivePiece(zDOID, piece);
			}
		}

		public void ActivatePiece(Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogInfo("Activating piece " + piece.m_name + " @ " + piece.transform.position + ": Parent: " + m_nview.m_zdo.m_uid);
#endif
			ZNetView component = piece.GetComponent<ZNetView>();
			if ((bool)component)
			{
				piece.transform.SetParent(transform);
				piece.transform.localPosition = component.m_zdo.GetVec3(MBPositionHash, piece.transform.localPosition);
				piece.transform.localRotation = component.m_zdo.GetQuaternion(MBRotationHash, piece.transform.localRotation);
				WearNTear component2 = piece.GetComponent<WearNTear>();
				if ((bool)component2)
				{
					component2.enabled = true;
				}
				AddPiece(piece);
			}
		}

		public void AddNewPiece(Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogInfo("Adding piece " + piece.m_name + " @ " + piece.transform.position + ": Parent: " + m_nview.m_zdo.m_uid);
#endif
			piece.transform.SetParent(transform);
			ZNetView component = piece.GetComponent<ZNetView>();
			
			component.m_zdo.Set(MBParentHash, m_nview.m_zdo.m_uid);
			component.m_zdo.Set(MBPositionHash, piece.transform.localPosition);
			component.m_zdo.Set(MBRotationHash, piece.transform.localRotation);
			AddPiece(piece);
		}

		public void AddPiece(Piece piece)
		{
			m_pieces.Add(piece); 
			//EncapsulateBounds(piece);
		//	MastComponent component = piece.GetComponent<MastComponent>();
		//	if ((bool)component)
		//	{
		//		m_mastPieces.Add(component);
		//	}
		//	RudderComponent component2 = piece.GetComponent<RudderComponent>();
		//	if ((bool)component2)
		//	{
		//		if (!component2.m_controls)
		//		{
		//			component2.m_controls = piece.GetComponentInChildren<ShipControlls>();
		//		}
		//		if (!component2.m_wheel)
		//		{
		//			component2.m_wheel = piece.transform.Find("controls/wheel");
		//		}
		//		component2.m_controls.m_nview = m_nview;
		//		component2.m_controls.m_ship = m_moveableBaseSync.GetComponent<Ship>();
		//		m_rudderPieces.Add(component2);
		//	}
			TeleportWorld component3 = piece.GetComponent<TeleportWorld>();
			if ((bool)component3)
			{
				m_portals.Add(piece);
			}
			MeshRenderer[] componentsInChildren = piece.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
			MeshRenderer[] array = componentsInChildren;
			foreach (MeshRenderer meshRenderer in array)
			{
				if ((bool)meshRenderer.sharedMaterial)
				{
					Material[] sharedMaterials = meshRenderer.sharedMaterials;
					for (int j = 0; j < sharedMaterials.Length; j++)
					{
						Material material = new Material(sharedMaterials[j]);
						material.SetFloat("_RippleDistance", 0f);
						material.SetFloat("_ValueNoise", 0f);
						sharedMaterials[j] = material;
					}
					meshRenderer.sharedMaterials = sharedMaterials;
				}
			}
			Rigidbody[] componentsInChildren2 = piece.GetComponentsInChildren<Rigidbody>();
			for (int k = 0; k < componentsInChildren2.Length; k++)
			{
				if (componentsInChildren2[k].isKinematic)
				{
                    Destroy(componentsInChildren2[k]);
				}
			}
			UpdateStats();
		}

		public void OnTriggerEnter(Collider collider)
		{
			m_elevator?.OnTriggerEnter(collider);
		}

		public void OnTriggerExit(Collider collider)
		{
			m_elevator?.OnTriggerExit(collider);
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

		public int GetPieceCount()
		{
			return m_pieces.Count;
		}
	}
}
