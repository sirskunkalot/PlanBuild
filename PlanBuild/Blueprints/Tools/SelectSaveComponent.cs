using System.IO;
using PlanBuild.Blueprints.Marketplace;
using UnityEngine;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectSaveComponent : SelectionToolComponentBase
    {
        public override void OnUpdatePlacement(Player self)
        {
            if (!self.m_placementMarkerInstance)
            {
                return;
            }

            DisableSelectionProjector();

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0)
            {
                if (ZInput.GetButton(Config.CameraModifierButton.Name))
                {
                    UpdateCameraOffset(scrollWheel);
                }
                UndoRotation(self, scrollWheel);
            }
        }

        public override bool OnPlacePiece(Player self, Piece piece)
        {
            return MakeBlueprint();
        }

        private bool MakeBlueprint()
        {
            var bp = new Blueprint();
            var bpname = Selection.Instance.BlueprintName;
            bpname ??= $"blueprint{BlueprintManager.LocalBlueprints.Count + 1:000}";

            if (bp.Capture(Selection.Instance))
            {
                TextInput.instance.m_queuedSign = new BlueprintSaveGUI(bp);
                TextInput.instance.Show(Localization.instance.Localize("$msg_bpcapture_save", bp.GetPieceCount().ToString()), bpname, 50);
            }
            else
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bpname}");
            }

            // Don't place the piece and clutter the world with it
            return false;
        }

        /// <summary>
        ///     Hook for patching
        /// </summary>
        /// <param name="newpiece"></param>
        internal virtual void OnPiecePlaced(GameObject placedPiece)
        {
        }
        
        /// <summary>
        ///     Helper class for naming and saving a captured blueprint via GUI
        ///     Implements the Interface <see cref="TextReceiver" />. SetText is called from <see cref="TextInput" /> upon entering
        ///     an name for the blueprint.<br />
        ///     Save the actual blueprint and add it to the list of known blueprints.
        /// </summary>
        internal class BlueprintSaveGUI : TextReceiver
        {
            private Blueprint newbp;

            public BlueprintSaveGUI(Blueprint bp)
            {
                newbp = bp;
            }

            public string GetText()
            {
                return newbp.Name;
            }

            public void SetText(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                string playerName = Player.m_localPlayer.GetPlayerName();
                string fileName = string.Concat(text.Split(Path.GetInvalidFileNameChars()));

                newbp.ID = $"{playerName}_{fileName}".Trim();
                newbp.Name = text;
                newbp.Creator = playerName;
                newbp.FileLocation = Path.Combine(Config.BlueprintSaveDirectoryConfig.Value, newbp.ID + ".blueprint");
                newbp.ThumbnailLocation = newbp.FileLocation.Replace(".blueprint", ".png");

                if (BlueprintManager.LocalBlueprints.TryGetValue(newbp.ID, out var oldbp))
                {
                    oldbp.DestroyBlueprint();
                    BlueprintManager.LocalBlueprints.Remove(newbp.ID);
                }

                if (!newbp.ToFile())
                {
                    return;
                }

                if (!newbp.CreatePiece())
                {
                    return;
                }

                BlueprintManager.LocalBlueprints.Add(newbp.ID, newbp);
                Selection.Instance.Clear();
                newbp.CreateThumbnail();
                Player.m_localPlayer?.UpdateKnownRecipesList();
                BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);
            }
        }
    }
}