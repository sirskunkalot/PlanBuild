using Jotunn.GUI;
using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Blueprints
{
    internal class SelectionGUI
    {
        public static SelectionGUI Instance;

        private GameObject Prefab;
        private GameObject Window;
        private Button CopyButton;
        private Button CutButton;
        private Button SaveButton;
        private Toggle SnapPointsToggle;
        private Toggle MarkersToggle;
        private Button DeleteButton;
        private Button ClearButton;
        private Button CancelButton;

        /// <summary>
        ///     Init
        /// </summary>
        /// <param name="prefab"></param>
        public static void Init(GameObject prefab)
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            Instance = new SelectionGUI();
            Instance.Prefab = prefab;

            GUIManager.OnCustomGUIAvailable += Instance.Register;
        }

        /// <summary>
        ///     Check availability
        /// </summary>
        /// <returns>true if the <see cref="Instance"/> is not null</returns>
        public static bool IsAvailable()
        {
            return Instance != null;
        }

        /// <summary>
        ///     Check visibility
        /// </summary>
        /// <returns>true if the GUI is available and visible</returns>
        public static bool IsVisible()
        {
            return IsAvailable() && Instance.Window != null && Instance.Window.activeSelf;
        }

        /// <summary>
        ///     Show selection GUI
        /// </summary>
        public void Show()
        {
            if (!Player.m_localPlayer)
            {
                return;
            }

            SnapPointsToggle.SetIsOnWithoutNotify(false);
            MarkersToggle.SetIsOnWithoutNotify(false);
            Window.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private void Register()
        {
            if (!Window)
            {
                Jotunn.Logger.LogDebug("Recreating SaveGUI");

                // Assigning the main window, so we can disable/enable it as we please.
                Window = UnityEngine.Object.Instantiate(Prefab, GUIManager.CustomGUIFront.transform);
                Window.AddComponent<DragWindowCntrl>();

                // Setting some vanilla styles
                var panel = Window.GetComponent<Image>();
                panel.sprite = GUIManager.Instance.GetSprite("woodpanel_settings");
                panel.type = Image.Type.Sliced;
                panel.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");

                foreach (Text txt in Window.GetComponentsInChildren<Text>(true))
                {
                    txt.font = GUIManager.Instance.AveriaSerif;
                }

                foreach (Button btn in Window.GetComponentsInChildren<Button>(true))
                {
                    GUIManager.Instance.ApplyButtonStyle(btn);
                }

                foreach (Toggle tgl in Window.GetComponentsInChildren<Toggle>(true))
                {
                    GUIManager.Instance.ApplyToogleStyle(tgl);
                }

                // Register Components
                CopyButton = Window.transform.Find("CopyButton").GetComponent<Button>();
                CopyButton.onClick.AddListener(() => OnClick(Copy));
                CutButton = Window.transform.Find("CutButton").GetComponent<Button>();
                CutButton.onClick.AddListener(() => OnClick(Cut));
                SaveButton = Window.transform.Find("SaveButton").GetComponent<Button>();
                SaveButton.onClick.AddListener(() => OnClick(SaveGUI));
                SnapPointsToggle = Window.transform.Find("SnapPointsToggle").GetComponent<Toggle>();
                MarkersToggle = Window.transform.Find("MarkersToggle").GetComponent<Toggle>();
                DeleteButton = Window.transform.Find("DeleteButton").GetComponent<Button>();
                DeleteButton.onClick.AddListener(() => OnClick(Delete));
                ClearButton = Window.transform.Find("ClearButton").GetComponent<Button>();
                ClearButton.onClick.AddListener(() => OnClick(Clear));
                CancelButton = Window.transform.Find("CancelButton").GetComponent<Button>();
                CancelButton.onClick.AddListener(() => OnClick(Cancel));

                void OnClick(Action action)
                {
                    action?.Invoke();
                    Window.SetActive(false);
                    GUIManager.BlockInput(false);
                }

                // Localize
                Localization.instance.Localize(Instance.Window.transform);

                // Input Behaviour
                Instance.Window.AddComponent<SelectionGUIBehaviour>();

                // Dont display directly
                Window.SetActive(false);
            }
        }

        private void Cancel()
        {
            Window.SetActive(false);
            GUIManager.BlockInput(false);
        }

        private class SelectionGUIBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (!IsVisible())
                {
                    return;
                }

                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    Instance.Cancel();
                }
            }
        }

        private void Copy()
        {
            SelectionTools.Copy(Selection.Instance, SnapPointsToggle.isOn, MarkersToggle.isOn);
            Selection.Instance.Clear();
        }

        private void Cut()
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_select_cut_disabled");
                return;
            }

            SelectionTools.Cut(Selection.Instance, SnapPointsToggle.isOn, MarkersToggle.isOn);
            Selection.Instance.Clear();
        }

        private void SaveGUI()
        {
            SelectionTools.SaveWithGUI(Selection.Instance, SnapPointsToggle.isOn, MarkersToggle.isOn);
        }

        private void Delete()
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$msg_select_delete_disabled");
                return;
            }

            SelectionTools.Delete(Selection.Instance);
            Selection.Instance.Clear();
        }

        private void Clear()
        {
            Selection.Instance.Clear();
        }
    }
}
