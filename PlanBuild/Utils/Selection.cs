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
    class Selection : IEnumerable<ZDOID>
    {
        public const int MAX_HIGHLIGHT_PER_FRAME = 30;
        public const int MAX_GROW_PER_FRAME = 30;
        public static int growMask;

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
        
        private int snapPoints = 0;
        private int centerMarkers = 0;
        private bool highlighted = false;
        private bool hidden = false;

        private Coroutine highlightRoutine; 
        private Coroutine unhighlightRoutine;
         
        public IEnumerator<ZDOID> GetEnumerator()
        {
           foreach(ZDOID zdoid in new List<ZDOID>(zDOIDs))
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
            if(!required)
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
            yield return new WaitForSeconds(0.5f);

            //Don't stop me now
            unhighlightRoutine = null;
            highlighted = false;
#if DEBUG
            Logger.LogInfo("Unhighlighting");
#endif
            int n = 0;
            foreach (ZDOID zdoid in new List<ZDOID>(this))
            {
                GameObject selected = GetGameObject(zdoid);
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

        internal void OnPieceAwake(Piece piece)
        {
            if (highlighted && Contains(piece)) { 
                Highlight(piece.gameObject); 
            }
        }

        public IEnumerator<YieldInstruction> HighlightSelection()
        {
#if DEBUG
            Logger.LogInfo("Highlighting selection now");
#endif
            highlighted = true;
            int n = 0;
            foreach (ZDOID zdoid in new List<ZDOID>(this))
            {
                GameObject selected = GetGameObject(zdoid);
                Highlight(selected);
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

        public static void Highlight(GameObject selected)
        {
            if (selected && selected.TryGetComponent(out WearNTear wearNTear))
            {
                wearNTear.Highlight(Color.green);
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
            PlanBuildPlugin.Instance.StartCoroutine(AddGrowIterator(piece));
        }

        internal void RemoveGrowFromPiece(Piece piece)
        { 
            PlanBuildPlugin.Instance.StartCoroutine(RemoveGrowIterator(piece));
        }


        internal HashSet<Piece> GetSupportingPieces(Piece piece)
        {
            HashSet<Piece> result = new HashSet<Piece>();
            Vector3 shellDistance = Vector3.one * BlueprintConfig.SelectionConnectedMarginConfig.Value;
            if (piece.TryGetComponent(out WearNTear wearNTear))
            {
                if (wearNTear.m_bounds == null)
                {
                    wearNTear.SetupColliders();
                }
                foreach (BoundData bound in wearNTear.m_bounds)
                {
                    int num = Physics.OverlapBoxNonAlloc(bound.m_pos, bound.m_size + shellDistance, m_tempColliders, bound.m_rot, growMask);
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

        private IEnumerator<YieldInstruction> RemoveGrowIterator(Piece originalPiece)
        {
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
                if (piece.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.ResetHighlight();
                }
                if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName))
                {
                    snapPoints--;
                }
                else if (piece.name.StartsWith(BlueprintAssets.PieceCenterPointName))
                {
                    centerMarkers--;
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
                Highlight(piece.gameObject);
                if (piece.name.StartsWith(BlueprintAssets.PieceSnapPointName))
                {
                    snapPoints++;
                }
                else if (piece.name.StartsWith(BlueprintAssets.PieceCenterPointName))
                {
                    centerMarkers++;
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
                if (selected && selected.TryGetComponent(out WearNTear wearNTear))
                {
                    wearNTear.ResetHighlight();
                } 
            }
            zDOIDs.Clear(); 
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
            if(snapPoints > 0)
            {
                result += Localization.instance.Localize(" ($piece_blueprint_select_snappoints_desc)", snapPoints.ToString());
            }
            if(centerMarkers > 0)
            {
                result += Localization.instance.Localize(" ($piece_blueprint_select_center_desc)");
            }
            return result;
        }
    }
}
