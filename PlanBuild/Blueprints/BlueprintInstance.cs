using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintInstance : MonoBehaviour
    {
        private ZNetView NetView;
        public string Name;

        public static bool TryGetInstance(Piece piece, out BlueprintInstance instance)
        {
            if (piece)
            {
                return TryGetInstance(piece.GetBlueprintID(), out instance);
            }
            instance = null;
            return false;
        }

        public static bool TryGetInstance(ZDOID blueprintID, out BlueprintInstance instance)
        {
            instance = null;
            if (blueprintID == ZDOID.None)
            {
                return false;
            }
            var zdo = ZDOMan.instance.GetZDO(blueprintID);
            if (zdo == null)
            {
                return false;
            }
            var go = ZNetScene.instance.FindInstance(zdo);
            if (go)
            {
                instance = go.GetComponent<BlueprintInstance>();
                return true;
            }
            instance = ZNetScene.instance.CreateObject(zdo)?.GetComponent<BlueprintInstance>();
            return instance;
        }

        private void Awake()
        {
            if (ZNetView.m_forceDisableInit)
            {
                Destroy(this);
                return;
            }
            NetView = GetComponent<ZNetView>();
            Name = NetView.m_zdo.GetString(BlueprintManager.zdoBlueprintName);
        }

        public void SetName(string name)
        {
            if (!NetView.IsOwner())
            {
                return;
            }

            Name = name;
            NetView.m_zdo.Set(BlueprintManager.zdoBlueprintName, name);
        }

        public ZDOID GetID()
        {
            return NetView.m_zdo.m_uid;
        }
        
        public void AddPiece(Piece piece)
        {
            var zdoid = piece?.GetZDOID();
            if (zdoid == null)
            {
                return;
            }
            if (AddPieceID(zdoid.Value))
            {
                piece.m_nview.m_zdo.Set(BlueprintManager.zdoBlueprintID, GetID());
            }
        }
        
        public bool AddPieceID(ZDOID zdoid)
        {
            var ids = GetPieceIDs();
            if (!ids.Add(zdoid))
            {
                return false;
            }
            SetPieceIDs(ids);
            return true;
        }
        
        public void RemovePiece(Piece piece)
        {
            var zdoid = piece?.GetZDOID();
            if (zdoid == null)
            {
                return;
            }
            if (!RemovePieceID(zdoid.Value))
            {
                return;
            }
            if (!GetPieceIDs().Any())
            {
                ZNetScene.instance.Destroy(gameObject);
            }
        }

        public bool RemovePieceID(ZDOID zdoid)
        {
            var ids = GetPieceIDs();
            if (!ids.Remove(zdoid))
            {
                return false;
            }
            SetPieceIDs(ids);
            return true;
        }
        
        /// <summary>
        ///     Get the <see cref="ZDOID">ZDOIDs</see> of all pieces from this blueprint instance
        /// </summary>
        public ZDOIDSet GetPieceIDs()
        {
            byte[] data = NetView.m_zdo.GetByteArray(BlueprintManager.zdoBlueprintPiece);
            if (data == null)
            {
                return new ZDOIDSet();
            }
            return ZDOIDSet.From(new ZPackage(data));
        }

        public void SetPieceIDs(ZDOIDSet zdoids)
        {
            NetView.m_zdo.Set(BlueprintManager.zdoBlueprintPiece, zdoids.ToZPackage().GetArray());
        }

        /// <summary>
        ///     Get all piece instances of this blueprint instance
        /// </summary>
        public List<Piece> GetPieceInstances()
        {
            List<Piece> result = new List<Piece>();
            ZDOIDSet blueprintPieces = GetPieceIDs();
            foreach (ZDOID pieceZDOID in blueprintPieces)
            {
                GameObject pieceObject = ZNetScene.instance.FindInstance(pieceZDOID);
                if (pieceObject && pieceObject.TryGetComponent(out Piece blueprintPiece))
                {
                    result.Add(blueprintPiece);
                }
            }
            return result;
        }
    }
}
