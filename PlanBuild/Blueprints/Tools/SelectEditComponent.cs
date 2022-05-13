using Jotunn.Managers;
using System;
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
                if (ZInput.GetButton(Config.ShiftModifierButton.Name))
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
                height: 250,
                draggable: false);
            panel.SetActive(false);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 5f;

            var copyButton = GUIManager.Instance.CreateButton(
                text: "Copy",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            copyButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            copyButton.GetComponent<Button>().onClick.AddListener(() => OnClick(Copy));
            
            var copySnapButton = GUIManager.Instance.CreateButton(
                text: "Copy with vanilla SnapPoints",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            copySnapButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            copySnapButton.GetComponent<Button>().onClick.AddListener(() => OnClick(CopyWithSnapPoints));

            var saveButton = GUIManager.Instance.CreateButton(
                text: "Save",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            saveButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            saveButton.GetComponent<Button>().onClick.AddListener(() => OnClick(SelectionTools.Save));

            var deleteButton = GUIManager.Instance.CreateButton(
                text: "Delete",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            deleteButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            deleteButton.GetComponent<Button>().onClick.AddListener(() => OnClick(Delete));

            var cancelButton = GUIManager.Instance.CreateButton(
                text: "Cancel",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            cancelButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            cancelButton.GetComponent<Button>().onClick.AddListener(() => OnClick(null));

            void OnClick(Action action)
            {
                action?.Invoke();
                panel.SetActive(false);
                GUIManager.BlockInput(false);
            }

            panel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private void Copy() => SelectionTools.Copy(false);
        
        private void CopyWithSnapPoints() => SelectionTools.Copy(true);

        private void Delete()
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_select_delete_disabled");
                return;
            }

            SelectionTools.Delete();
        }
    }
}