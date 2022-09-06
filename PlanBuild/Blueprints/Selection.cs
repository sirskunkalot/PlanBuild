using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace PlanBuild.Blueprints
{
    internal class Selection : IEnumerable<ZDOID>
    {
        public const int MAX_HIGHLIGHT_PER_FRAME = 50;
        public const int MAX_GROW_PER_FRAME = 50;
        public static int GrowMask;

        private static Selection _instance;
        public static Selection Instance => _instance ??= new Selection();
        
        public int SnapPoints { get; internal set; }
        public int CenterMarkers { get; internal set; }

        private readonly ZDOIDSet SelectedZDOIDs = new ZDOIDSet();
        private readonly ZDOIDSet HighlightedZDOIDs = new ZDOIDSet();
        private Coroutine UnhighlightCoroutine;

        public static void Init()
        {
            GrowMask = LayerMask.GetMask("Default", "piece", "piece_nonsolid");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ZDOID> GetEnumerator()
        {
            foreach (ZDOID zdoid in new List<ZDOID>(SelectedZDOIDs))
            {
                yield return zdoid;
            }
        }

        public bool Add(ZDOID zdoid)
        {
            var go = BlueprintManager.GetGameObject(zdoid);
            if (go && go.TryGetComponent<Piece>(out var piece))
            {
                return AddPiece(piece);
            }
            if (SelectedZDOIDs.Add(zdoid))
            {
                return true;
            }
            return false;
        }

        public bool AddPiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && SelectedZDOIDs.Add(zdoid.Value))
            {
                AttachListener(piece);
                Highlight(zdoid.Value, piece.gameObject);
                if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName, StringComparison.Ordinal))
                {
                    SnapPoints++;
                }
                else if (piece.name.StartsWith(BlueprintAssets.PieceCenterPointName, StringComparison.Ordinal))
                {
                    CenterMarkers++;
                }
                return true;
            }

            return false;
        }

        public void AddPiecesInRadius(Vector3 worldPos, float radius, bool onlyPlanned = false)
        {
            Vector2 pos2d = new Vector2(worldPos.x, worldPos.z);
            foreach (var piece in Piece.m_allPieces)
            {
                Vector3 piecePos = piece.transform.position;
                if (Vector2.Distance(pos2d, new Vector2(piecePos.x, piecePos.z)) <= radius
                    && BlueprintManager.CanCapture(piece, onlyPlanned))
                {
                    AddPiece(piece);
                }
            }
        }

        public void AddPiecesBetween(Piece startPiece, Piece endPiece)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(
                Vector3.Min(startPiece.GetCenter(), endPiece.GetCenter()),
                Vector3.Max(startPiece.GetCenter(), endPiece.GetCenter()));

            foreach (var piece in Piece.m_allPieces
                         .Where(x => x.GetComponentInChildren<Collider>() is Collider col && bounds.Intersects(col.bounds)))
            {
                AddPiece(piece);
            }
        }

        public bool Contains(Piece piece)
        {
            return Contains(piece?.GetZDOID());
        }

        public bool Contains(ZDOID? zdoid)
        {
            return zdoid.HasValue && SelectedZDOIDs.Contains(zdoid.Value);
        }

        public bool Remove(ZDOID zdoid)
        {
            var go = BlueprintManager.GetGameObject(zdoid);
            if (go && go.TryGetComponent<Piece>(out var piece))
            {
                return RemovePiece(piece);
            }
            if (SelectedZDOIDs.Remove(zdoid))
            {
                return true;
            }
            return false;
        }

        public bool RemovePiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && SelectedZDOIDs.Remove(zdoid.Value))
            {
                Unhighlight(zdoid.Value, piece.gameObject);
                if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName, StringComparison.Ordinal))
                {
                    SnapPoints--;
                }
                else if (piece.name.StartsWith(BlueprintAssets.PieceCenterPointName, StringComparison.Ordinal))
                {
                    CenterMarkers--;
                }
                return true;
            }
            return false;
        }

        public void RemovePiecesInRadius(Vector3 worldPos, float radius, bool onlyPlanned = false)
        {
            Vector2 pos2d = new Vector2(worldPos.x, worldPos.z);
            foreach (var piece in Piece.m_allPieces)
            {
                Vector3 piecePos = piece.transform.position;
                if (Vector2.Distance(pos2d, new Vector2(piecePos.x, piecePos.z)) <= radius
                    && BlueprintManager.CanCapture(piece, onlyPlanned))
                {
                    RemovePiece(piece);
                }
            }
        }

        public void Clear()
        {
            foreach (ZDOID zdoid in this)
            {
                GameObject selected = BlueprintManager.GetGameObject(zdoid);
                Unhighlight(zdoid, selected);
            }
            SnapPoints = 0;
            CenterMarkers = 0;
            SelectedZDOIDs.Clear();
        }

        public void AddGrowFromPiece(Piece piece)
        {
            IEnumerator Start(Piece originalPiece)
            {
                yield return null;

                Queue<Piece> workingSet = new Queue<Piece>();
                workingSet.Enqueue(originalPiece);
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
            PlanBuildPlugin.Instance.StartCoroutine(Start(piece));
        }

        public void RemoveGrowFromPiece(Piece piece)
        {
            IEnumerator<YieldInstruction> RemoveGrowIterator(Piece originalPiece)
            {
                yield return null;

                Queue<Piece> workingSet = new Queue<Piece>();
                workingSet.Enqueue(originalPiece);
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
            PlanBuildPlugin.Instance.StartCoroutine(RemoveGrowIterator(piece));
        }

        private HashSet<Piece> GetSupportingPieces(Piece piece)
        {
            HashSet<Piece> result = new HashSet<Piece>();
            Vector3 shellDistance = Vector3.one * Config.SelectionConnectedMarginConfig.Value;
            if (piece.TryGetComponent(out WearNTear wearNTear))
            {
                if (wearNTear.m_bounds == null)
                {
                    wearNTear.SetupColliders();
                }
                foreach (WearNTear.BoundData bound in wearNTear.m_bounds)
                {
                    int num = Physics.OverlapBoxNonAlloc(bound.m_pos, bound.m_size + shellDistance, WearNTear.m_tempColliders, bound.m_rot, GrowMask);
                    for (int i = 0; i < num; i++)
                    {
                        Collider collider = WearNTear.m_tempColliders[i];
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

        public void StartHighlightSelection()
        {
            IEnumerator Start()
            {
                yield return null;
                if (UnhighlightCoroutine != null)
                {
#if DEBUG
                    Logger.LogInfo($"{Time.frameCount} Canceling pending unhighlight");
#endif
                    PlanBuildPlugin.Instance.StopCoroutine(UnhighlightCoroutine);
                    UnhighlightCoroutine = null;
                    //Selection is still highlighted
                    yield break;
                }
#if DEBUG
                Logger.LogInfo($"{Time.frameCount} Starting highlight coroutine");
#endif
                int n = 0;
                foreach (ZDOID zdoid in new List<ZDOID>(this))
                {
                    //Iterating over a copy of the list to avoid ConcurrentModificationEcveption
                    if (!SelectedZDOIDs.Contains(zdoid))
                    {
                        //Piece was unselected while still highlighting
                        continue;
                    }
                    GameObject selected = BlueprintManager.GetGameObject(zdoid);
                    Highlight(zdoid, selected);
                    if (n++ >= MAX_HIGHLIGHT_PER_FRAME)
                    {
                        n = 0;
                        yield return null;
                    }
                }
#if DEBUG
                Logger.LogInfo($"{Time.frameCount} Finished highlight coroutine");
#endif
            }
#if DEBUG
            Logger.LogInfo($"{Time.frameCount} Enqueue highlight coroutine");
#endif

            PlanBuildPlugin.Instance.StartCoroutine(Start());
        }

        public void StopHighlightSelection()
        {
            IEnumerator Stop()
            {
                yield return null;
                yield return null;

#if DEBUG
                Logger.LogInfo($"{Time.frameCount} Starting unhighlight coroutine");
#endif
                int n = 0;
                foreach (ZDOID zdoid in new List<ZDOID>(HighlightedZDOIDs))
                {
                    GameObject selected = BlueprintManager.GetGameObject(zdoid);
                    Unhighlight(zdoid, selected);
                    if (n++ >= MAX_HIGHLIGHT_PER_FRAME)
                    {
                        n = 0;
                        yield return null;
                    }
                }
#if DEBUG
                Logger.LogInfo($"{Time.frameCount} Finished unhighlight coroutine");
#endif
                UnhighlightCoroutine = null;
            }

            if (UnhighlightCoroutine != null)
            {
#if DEBUG
                Logger.LogInfo($"{Time.frameCount} Not queueing unhighlight as there is already a unhighlight coroutine");
#endif
                return;
            }

#if DEBUG
            Logger.LogInfo($"{Time.frameCount} Enqueue unhighlight coroutine");
#endif
            UnhighlightCoroutine = PlanBuildPlugin.Instance.StartCoroutine(Stop());
        }

        public void Highlight(ZDOID zdoid, GameObject selected)
        {
            if (HighlightedZDOIDs.Contains(zdoid))
            {
                return;
            }
            if (selected && selected.TryGetComponent(out WearNTear wearNTear))
            {
                wearNTear.Highlight(Color.green);
                HighlightedZDOIDs.Add(zdoid);
            }
        }

        public void Unhighlight(ZDOID zdoid, GameObject gameObject)
        {
            if (!HighlightedZDOIDs.Contains(zdoid))
            {
                return;
            }
            if (gameObject && gameObject.TryGetComponent(out WearNTear wearNTear))
            {
                wearNTear.ResetHighlight();
            }
            HighlightedZDOIDs.Remove(zdoid);
        }

        public bool IsHighlighted(Piece piece)
        {
            return IsHighlighted(piece?.GetZDOID());
        }

        public bool IsHighlighted(ZDOID? zdoid)
        {
            return zdoid.HasValue && HighlightedZDOIDs.Contains(zdoid.Value);
        }

        internal void OnPieceAwake(Piece piece)
        {
            ZDOID? zdoid = piece.GetZDOID();
            if (!zdoid.HasValue)
            {
                return;
            }
            bool containsPiece = SelectedZDOIDs.Contains(zdoid.Value); ;
            if (containsPiece && !IsHighlighted(piece))
            {
                Highlight(zdoid.Value, piece.gameObject);
            }
            if (containsPiece)
            {
                AttachListener(piece);
            }
        }

        private void AttachListener(Piece piece)
        {
            if (piece.TryGetComponent(out WearNTear wearNTear))
            {
                wearNTear.m_onDestroyed += () => OnWearNTearDestroyed(wearNTear);
            }
        }

        internal void OnPieceUnload(Piece piece)
        {
            if (Contains(piece))
            {
                ZDOID zdoid = piece.m_nview.GetZDO().m_uid;
                HighlightedZDOIDs.Remove(zdoid);
            }
        }

        private void OnWearNTearDestroyed(WearNTear wearNTear)
        {
            ZDOID? zdoid = wearNTear.GetZDOID();
            if (!zdoid.HasValue)
            {
                return;
            }
            SelectedZDOIDs.Remove(zdoid.Value);
            HighlightedZDOIDs.Remove(zdoid.Value);
            if (wearNTear.name.StartsWith(BlueprintAssets.PieceSnapPointName, StringComparison.Ordinal))
            {
                SnapPoints--;
            }
            else if (wearNTear.name.StartsWith(BlueprintAssets.PieceCenterPointName, StringComparison.Ordinal))
            {
                CenterMarkers--;
            }
        }

        public override string ToString()
        {
            string result = string.Empty;
            result += Localization.instance.Localize("$piece_blueprint_select_desc", Instance.Count().ToString());
            if (SnapPoints > 0)
            {
                result += Localization.instance.Localize(" ($piece_blueprint_select_snappoints_desc)", SnapPoints.ToString());
            }
            if (CenterMarkers > 0)
            {
                result += Localization.instance.Localize(" ($piece_blueprint_select_center_desc)");
            }
            return result;
        }
    }
}