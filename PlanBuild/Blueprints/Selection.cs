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

        private readonly ZDOIDSet selectedZDOIDs = new ZDOIDSet();
        private readonly ZDOIDSet highlightedZDOIDs = new ZDOIDSet();

        private int SnapPoints;
        private int CenterMarkers;
        private bool Highlighted;

        private Coroutine unhighlightCoroutine;

        public IEnumerator<ZDOID> GetEnumerator()
        {
            foreach (ZDOID zdoid in new List<ZDOID>(selectedZDOIDs))
            {
                yield return zdoid;
            }
        }

        public GameObject GetGameObject(ZDOID zdoid, bool required = false)
        {
            GameObject go = ZNetScene.instance.FindInstance(zdoid);
            if (go)
            {
                return go;
            }
            if (!required)
            {
                return null;
            }
#if DEBUG
            Logger.LogWarning($"Creating object for {zdoid}");
#endif
            return ZNetScene.instance.CreateObject(ZDOMan.instance.GetZDO(zdoid));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void StartHighlightSelection()
        {
            IEnumerator Start()
            {
                yield return null;
                if (unhighlightCoroutine != null)
                {
#if DEBUG
                    Logger.LogInfo($"{Time.frameCount} Canceling pending unhighlight");
#endif
                    PlanBuildPlugin.Instance.StopCoroutine(unhighlightCoroutine);
                    unhighlightCoroutine = null;
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
                    if (!selectedZDOIDs.Contains(zdoid))
                    {
                        //Piece was unselected while still highlighting
                        continue;
                    }
                    GameObject selected = GetGameObject(zdoid);
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

            Highlighted = true;
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
                foreach (ZDOID zdoid in new List<ZDOID>(highlightedZDOIDs))
                {
                    GameObject selected = GetGameObject(zdoid);
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
                unhighlightCoroutine = null;
            }

            if (unhighlightCoroutine != null)
            {
#if DEBUG
                Logger.LogInfo($"{Time.frameCount} Not queueing unhighlight as there is already a unhighlight coroutine");
#endif
                return;
            }

#if DEBUG
            Logger.LogInfo($"{Time.frameCount} Enqueue unhighlight coroutine");
#endif
            unhighlightCoroutine = PlanBuildPlugin.Instance.StartCoroutine(Stop());
            Highlighted = false;
        }

        internal void OnPieceAwake(Piece piece)
        {
            ZDOID? zdoid = piece.GetZDOID();
            if (!zdoid.HasValue)
            {
                return;
            }
            bool containsPiece = selectedZDOIDs.Contains(zdoid.Value); ;
            if (Highlighted && containsPiece && !IsHighlighted(piece))
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

        private bool IsHighlighted(Piece piece)
        {
            ZDOID zdoid = piece.m_nview.GetZDO().m_uid;
            return highlightedZDOIDs.Contains(zdoid);
        }

        internal void OnPieceUnload(Piece piece)
        {
            if (Contains(piece))
            {
                ZDOID zdoid = piece.m_nview.GetZDO().m_uid;
                highlightedZDOIDs.Remove(zdoid);
            }
        }

        internal void OnWearNTearDestroyed(WearNTear wearNTear)
        {
            ZDOID? zdoid = wearNTear.GetZDOID();
            if (!zdoid.HasValue)
            {
                return;
            }
            selectedZDOIDs.Remove(zdoid.Value);
            highlightedZDOIDs.Remove(zdoid.Value);
        }

        public void Highlight(ZDOID zdoid, GameObject selected)
        {
            if (highlightedZDOIDs.Contains(zdoid))
            {
                return;
            }
            if (selected && selected.TryGetComponent(out WearNTear wearNTear))
            {
                wearNTear.Highlight(Color.green);
                highlightedZDOIDs.Add(zdoid);
            }
        }

        public void Unhighlight(ZDOID zdoid, GameObject gameObject)
        {
            if (!highlightedZDOIDs.Contains(zdoid))
            {
                return;
            }
            if (gameObject.TryGetComponent(out WearNTear wearNTear))
            {
                wearNTear.ResetHighlight();
                highlightedZDOIDs.Remove(zdoid);
            }
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

        internal void RemoveGrowFromPiece(Piece piece)
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

        internal HashSet<Piece> GetSupportingPieces(Piece piece)
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

        internal bool Contains(Piece piece)
        {
            ZDOID? zdoid = piece.GetZDOID();
            return zdoid.HasValue && selectedZDOIDs.Contains(zdoid.Value);
        }

        internal bool RemovePiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && selectedZDOIDs.Remove(zdoid.Value))
            {
                Unhighlight(zdoid.Value, piece.gameObject);
                if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName))
                {
                    SnapPoints--;
                }
                else if (piece.name.StartsWith(BlueprintAssets.PieceCenterPointName))
                {
                    CenterMarkers--;
                }
                return true;
            }
            return false;
        }

        internal bool AddPiece(Piece piece)
        {
            ZDOID? zdoid = piece.m_nview?.GetZDO()?.m_uid;
            if (zdoid.HasValue && selectedZDOIDs.Add(zdoid.Value))
            {
                AttachListener(piece);
                Highlight(zdoid.Value, piece.gameObject);
                if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName))
                {
                    SnapPoints++;
                }
                else if (piece.name.StartsWith(BlueprintAssets.PieceCenterPointName))
                {
                    CenterMarkers++;
                }
                return true;
            }

            return false;
        }

        public void Clear()
        {
            foreach (ZDOID zdoid in this)
            {
                GameObject selected = GetGameObject(zdoid);
                Unhighlight(zdoid, selected);
            }
            selectedZDOIDs.Clear();
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

        public new string ToString()
        {
            string result = Localization.instance.Localize("$piece_blueprint_select_desc", Selection.Instance.Count().ToString());
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