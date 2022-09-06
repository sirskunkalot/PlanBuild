using Jotunn.Managers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class SelectionTools
    {
        public static void Copy(Selection selection, bool captureVanillaSnapPoints)
        {
            var bp = new Blueprint();
            bp.ID = $"__{BlueprintManager.TemporaryBlueprints.Count + 1:000}";
            bp.Creator = Player.m_localPlayer.GetPlayerName();
            bp.Name = bp.ID;
            bp.Category = BlueprintAssets.CategoryClipboard;
            if (!bp.Capture(selection, captureVanillaSnapPoints))
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bp.ID}");
                selection.Clear();
                return;
            }
            bp.CreatePiece();
            bp.CreateThumbnail(flush: false);
            BlueprintManager.TemporaryBlueprints.Add(bp.ID, bp);
            Player.m_localPlayer.UpdateKnownRecipesList();
            Player.m_localPlayer.UpdateAvailablePiecesList();
            int cat = (int)PieceManager.Instance.GetPieceCategory(BlueprintAssets.CategoryClipboard);
            List<Piece> reorder = Player.m_localPlayer.m_buildPieces.m_availablePieces[cat].OrderByDescending(x => x.name).ToList();
            Player.m_localPlayer.m_buildPieces.m_availablePieces[cat] = reorder;
            Player.m_localPlayer.m_buildPieces.m_selectedCategory = (Piece.PieceCategory)cat;
            Player.m_localPlayer.m_buildPieces.SetSelected(new Vector2Int(0, 0));
            Player.m_localPlayer.SetupPlacementGhost();
            BlueprintGUI.RefreshBlueprints(BlueprintLocation.Temporary);
        }

        public static void Cut(Selection selection, bool captureVanillaSnapPoints)
        {
            Copy(selection, captureVanillaSnapPoints);
            Delete(selection);
        }

        public static void Delete(Selection selection)
        {
            var ZDOs = new List<ZDO>();
            var toClear = selection.ToList();
            foreach (var zdoid in toClear)
            {
                var go = ZNetScene.instance.FindInstance(zdoid);
                if (go && go.TryGetComponent(out ZNetView zNetView))
                {
                    ZDOs.Add(zNetView.m_zdo);
                    zNetView.ClaimOwnership();
                    ZNetScene.instance.Destroy(go);
                }
            }

            var action = new UndoRemove(ZDOs);
            UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);
        }

        public static void Save(Selection selection, string name, string category, string description, bool captureVanillaSnapPoints)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var bp = new Blueprint();

            if (!bp.Capture(selection, captureVanillaSnapPoints))
            {
                return;
            }

            bp.ID = Blueprint.CreateIDString(name);
            bp.Name = name;
            bp.Creator = Player.m_localPlayer.GetPlayerName();
            bp.Category = string.IsNullOrEmpty(category) ? BlueprintAssets.CategoryBlueprints : category;
            bp.Description = description;
            bp.FileLocation = Path.Combine(Config.BlueprintSaveDirectoryConfig.Value, bp.ID + ".blueprint");
            bp.ThumbnailLocation = bp.FileLocation.Replace(".blueprint", ".png");

            if (BlueprintManager.LocalBlueprints.TryGetValue(bp.ID, out var oldbp))
            {
                oldbp.DestroyBlueprint();
                BlueprintManager.LocalBlueprints.Remove(bp.ID);
            }

            if (!bp.ToFile())
            {
                return;
            }

            if (!bp.CreatePiece())
            {
                return;
            }

            BlueprintManager.LocalBlueprints.Add(bp.ID, bp);
            bp.CreateThumbnail();
            BlueprintManager.RegisterKnownBlueprints();
            BlueprintGUI.RefreshBlueprints(BlueprintLocation.Local);
        }

        public static void SaveWithGUI(Selection selection, bool captureVanillaSnapPoints, bool clearSelectionOnCancel)
        {
            var bpname = $"blueprint{BlueprintManager.LocalBlueprints.Count + 1:000}";
            SelectionSaveGUI.Instance.Show(selection, bpname,
                (name, category, description) =>
                {

                    Save(selection, name, category, description, captureVanillaSnapPoints);
                    selection.Clear();
                },
                () =>
                {
                    if (clearSelectionOnCancel)
                    {
                        selection.Clear();
                    }
                });
        }
    }
}
