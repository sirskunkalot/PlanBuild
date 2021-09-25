using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WearNTear;
using Logger = Jotunn.Logger;

namespace PlanBuild.Blueprints
{
    class Selection : IEnumerable<Piece>
    {
        public const int MAX_HIGHLIGHT_PER_FRAME = 20;
        public const int MAX_GROW_PER_FRAME = 20;

        private static Selection _instance;

        public static Selection Instance
        {
            get
            {
                if (_instance == null) _instance = new Selection();
                return _instance;
            }
        }
        private readonly ZDOIDSet zDOIDs = new ZDOIDSet();
        private readonly Dictionary<ZDOID, Piece> selectedObjectsCache = new Dictionary<ZDOID, Piece>();
        private Coroutine highlightRoutine;
        private Coroutine unhighlightRoutine;

        internal void Highlight()
        {
#if DEBUG
            Logger.LogInfo("Highlighting selection");
#endif 
            if (unhighlightRoutine != null)
            {
#if DEBUG
                Logger.LogInfo("Stopping pending unhighlighting");
#endif 
                PlanBuildPlugin.Instance.StopCoroutine(unhighlightRoutine);
            }
            highlightRoutine = PlanBuildPlugin.Instance.StartCoroutine(HighlightSelection());
        }

        internal void Unhighlight()
        {
            if(highlightRoutine != null)
            {
                return;
            }
            unhighlightRoutine = PlanBuildPlugin.Instance.StartCoroutine(StopHighlightSelection());
        }

        public IEnumerator<YieldInstruction> StopHighlightSelection()
        {
#if DEBUG
            Logger.LogInfo("Waiting for cancel");
#endif 
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            //Don't stop me now
            unhighlightRoutine = null;
#if DEBUG
            Logger.LogInfo("Unhighlighting");
#endif
            int n = 0;
            foreach (Piece selected in new List<Piece>(this))
            {
                if (selected && selected.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.ResetHighlight();
                }
                if (n++ >= MAX_HIGHLIGHT_PER_FRAME)
                {
                    n = 0;
                    yield return null;
                }
            }

#if DEBUG
            Logger.LogInfo("Done unhighlighting");
#endif
        }

        public IEnumerator<YieldInstruction> HighlightSelection()
        {
#if DEBUG
            Logger.LogInfo("Highlighting selection now");
#endif
            int n = 0;
            foreach (Piece selected in new List<Piece>(this))
            {
                if (selected && selected.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.Highlight(Color.green, 0);
                }
                if (n++ >= MAX_HIGHLIGHT_PER_FRAME)
                {
                    n = 0;
                    yield return null;
                }
            }

#if DEBUG
            Logger.LogInfo("Done highlighting");
#endif
            highlightRoutine = null;
        }
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

        internal void RemoveGrowFromPiece(Piece piece)
        { //Use global MonoBehavior to avoid switching tools stopping the iteration
            PlanBuildPlugin.Instance.StartCoroutine(RemoveGrowIterator(piece));
        }


        internal HashSet<Piece> GetSupportingPieces(Piece piece)
        {
            HashSet<Piece> result = new HashSet<Piece>();
            if (piece.TryGetComponent(out WearNTear wearNTear))
            {
                if (wearNTear.m_bounds == null)
                {
                    wearNTear.SetupColliders();
                }
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
            int n = 0;
            while (workingSet.Any())
            {
                Piece piece = workingSet.Dequeue();
                AddPiece(piece);
                n++;
                foreach (Piece nextPiece in GetSupportingPieces(piece))
                {
                    if (!Contains(nextPiece) && !workingSet.Contains(nextPiece))
                    {
                        workingSet.Enqueue(nextPiece);
                    }
                }
                if (n >= MAX_GROW_PER_FRAME)
                {
                    n = 0;
                    yield return null;
                }
            }
        }

        private IEnumerator<YieldInstruction> RemoveGrowIterator(Piece originalPiece)
        {
            Queue<Piece> workingSet = new Queue<Piece>(GetSupportingPieces(originalPiece));
            int n = 0;
            while (workingSet.Any())
            {
                Piece piece = workingSet.Dequeue();
                RemovePiece(piece);
                n++;
                foreach (Piece nextPiece in GetSupportingPieces(piece))
                {
                    if (Contains(nextPiece) && !workingSet.Contains(nextPiece))
                    {
                        workingSet.Enqueue(nextPiece);
                    }
                }
                if (n >= MAX_GROW_PER_FRAME)
                {
                    n = 0;
                    yield return null;
                }
            }
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
                if (piece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.ResetHighlight();
                }
                return true;
            }
            return false;
        }

        internal bool AddPiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && zDOIDs.Add(zdoid.Value))
            {
                selectedObjectsCache[zdoid.Value] = piece;
                if (piece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.Highlight(Color.green, 0);
                }
                return true;
            }
            return false;
        }

        public void Show()
        {
            //TODO
        }

        public void Hide()
        {
            //TODO
        }

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

        public IEnumerator<Piece> GetEnumerator()
        {
            return selectedObjectsCache.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return selectedObjectsCache.Values.GetEnumerator();
        }
    }
}
