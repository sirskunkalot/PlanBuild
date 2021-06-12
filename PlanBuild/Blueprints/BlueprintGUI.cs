using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlanBuild.Blueprints
{
    internal class BlueprintGUI : MonoBehaviour
    {
        public static BlueprintGUI Instance;

        private GameObject GUI;
        private GameObject Window;
        private GameObject Categories;

        private enum Tabs
        {
            Local, Server, Admin
        }

        private class Tab
        {
            private GameObject button;
            private GameObject panel;

            internal Tab(int pos, string name, GameObject panel)
            {
                float width = 150f;
                float height = 30f;
                float offset = width * pos + width / 2;

                button = GUIManager.Instance.CreateButton(name, Instance.Categories.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(offset, 0f), width, height);
                button.SetActive(true);
                
                UIInputHandler component = button.AddComponent<UIInputHandler>();
                component.m_onLeftDown += (UIInputHandler uh) => { Jotunn.Logger.LogMessage($"{name} clicked"); };
            }
        }

        private void Awake()
        {
            Instance = this;
            CreateGUI();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if (GUI && Input.GetKeyDown(KeyCode.End))
            {
                ToggleGUI();
            }
        }

        private void CreateGUI()
        {
            // Get the GUI elements
            GUI = Instantiate(Hud.instance.m_buildHud, GUIManager.PixelFix.transform, true);
            GUI.name = "BlueprintGUI";
            GUI.SetActive(false);

            Window = GUI.transform.Find("bar/SelectionWindow").gameObject;
            Window.SetActive(true);

            Categories = GUI.transform.Find("bar/SelectionWindow/Categories").gameObject;
            Categories.SetActive(true);

            // Clean the GUI
            Destroy(GUI.transform.Find("SelectedInfo").gameObject);
            Destroy(GUI.transform.Find("SelectedInfoOLD").gameObject);
            foreach (Transform child in Window.transform)
            {
                if (child.name != "Bkg2" && child.name != "Categories")
                {
                    Destroy(child.gameObject);
                }
            }
            foreach (Transform child in Categories.transform)
            {
                if (child.name != "TabBorder" && child.name != "TabBase")
                {
                    Destroy(child.gameObject);
                }
            }

            // Append new tabs to the GUI
            foreach (var tab in Enum.GetNames(typeof(Tabs)))
            {
                Tab newTab = new Tab((int)Enum.Parse(typeof(Tabs), tab), tab, null);
            }

            Categories.transform.Find("TabBorder").SetAsLastSibling();
        }

        internal void ToggleGUI()
        {
            GUI.SetActive(!GUI.activeSelf);
        }
    }
}
