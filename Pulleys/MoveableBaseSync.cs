using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pulleys
{
    public class MoveableBaseSync: MonoBehaviour
    {
		public const string MBSyncID = "marcopogo.MBSyncID"; 

		public static readonly KeyValuePair<int, int> MBParentHash = ZDO.GetHashZDOID("marcopogo.MBParent");
		public static readonly int MBPositionHash = "marcopogo.MBPosition".GetStableHashCode();
        public static readonly int MBRotationHash = "marcopogo.MBRotation".GetStableHashCode();

		public static readonly List<MoveableBaseSync> allMoveableBaseSyncs = new List<MoveableBaseSync>();
        public static readonly Dictionary<ZDOID, HashSet<Piece>> m_pendingPieces = new Dictionary<ZDOID, HashSet<Piece>>();

		public readonly List<Piece> m_pieces = new List<Piece>(); 
		public readonly List<Piece> m_portals = new List<Piece>();


		public MoveableBaseRoot m_baseRoot;
		public float m_lastPortalUpdate;

		public Vector2i m_sector;
		public Rigidbody m_rigidbody;

		public ZNetView m_nview;
		public bool m_follower;

        internal static List<MoveableBaseSync> GetAllMoveableBaseSyncs()
        {
			return new List<MoveableBaseSync>(allMoveableBaseSyncs);
        }

        internal void UpdateWear()
        {
         
        }

        public GameObject m_baseControllerObject;
        public GameObject m_baseRootObject;
		private bool activatedPendingPieces = false; 
        internal Pulley m_pulley;

        public void Awake()
        {
			m_nview = GetComponent<ZNetView>();
            m_pulley = GetComponent<Pulley>(); 
#if DEBUG
            Jotunn.Logger.LogInfo(m_nview.m_zdo.m_uid + " Creating MoveableBaseRoot");
#endif 
            m_baseControllerObject = Object.Instantiate(PrefabManager.Instance.GetPrefab(PulleyManager.MoveableBaseRootName), transform.position, transform.rotation, ZNetScene.instance.transform); 
            m_baseRoot = m_baseControllerObject.GetComponent<MoveableBaseRoot>();
			m_baseRoot.SetBaseSync(this);

			allMoveableBaseSyncs.Add(this);
        }

		public void OnDestroy()
        {
			allMoveableBaseSyncs.Remove(this);
        }

        public void Update()
        {
			if (!m_nview || !m_nview.IsValid())
			{
				return;
			} 
			if(m_follower)
            {
				return;
            }
			if (m_baseRoot && !activatedPendingPieces)
            {
				activatedPendingPieces = ActivatePendingPieces();
            }
        }
         
        internal ZDOID GetZDOID()
        {
			return m_nview.m_zdo.m_uid;
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
			foreach (Piece piece in value)
			{
				if (piece)
				{
					ActivatePiece(piece);
				}
			}
			value.Clear();
			m_pendingPieces.Remove(uid);
			return true;
		}

		public int GetPieceCount()
		{
			return m_pieces.Count;
		}

		public static void InitPiece(Piece piece)
		{
			Rigidbody componentInChildren = piece.GetComponentInChildren<Rigidbody>();
			if (componentInChildren && !componentInChildren.isKinematic)
			{
				Jotunn.Logger.LogInfo("Ignoring rigidbody: " + piece);
				return;
			}
			ZDOID zDOID = piece.m_nview.m_zdo.GetZDOID(MBParentHash);
			if (zDOID == ZDOID.None)
			{
				return;
			}
#if DEBUG
			Jotunn.Logger.LogInfo("Piece (" + piece.m_nview.m_zdo.m_uid + ") has Parent: " + zDOID);
#endif
			GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
			if ((bool)gameObject)
			{
				MoveableBaseSync component = gameObject.GetComponent<MoveableBaseSync>();
				if (component && component.m_baseRoot)
				{
					component.ActivatePiece(piece);
				}
#if DEBUG
				else
				{
					Jotunn.Logger.LogWarning("ZDOID Saved MoveableBaseSync has no MoveableBaseSync: " + component + " " + component.transform.position);
				}
#endif
			}
			else
			{
				AddInactivePiece(zDOID, piece);
			}
		}

		public static void AddInactivePiece(ZDOID id, Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogInfo("Adding inactive piece: " + id + " " + piece + " (" + piece.m_nview?.m_zdo?.m_uid + ")");
#endif
			if (!m_pendingPieces.TryGetValue(id, out var value))
			{
				value = new HashSet<Piece>();
				m_pendingPieces.Add(id, value);
			}
			value.Add(piece);
			WearNTear component = piece.GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}

		public void ActivatePiece(Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogInfo(GetZDOID() + " Activating piece " + piece.m_name + " @ " + piece.transform.position + ": Parent: " + m_nview.m_zdo.m_uid);
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

		public void OnBaseRootDestroy(MoveableBaseSync destroyingSync)
		{
			if (ZNetScene.instance && ZNetScene.instance.m_netSceneRoot)
			{
				for (int i = 0; i < m_pieces.Count; i++)
				{
					Piece piece = m_pieces[i];
					if ((bool)piece)
					{
						AddInactivePiece(m_nview.m_zdo.m_uid, piece);
					}
				}
				List<Player> allPlayers = Player.GetAllPlayers();
				for (int j = 0; j < allPlayers.Count; j++)
				{
					if (allPlayers[j] && allPlayers[j].transform.parent == transform)
					{
						allPlayers[j].transform.SetParent(ZNetScene.instance.m_netSceneRoot.transform);
					}
				}
			}
		}

		public void AddNewPiece(Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogInfo(GetZDOID() + " Adding piece " + piece.m_name + " @ " + piece.transform.position + ": Parent: " + m_nview.m_zdo.m_uid);
#endif
			SetParentAndZDOs(piece);
			AddPiece(piece);
		}

		private void SetParentAndZDOs(Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogInfo("MoveableBaseRoot " + m_nview.m_zdo.m_uid + ": Claiming piece " + piece + " " + piece.m_nview.m_zdo.m_uid);
#endif
			piece.transform.SetParent(m_baseRoot.transform);
			ZNetView pieceNview = piece.GetComponent<ZNetView>();
			pieceNview.m_zdo.Set(MBParentHash, m_nview.m_zdo.m_uid);
			pieceNview.m_zdo.Set(MBPositionHash, piece.transform.localPosition);
			pieceNview.m_zdo.Set(MBRotationHash, piece.transform.localRotation);
		}

		internal void ClearMoveableBaseSyncZDO()
		{
			m_nview.m_zdo.Set(MBParentHash, ZDOID.None);
			m_nview.m_zdo.Set(MBPositionHash, Vector3.zero);
			m_nview.m_zdo.Set(MBRotationHash, Quaternion.identity);
		}

		private void OnDestroyed(Piece piece)
		{
#if DEBUG
			Jotunn.Logger.LogWarning(GetZDOID() + " Removing destroyed piece " + piece + " " + piece.m_nview.m_zdo.m_uid);
#endif
			m_pieces.Remove(piece);
			if (piece.TryGetComponent(out Pulley pulley))
			{
				m_baseRoot.RemovePulley(pulley);
			}
		}

		public void AddPiece(Piece piece)
		{
			m_pieces.Add(piece); 

			if(piece.TryGetComponent(out Pulley pulley))
            {
				m_baseRoot.AddPulley(pulley);
            }
			 
			if (piece.TryGetComponent(out WearNTear wearNTear))
			{
				wearNTear.m_onDestroyed += () => OnDestroyed(piece);
			}
			 
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
#if DEBUG
					Jotunn.Logger.LogWarning(GetZDOID() + " Destroying rigidbody: " + componentsInChildren2[k]);
#endif
					Destroy(componentsInChildren2[k]);
				}
			} 
		}

		public void RemovePiece(Piece piece)
		{
			m_pieces.Remove(piece);
			TeleportWorld component3 = piece.GetComponent<TeleportWorld>();
			if ((bool)component3)
			{
				m_portals.Remove(piece);
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


	}
}