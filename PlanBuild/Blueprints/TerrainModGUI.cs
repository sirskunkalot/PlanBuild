using Jotunn.GUI;
using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Blueprints
{
    internal class TerrainModGUI
    {
        public static TerrainModGUI Instance;

        private GameObject Prefab;
        private GameObject Window;
        private Dropdown Shape;
        private InputField Radius;
        private InputField Rotation;
        private InputField Smooth;
        private Dropdown Paint;
        private Button OkButton;
        private Button CancelButton;
        private Action<string, string, string, string, string> OkAction;
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

            Instance = new TerrainModGUI();
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
        public void Show(string shape, string radius, string rotation, string smooth, string paint, Action<string, string, string, string, string> okAction, Action cancelAction)
        {
            OkAction = okAction;
            CancelAction = cancelAction;

            Shape.value = Shape.options.FindIndex(x => x.text.Equals(shape, StringComparison.OrdinalIgnoreCase));
            Radius.text = radius;
            Rotation.text = rotation;
            Smooth.text = smooth;
            Paint.value = Paint.options.FindIndex(x => x.text.Equals(paint, StringComparison.OrdinalIgnoreCase));

            Window.SetActive(true);
            Shape.Select();
            GUIManager.BlockInput(true);
        }

        private void Register()
        {
            if (!Window)
            {
                Jotunn.Logger.LogDebug("Recreating TerrainModGUI");

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

                foreach (Dropdown dwn in Window.GetComponentsInChildren<Dropdown>(true))
                {
                    GUIManager.Instance.ApplyDropdownStyle(dwn, 15);
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
                Shape = Window.transform.Find("Shape/Dropdown").GetComponent<Dropdown>();
                Radius = Window.transform.Find("Radius/InputField").GetComponent<InputField>();
                Radius.textComponent.alignment = TextAnchor.MiddleLeft;
                Rotation = Window.transform.Find("Rotation/InputField").GetComponent<InputField>();
                Rotation.textComponent.alignment = TextAnchor.MiddleLeft;
                Smooth = Window.transform.Find("Smooth/InputField").GetComponent<InputField>();
                Smooth.textComponent.alignment = TextAnchor.MiddleLeft;
                Paint = Window.transform.Find("Paint/Dropdown").GetComponent<Dropdown>();
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
                Instance.Window.AddComponent<SaveGUIBehaviour>();

                // Dont display directly
                Window.SetActive(false);
            }
        }

        private void OnOk()
        {
            OkAction.Invoke(Shape.options[Shape.value].text, Radius.text.Trim(), Rotation.text.Trim(),
                Smooth.text.Trim(), Paint.options[Paint.value].text);
            Window.SetActive(false);
            GUIManager.BlockInput(false);
        }

        private void OnCancel()
        {
            CancelAction.Invoke();
            Window.SetActive(false);
            GUIManager.BlockInput(false);
        }

        private class SaveGUIBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (!IsVisible())
                {
                    return;
                }

                if (Input.GetKeyUp(KeyCode.Return))
                {
                    Instance.OnOk();
                }
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    Instance.OnCancel();
                }
            }
        }
    }
}