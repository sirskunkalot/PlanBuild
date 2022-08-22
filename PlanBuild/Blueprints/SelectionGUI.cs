using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal static class SelectionGUI
    {
        public static void ShowGUI()
        {
            if (!Player.m_localPlayer)
            {
                return;
            }

            var panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 400,
                height: 300,
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
                text: "Copy with vanilla snap points",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            copySnapButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            copySnapButton.GetComponent<Button>().onClick.AddListener(() => OnClick(CopyWithSnapPoints));
            
            var cutButton = GUIManager.Instance.CreateButton(
                text: "Cut",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            cutButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            cutButton.GetComponent<Button>().onClick.AddListener(() => OnClick(Cut));

            var cutSnapButton = GUIManager.Instance.CreateButton(
                text: "Cut with vanilla snap points",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            cutSnapButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            cutSnapButton.GetComponent<Button>().onClick.AddListener(() => OnClick(CutWithSnapPoints));

            var saveButton = GUIManager.Instance.CreateButton(
                text: "Save",
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f));
            saveButton.AddComponent<LayoutElement>().preferredHeight = 40f;
            saveButton.GetComponent<Button>().onClick.AddListener(() => OnClick(SaveGUI));

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
                Object.Destroy(panel);
                GUIManager.BlockInput(false);
            }

            panel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private static void Copy()
        {
            SelectionTools.Copy(Selection.Instance, false);
            Selection.Instance.Clear();
        }

        private static void CopyWithSnapPoints()
        {
            SelectionTools.Copy(Selection.Instance, true);
            Selection.Instance.Clear();
        }

        private static void Cut()
        {
            SelectionTools.Cut(Selection.Instance, false);
            Selection.Instance.Clear();
        }

        private static void CutWithSnapPoints()
        {
            SelectionTools.Cut(Selection.Instance, true);
            Selection.Instance.Clear();
        }
        
        private static void SaveGUI()
        {
            SelectionTools.SaveWithGUI(Selection.Instance);
        }

        private static void Delete()
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_select_delete_disabled");
                return;
            }

            SelectionTools.Delete(Selection.Instance);
            Selection.Instance.Clear();
        }
    }
}
