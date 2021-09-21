using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WearNTear;

namespace PlanBuild.Blueprints
{
    class Selection: IEnumerable<GameObject>
    {

        private readonly ZDOIDSet zDOIDs = new ZDOIDSet();

        internal void RemovePiecesInRadius(Vector3 worldPos, float radius, bool onlyPlanned = false)
        {
            Vector2 pos2d = new Vector2(worldPos.x, worldPos.z);
            foreach (var piece in Piece.m_allPieces)
            {
                Vector3 piecePos = piece.transform.position;
                if (Vector2.Distance(pos2d, new Vector2(piecePos.x, piecePos.z)) <= radius
                    && BlueprintManager.Instance.CanCapture(piece, onlyPlanned))
                {
                    RemovePiece(piece);
                }
            }
        }

        internal void AddGrowFromPiece(Piece piece)
        {
            //Use global MonoBehavior to avoid switching tools stopping the iteration
            PlanBuildPlugin.Instance.StartCoroutine(AddGrowIterator(piece));
        }

        internal HashSet<Piece> GetSupportingPieces(Piece piece)
        {
            HashSet<Piece> result = new HashSet<Piece>();
            if (piece.TryGetComponent(out WearNTear wearNTear))
            {
                foreach (BoundData bound in wearNTear.m_bounds)
                {
                    int num = Physics.OverlapBoxNonAlloc(bound.m_pos, bound.m_size, m_tempColliders, bound.m_rot, m_rayMask);
                    for (int i = 0; i < num; i++)
                    {
                        Collider collider = m_tempColliders[i];
                        if (wearNTear.m_colliders.Contains(collider) || collider.attachedRigidbody != null || collider.isTrigger)
                        {
                            continue;
                        }
                        Piece supportingPiece = collider.GetComponentInParent<Piece>();
                        if (supportingPiece == null)
                        {
                            continue;
                        }
                        result.Add(supportingPiece);
                    }
                }
            }
            return result;
        }

        private IEnumerator<YieldInstruction> AddGrowIterator(Piece originalPiece)
        {
            Queue<Piece> workingSet = new Queue<Piece>(GetSupportingPieces(originalPiece));
            while (workingSet.Any())
            {
                Piece piece = workingSet.Dequeue();
                AddPiece(piece);
                foreach(Piece nextPiece in GetSupportingPieces(piece))
                {
                    if(!Contains(nextPiece) && !workingSet.Contains(nextPiece))
                    {
                        workingSet.Enqueue(nextPiece);   
                    }
                }
                yield return null;
            }
            yield return null;
        }

        internal bool Contains(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            return zdoid.HasValue && zDOIDs.Contains(zdoid.Value);
        }

        internal bool RemovePiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && zDOIDs.Remove(zdoid.Value))
            {
                selectedObjectsCache.Remove(zdoid.Value);
                return true;
            }
            return false;
        }

        internal bool AddPiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && zDOIDs.Add(zdoid.Value))
            {
                selectedObjectsCache[zdoid.Value] = piece.gameObject;
                return true;
            }
            return false;
        }

        private readonly Dictionary<ZDOID, GameObject> selectedObjectsCache = new Dictionary<ZDOID, GameObject>();

        public void Clear()
        {
            zDOIDs.Clear();
            selectedObjectsCache.Clear();
        }

        public void AddPiecesInRadius(Vector3 worldPos, float radius, bool onlyPlanned = false)
        { 
            Vector2 pos2d = new Vector2(worldPos.x, worldPos.z);
            foreach (var piece in Piece.m_allPieces)
            {
                Vector3 piecePos = piece.transform.position;
                if (Vector2.Distance(pos2d, new Vector2(piecePos.x, piecePos.z)) <= radius
                    && BlueprintManager.Instance.CanCapture(piece, onlyPlanned))
                {
                    AddPiece(piece);
                }
            } 
        }

        public IEnumerator<GameObject> GetEnumerator()
        {
            return selectedObjectsCache.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return selectedObjectsCache.Values.GetEnumerator();
        }
    }
}
