using Jotunn.Managers;
using PlanBuild.Blueprints.Marketplace;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Blueprints.Tools
{
    internal class SelectEditComponent : SelectionToolComponentBase
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

        public override void OnPlacePiece(Player self, Piece piece)
        {
            ShowGUI();
        }

        public void ShowGUI()
        {
            if (!Selection.Instance.Any())
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_blueprint_select_empty"));
                return;
            }

            var panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 400,
                height: 400,
                draggable: false);
            panel.SetActive(false);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 5f;

            var saveButton = GUIManager.Instance.CreateButton(
                text: "Copy",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            saveButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            saveButton.GetComponent<Button>().onClick.AddListener(() => SelectGUI(MakeBlueprint));

            var saveButton2 = GUIManager.Instance.CreateButton(
                text: "Save Blueprint",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            saveButton2.AddComponent<LayoutElement>().preferredHeight = 40f;
            saveButton2.GetComponent<Button>().onClick.AddListener(() => SelectGUI(SaveBlueprint));

            void SelectGUI(Action action)
            {
                action.Invoke();
                panel.SetActive(false);
                GUIManager.BlockInput(false);
            }

            panel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private void MakeBlueprint()
        {
            var bp = new Blueprint();
            bp.Name = "temp";
            bp.Capture(Selection.Instance);
            bp.CreatePiece();
        }

        private void SaveBlueprint()
        {
            var bp = new Blueprint();
            var bpname = Selection.Instance.BlueprintInstance?.ID;
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