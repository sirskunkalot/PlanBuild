using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class SelectionTools
    {
        public static void Copy(Selection selection, bool captureCurrentSnapPoints, bool keepMarkers)
        {
            var bp = new Blueprint();
            bp.ID = NextClipboardID();
            bp.Creator = Player.m_localPlayer.GetPlayerName();
            bp.Name = bp.ID;
            bp.Category = BlueprintAssets.CategoryClipboard;
            if (!bp.Capture(selection, captureCurrentSnapPoints, keepMarkers))
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bp.ID}");
                selection.Clear();
                return;
            }
            AddToClipboard(bp, true);
        }

        /// <summary>
        ///     Register a new clipboard blueprint, optionally selecting it for placement
        /// </summary>
        internal static void AddToClipboard(Blueprint bp, bool select)
        {
            bp.CreatePiece();
            if (select)
            {
                // Instantiate up front so CreateThumbnail keeps the ghost for the selection below
                // instead of building it twice; the timer lets the watchdog clean it up even if
                // the selection fails
                bp.InstantiateGhost();
                bp.GhostActiveTime = Time.time;
            }
            bp.CreateThumbnail(flush: false);
            BlueprintManager.TemporaryBlueprints.Add(bp.ID, bp);
            Player.m_localPlayer.UpdateKnownRecipesList();
            Player.m_localPlayer.UpdateAvailablePiecesList();
            if (select)
            {
                // Select the new piece by reference - its grid position depends on the piece table order
                if (bp.PiecePrefab && Player.m_localPlayer.SetSelectedPiece(bp.PiecePrefab))
                {
                    Player.m_localPlayer.SetupPlacementGhost();
                }
                else
                {
                    Jotunn.Logger.LogWarning($"Could not select blueprint {bp.ID} in the piece table");
                }
            }
            BlueprintGUI.RefreshBlueprints(BlueprintLocation.Temporary);
        }

        /// <summary>
        ///     Highest clipboard number + 1. Count + 1 reused an existing ID once a clipboard blueprint
        ///     other than the last one was deleted.
        /// </summary>
        internal static string NextClipboardID()
        {
            int highest = 0;
            foreach (string id in BlueprintManager.TemporaryBlueprints.Keys)
            {
                if (id.StartsWith("__", StringComparison.Ordinal)
                    && int.TryParse(id.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                {
                    highest = Math.Max(highest, number);
                }
            }
            return $"__{highest + 1:000}";
        }

        public static void Cut(Selection selection, bool captureCurrentSnapPoints, bool keepMarkers)
        {
            Copy(selection, captureCurrentSnapPoints, keepMarkers);
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

            // An empty undo step would make the next bp.undo seem to do nothing
            if (ZDOs.Any())
            {
                var action = new UndoRemove(ZDOs);
                UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);
            }
        }

        public static void Save(Selection selection, string name, string category, string description, bool captureCurrentSnapPoints, bool keepMarkers)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var bp = new Blueprint();

            if (!bp.Capture(selection, captureCurrentSnapPoints, keepMarkers))
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

        public static void SaveWithGUI(Selection selection, bool captureCurrentSnapPoints, bool keepMarkers)
        {
            var bpname = $"blueprint{BlueprintManager.LocalBlueprints.Count + 1:000}";
            SelectionSaveGUI.Instance.Show(selection, bpname,
                (name, category, description) =>
                {

                    Save(selection, name, category, description, captureCurrentSnapPoints, keepMarkers);
                    selection.Clear();
                },
                () =>
                {
                    selection.Clear();
                });
        }
    }
}
