using Jotunn.GUI;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PlanBuild.Blueprints
{
    internal class SelectionSaveGUI
    {
        public static SelectionSaveGUI Instance;

        private GameObject Prefab;
        private GameObject Window;
        private TMP_Text Pieces;
        private TMP_Text SnapPoints;
        private TMP_Text CenterMarkers;
        private TMP_Text TerrainMods;
        private InputField Name;
        private InputField Category;
        private InputField Description;
        private Button OkButton;
        private Button CancelButton;
        private Action<string, string, string> OkAction;
        private Action CancelAction;

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

            Instance = new SelectionSaveGUI();
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
        ///     Show save GUI
        /// </summary>
        public void Show(Selection selection, string bpname, Action<string, string, string> okAction, Action cancelAction)
        {
            OkAction = okAction;
            CancelAction = cancelAction;

            Pieces.text = Localization.instance.Localize("$gui_bpsave_pieces", selection.Count().ToString());
            SnapPoints.text = Localization.instance.Localize("$gui_bpsave_snappoints", selection.SnapPoints.ToString());
            CenterMarkers.text =
                Localization.instance.Localize("$gui_bpsave_centermarkers", selection.CenterMarkers.ToString());
            TerrainMods.text = Localization.instance.Localize("$gui_bpsave_terrainmods", selection.TerrainMods.ToString());
            Name.text = bpname;
            Category.text = BlueprintAssets.CategoryBlueprints;
            Description.text = null;

            Window.SetActive(true);
            Name.Select();
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

                foreach (TMP_Text txt in Window.GetComponentsInChildren<TMP_Text>(true))
                {
                    txt.font = TMP_FontAsset.CreateFontAsset(GUIManager.Instance.AveriaSerif);
                }

                foreach (InputField fld in Window.GetComponentsInChildren<InputField>(true))
                {
                    GUIManager.Instance.ApplyInputFieldStyle(fld, 16);
                }

                foreach (Button btn in Window.GetComponentsInChildren<Button>(true))
                {
                    GUIManager.Instance.ApplyButtonStyle(btn);
                }

                // Register Components
                Pieces = Window.transform.Find("Details/Pieces").GetComponent<TMP_Text>();
                CenterMarkers = Window.transform.Find("Details/CenterMarkers").GetComponent<TMP_Text>();
                SnapPoints = Window.transform.Find("Details/SnapPoints").GetComponent<TMP_Text>();
                TerrainMods = Window.transform.Find("Details/TerrainMods").GetComponent<TMP_Text>();
                Name = Window.transform.Find("Name/InputField").GetComponent<InputField>();
                Name.textComponent.alignment = TextAnchor.MiddleLeft;
                Category = Window.transform.Find("Category/InputField").GetComponent<InputField>();
                Category.textComponent.alignment = TextAnchor.MiddleLeft;
                Description = Window.transform.Find("Description/InputField").GetComponent<InputField>();
                OkButton = Window.transform.Find("Buttons/OkButton").GetComponent<Button>();
                CancelButton = Window.transform.Find("Buttons/CancelButton").GetComponent<Button>();

                OkButton.onClick.AddListener(() =>
                {
                    OnOk();
                });

                CancelButton.onClick.AddListener(() =>
                {
                    OnCancel();
                });

                // Localize
                Localization.instance.Localize(Instance.Window.transform);

                // Input Behaviour
                Instance.Window.AddComponent<SelectionSaveGUIBehaviour>();

                // Dont display directly
                Window.SetActive(false);
            }
        }

        private void OnOk()
        {
            OkAction.Invoke(Name.text.Trim(), Category.text.Trim(), Description.text.Trim());
            Window.SetActive(false);
            GUIManager.BlockInput(false);
        }

        private void OnCancel()
        {
            CancelAction.Invoke();
            Window.SetActive(false);
            GUIManager.BlockInput(false);
        }

        private class SelectionSaveGUIBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (!IsVisible())
                {
                    return;
                }

                if (Input.GetKeyUp(KeyCode.Return) && !Instance.Description.isFocused)
                {
                    Instance.OnOk();
                }
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    Instance.OnCancel();
                }

                // jees, what a horrible way to do that. need to implement generic code someday
                if (Input.GetKeyDown(KeyCode.Tab) && Instance.Name.isFocused)
                {
                    Instance.Category.Select();
                }
                if (Input.GetKeyDown(KeyCode.Tab) && Instance.Category.isFocused)
                {
                    Instance.Description.Select();
                }
            }
        }
    }
}