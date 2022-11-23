using Jotunn.GUI;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Blueprints
{
    internal class BlueprintGUI
    {
        public static BlueprintGUI Instance;

        private GameObject Prefab;

        public GameObject Window { get; set; }

        public ActionAppliedOverlay ActionAppliedOverlay { get; set; }

        public BlueprintTab CurrentTab { get; set; }

        public BlueprintTab LocalTab { get; set; } = new BlueprintTab();

        public BlueprintTab ClipboardTab { get; set; } = new BlueprintTab();

        public BlueprintTab ServerTab { get; set; } = new BlueprintTab();

        public static void Init(GameObject prefab)
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            Instance = new BlueprintGUI();
            Instance.Prefab = prefab;

            GUIManager.OnCustomGUIAvailable += Instance.Register;
        }

        /// <summary>
        ///     Check availability
        /// </summary>
        /// <returns>true if the <see cref="Instance"/> and the <see cref="Window"/> are not null</returns>
        public static bool IsAvailable()
        {
            return Instance != null && Instance.Window != null;
        }

        /// <summary>
        ///     Check visibility
        /// </summary>
        /// <returns>true if the GUI is available and visible</returns>
        public static bool IsVisible()
        {
            return IsAvailable() && Instance.Window.activeSelf;
        }

        /// <summary>
        ///     Check if any visible fiels have focus
        /// </summary>
        /// <returns>true if any visible fiels have focus</returns>
        public static bool TextFieldHasFocus()
        {
            if (Instance.CurrentTab == null)
            {
                return false;
            }

            return Instance.CurrentTab.DetailDisplay.Name.isFocused
                   || Instance.CurrentTab.DetailDisplay.Category.isFocused
                   || Instance.CurrentTab.DetailDisplay.Description.isFocused;
        }

        public static void ShowTab(BlueprintLocation location)
        {
            if (!IsAvailable())
            {
                return;
            }

            Instance.LocalTab.TabElements.TabContent.SetActive(location == BlueprintLocation.Local);
            Instance.ClipboardTab.TabElements.TabContent.SetActive(location == BlueprintLocation.Temporary);
            Instance.ServerTab.TabElements.TabContent.SetActive(location == BlueprintLocation.Server);

            switch (location)
            {
                case BlueprintLocation.Local:
                    Instance.CurrentTab = Instance.LocalTab;
                    break;
                case BlueprintLocation.Temporary:
                    Instance.CurrentTab = Instance.ClipboardTab;
                    break;
                case BlueprintLocation.Server:
                    Instance.CurrentTab = Instance.ServerTab;
                    break;
            }
        }

        /// <summary>
        ///     Loop through the tab displays and DestroyImmediate all <see cref="BlueprintDetailContent"/> instances
        /// </summary>
        /// <param name="location">Which tab should be cleared</param>
        public static void ClearBlueprints(BlueprintLocation location)
        {
            if (!IsAvailable())
            {
                return;
            }

            if (location == BlueprintLocation.Local || location == BlueprintLocation.All)
            {
                foreach (var cat in Instance.LocalTab.ListDisplay.Categories)
                {
                    UnityEngine.Object.DestroyImmediate(cat);
                }
                foreach (var detail in Instance.LocalTab.ListDisplay.Blueprints)
                {
                    UnityEngine.Object.DestroyImmediate(detail.ContentHolder);
                }
                Instance.LocalTab.ListDisplay.Categories.Clear();
                Instance.LocalTab.ListDisplay.Blueprints.Clear();
                Instance.LocalTab.DetailDisplay.Clear();
            }
            if (location == BlueprintLocation.Temporary || location == BlueprintLocation.All)
            {
                foreach (var cat in Instance.ClipboardTab.ListDisplay.Categories)
                {
                    UnityEngine.Object.DestroyImmediate(cat);
                }
                foreach (var detail in Instance.ClipboardTab.ListDisplay.Blueprints)
                {
                    UnityEngine.Object.DestroyImmediate(detail.ContentHolder);
                }
                Instance.ClipboardTab.ListDisplay.Categories.Clear();
                Instance.ClipboardTab.ListDisplay.Blueprints.Clear();
                Instance.ClipboardTab.DetailDisplay.Clear();
            }
            if (location == BlueprintLocation.Server || location == BlueprintLocation.All)
            {
                foreach (var cat in Instance.ServerTab.ListDisplay.Categories)
                {
                    UnityEngine.Object.DestroyImmediate(cat);
                }
                foreach (var detail in Instance.ServerTab.ListDisplay.Blueprints)
                {
                    UnityEngine.Object.DestroyImmediate(detail.ContentHolder);
                }
                Instance.ServerTab.ListDisplay.Categories.Clear();
                Instance.ServerTab.ListDisplay.Blueprints.Clear();
                Instance.ServerTab.DetailDisplay.Clear();
            }
        }

        /// <summary>
        ///     Loop through the tab display, clear them and refresh from the blueprint dictionary
        /// </summary>
        /// <param name="location">Which tab should be reloaded</param>
        public static void RefreshBlueprints(BlueprintLocation location)
        {
            if (!IsAvailable())
            {
                return;
            }

            ClearBlueprints(location);

            if (location == BlueprintLocation.Local || location == BlueprintLocation.All)
            {
                foreach (var cat in BlueprintManager.LocalBlueprints.GroupBy(x => x.Value.Category).OrderBy(x => x.Key))
                {
                    Instance.LocalTab.ListDisplay.AddCategory(cat.Key);
                    foreach (var entry in cat.OrderBy(x => x.Value.Name))
                    {
                        Instance.LocalTab.ListDisplay.AddBlueprint(entry.Key, entry.Value);
                    }
                }
            }
            if (location == BlueprintLocation.Temporary || location == BlueprintLocation.All)
            {
                foreach (var entry in BlueprintManager.TemporaryBlueprints.OrderBy(x => x.Value.Name))
                {
                    Instance.ClipboardTab.ListDisplay.AddBlueprint(entry.Key, entry.Value);
                }
            }
            if (location == BlueprintLocation.Server || location == BlueprintLocation.All)
            {
                foreach (var cat in BlueprintManager.ServerBlueprints.GroupBy(x => x.Value.Category).OrderBy(x => x.Key))
                {
                    Instance.ServerTab.ListDisplay.AddCategory(cat.Key);
                    foreach (var entry in cat.OrderBy(x => x.Value.Name))
                    {
                        Instance.ServerTab.ListDisplay.AddBlueprint(entry.Key, entry.Value);
                    }
                }
            }
        }

        /// <summary>
        ///     Hide, open or toggle the main window
        /// </summary>
        /// <param name="shutWindow"></param>
        /// <param name="openWindow"></param>
        public void Toggle(bool shutWindow = false, bool openWindow = false)
        {
            bool newState;

            // Requesting window shut.
            if (shutWindow)
            {
                newState = false;
            }
            // Requesting open window.
            else if (openWindow)
            {
                newState = true;
            }
            // Toggle current state
            else
            {
                newState = !Window.activeSelf;
            }
            Window.SetActive(newState);

            // Toggle input
            GUIManager.BlockInput(newState);
        }

        /// <summary>
        ///     Reload the blueprint dictionary from the disk or server and refresh the tab display
        /// </summary>
        /// <param name="originTab"></param>
        public void ReloadBlueprints(BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Get the local blueprint list
                    BlueprintSync.GetLocalBlueprints();
                    break;

                case BlueprintLocation.Server:
                    // Get the server blueprint list
                    Instance.ActionAppliedOverlay.Show();
                    BlueprintSync.GetServerBlueprints((success, message) =>
                    {
                        Instance.ActionAppliedOverlay.SetResult(success, message);
                    }, useCache: false);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        ///     Display the details of a blueprint values on the content side
        /// </summary>
        /// <param name="blueprint"></param>
        /// <param name="originTab"></param>
        public void ShowBlueprint(BlueprintDetailContent blueprint, BlueprintLocation originTab)
        {
            BlueprintTab tabToUse = null;
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    tabToUse = Instance.LocalTab;
                    break;

                case BlueprintLocation.Temporary:
                    tabToUse = Instance.ClipboardTab;
                    break;

                case BlueprintLocation.Server:
                    tabToUse = Instance.ServerTab;
                    break;

                default:
                    break;
            }
            if (tabToUse == null) return;
            tabToUse.DetailDisplay.SetActive(blueprint);
        }

        public void SaveBlueprint(BlueprintDetailContent detail, BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Save the blueprint changes
                    if (detail != null && BlueprintManager.LocalBlueprints.TryGetValue(detail.ID, out var bplocal))
                    {
                        bplocal.Name = string.IsNullOrEmpty(detail.Name) ? bplocal.Name : detail.Name;
                        bplocal.Category = string.IsNullOrEmpty(detail.Category) ? BlueprintAssets.CategoryBlueprints : detail.Category;
                        bplocal.Description = detail.Description;

                        BlueprintSync.SaveLocalBlueprint(bplocal.ID);
                    }
                    break;

                case BlueprintLocation.Temporary:
                    // Create a new local blueprint from the temp blueprint
                    if (detail != null && BlueprintManager.TemporaryBlueprints.TryGetValue(detail.ID, out var bptemp))
                    {
                        var name = string.IsNullOrEmpty(detail.Name) ? bptemp.Name : detail.Name;
                        var category =
                            string.IsNullOrEmpty(detail.Category) ||
                            detail.Category.Equals(BlueprintAssets.CategoryClipboard)
                                ? BlueprintAssets.CategoryBlueprints
                                : detail.Category;
                        var description = detail.Description;

                        BlueprintSync.SaveTempBlueprint(bptemp.ID, name, category, description);
                    }
                    break;

                case BlueprintLocation.Server:
                    // Upload the blueprint to the server again to save the changes
                    if (detail != null && BlueprintManager.ServerBlueprints.TryGetValue(detail.ID, out var bpserver))
                    {
                        bpserver.Name = string.IsNullOrEmpty(detail.Name) ? bpserver.Name : detail.Name;
                        bpserver.Category = string.IsNullOrEmpty(detail.Category) ? BlueprintAssets.CategoryBlueprints : detail.Category;
                        bpserver.Description = detail.Description;

                        Instance.ActionAppliedOverlay.Show();
                        BlueprintSync.PushServerBlueprint(bpserver.ID, (success, message) =>
                        {
                            Instance.ActionAppliedOverlay.SetResult(success, message);
                        });
                    }
                    break;

                default:
                    break;
            }
        }

        public void TransferBlueprint(BlueprintDetailContent detail, BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Push local blueprint to the server
                    if (detail != null && BlueprintManager.LocalBlueprints.ContainsKey(detail.ID))
                    {
                        Instance.ActionAppliedOverlay.Show();
                        BlueprintSync.PushLocalBlueprint(detail.ID, (success, message) =>
                        {
                            Instance.ActionAppliedOverlay.SetResult(success, message);
                        });
                    }
                    break;

                case BlueprintLocation.Server:
                    // Save server blueprint locally
                    if (detail != null && BlueprintManager.ServerBlueprints.ContainsKey(detail.ID))
                    {
                        BlueprintSync.SaveServerBlueprint(detail.ID);
                    }
                    break;

                default:
                    break;
            }
        }

        public void DeleteBlueprint(BlueprintDetailContent detail, BlueprintLocation originTab)
        {
            switch (originTab)
            {
                case BlueprintLocation.Local:
                    // Remove local blueprint
                    if (detail != null && BlueprintManager.LocalBlueprints.ContainsKey(detail.ID))
                    {
                        BlueprintSync.RemoveLocalBlueprint(detail.ID);
                    }
                    break;

                case BlueprintLocation.Temporary:
                    // Remove local blueprint
                    if (detail != null && BlueprintManager.TemporaryBlueprints.ContainsKey(detail.ID))
                    {
                        BlueprintSync.RemoveTempBlueprint(detail.ID);
                    }
                    break;

                case BlueprintLocation.Server:
                    // Remove server blueprint when admin
                    if (detail != null && BlueprintManager.ServerBlueprints.ContainsKey(detail.ID))
                    {
                        Instance.ActionAppliedOverlay.Show();
                        BlueprintSync.RemoveServerBlueprint(detail.ID, (success, message) =>
                        {
                            Instance.ActionAppliedOverlay.SetResult(success, message);
                        });
                    }
                    break;

                default:
                    break;
            }
        }

        public void Register()
        {
            if (!Window)
            {
                Jotunn.Logger.LogDebug("Recreating BlueprintGUI");

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
                    txt.font = GUIManager.Instance.AveriaSerifBold;
                }

                foreach (InputField fld in Window.GetComponentsInChildren<InputField>(true))
                {
                    GUIManager.Instance.ApplyInputFieldStyle(fld, 16);
                }

                foreach (Button btn in Window.GetComponentsInChildren<Button>(true))
                {
                    if (btn.name.Equals("ListEntry", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    GUIManager.Instance.ApplyButtonStyle(btn);
                }

                foreach (ScrollRect scrl in Window.GetComponentsInChildren<ScrollRect>(true))
                {
                    GUIManager.Instance.ApplyScrollRectStyle(scrl);
                }

                // Global overlay
                try
                {
                    ActionAppliedOverlay = new ActionAppliedOverlay();
                    ActionAppliedOverlay.Register(Window.transform.Find("ActionAppliedOverlay"));
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogDebug($"Failed in the action overlay: {ex}");
                }

                // Create Tab instances
                try
                {
                    LocalTab.TabElements.Register(Window.transform, "Local", "$gui_bpmarket_localblueprints");
                    LocalTab.ListDisplay.Register(LocalTab.TabElements.TabContent.transform, BlueprintLocation.Local);
                    LocalTab.DetailDisplay.Register(LocalTab.TabElements.TabContent.transform, BlueprintLocation.Local);
                    LocalTab.TabElements.TabButton.onClick.AddListener(() =>
                    {
                        ShowTab(BlueprintLocation.Local);
                    });
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogDebug($"Failed in LocalTab: {ex}");
                }

                try
                {
                    ClipboardTab.TabElements.Register(Window.transform, "Clipboard", "$gui_bpmarket_clipboardblueprints");
                    ClipboardTab.ListDisplay.Register(ClipboardTab.TabElements.TabContent.transform, BlueprintLocation.Temporary);
                    ClipboardTab.DetailDisplay.Register(ClipboardTab.TabElements.TabContent.transform, BlueprintLocation.Temporary);
                    ClipboardTab.TabElements.TabButton.onClick.AddListener(() =>
                    {
                        ShowTab(BlueprintLocation.Temporary);
                    });
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogDebug($"Failed in ClipboardTab: {ex}");
                }

                try
                {
                    ServerTab.TabElements.Register(Window.transform, "Server", "$gui_bpmarket_serverblueprints");
                    ServerTab.ListDisplay.Register(ServerTab.TabElements.TabContent.transform, BlueprintLocation.Server);
                    ServerTab.DetailDisplay.Register(ServerTab.TabElements.TabContent.transform, BlueprintLocation.Server);
                    ServerTab.TabElements.TabButton.onClick.AddListener(() =>
                    {
                        ShowTab(BlueprintLocation.Server);
                    });
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogDebug($"Failed in ServerTab: {ex}");
                }

                // Localize
                Localization.instance.Localize(Instance.Window.transform);

                // Close Behaviour
                Instance.Window.AddComponent<CloseGUIBehaviour>();

                // Show initial tab
                ShowTab(BlueprintLocation.Local);

                // Init blueprint lists
                RefreshBlueprints(BlueprintLocation.All);

                // Dont display directly
                Window.SetActive(false);
            }
        }

        private class CloseGUIBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (IsVisible() && Input.GetKeyUp(KeyCode.Escape))
                {
                    Instance.Window.SetActive(false);
                    GUIManager.BlockInput(false);
                }
            }
        }
    }

    internal class BlueprintTab
    {
        // Moved things out to seperate classes to make it easier to understand the flow.
        public BlueprintTabElements TabElements { get; set; } = new BlueprintTabElements();

        // Holds Lists of Blueprints within the tab.
        public BlueprintListDisplay ListDisplay { get; set; } = new BlueprintListDisplay();

        // Holds Detail of the selected blueprint.
        public BlueprintDetailDisplay DetailDisplay { get; set; } = new BlueprintDetailDisplay();
    }

    internal class BlueprintTabElements
    {
        public Transform TabTransform { get; set; }
        public Button TabButton { get; set; }
        public Text TabText { get; set; }
        public GameObject TabContent { get; set; }

        public void Register(Transform window, string tabName, string tabText)
        {
            try
            {
                GameObject button =
                    GUIManager.Instance.CreateButton(tabText, window.Find("Tabs"), Vector2.zero, Vector2.zero, Vector2.zero);
                button.name = tabName;
                TabTransform = button.transform;
                TabButton = button.GetComponent<Button>();
                TabText = button.transform.Find("Text").GetComponent<Text>();
                TabContent = UnityEngine.Object.Instantiate(window.Find("TabContent").gameObject, window.Find("Content"));
                TabContent.name = tabName;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed in BlueprintTabElements: {ex}");
            }
        }
    }

    internal class BlueprintListDisplay
    {
        public BlueprintLocation TabType { get; set; } = BlueprintLocation.Local;

        private GameObject BlueprintCategoryPrefab { get; set; }
        private GameObject BlueprintEntryPrefab { get; set; }

        // Parent for the Content Holder - Where we push new things.
        public Transform ScrollContentParent { get; set; }

        public UIConfirmationOverlay ConfirmationOverlay { get; set; } = new UIConfirmationOverlay();

        // All the categories that exist in this tab's list.
        public List<GameObject> Categories { get; set; } = new List<GameObject>();

        // All the blueprints that exist in this tab's list.
        public List<BlueprintDetailContent> Blueprints { get; set; } = new List<BlueprintDetailContent>();

        public void AddCategory(string name)
        {
            try
            {
                GameObject cat = UnityEngine.Object.Instantiate(BlueprintCategoryPrefab, ScrollContentParent);
                cat.SetActive(true);
                cat.GetComponent<Text>().text = name;
                Categories.Add(cat);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed to create new category: {ex}");
            }
        }

        public BlueprintDetailContent AddBlueprint(string id, Blueprint bp)
        {
            if (Blueprints.Any(i => i.ID == id))
            {
                Jotunn.Logger.LogDebug($"Blueprint {id} already exists in {this}.");
                return null;
            }

            BlueprintDetailContent newBp = new BlueprintDetailContent();
            try
            {
                newBp.ContentHolder = UnityEngine.Object.Instantiate(BlueprintEntryPrefab, ScrollContentParent);
                newBp.ContentHolder.SetActive(true);
                newBp.Button = newBp.ContentHolder.GetComponent<Button>();
                newBp.Text = newBp.ContentHolder.transform.Find("Text").GetComponent<Text>();

                newBp.ID = bp.ID;
                newBp.Name = bp.Name;
                newBp.Category = bp.Category;
                newBp.Creator = bp.Creator;
                newBp.Count = bp.GetPieceCount().ToString();
                newBp.SnapPoints = bp.GetSnapPointCount().ToString();
                newBp.TerrainMods = bp.GetTerrainModCount().ToString();
                newBp.Description = bp.Description;
                newBp.Text.text = bp.ToGUIString();
                if (bp.Thumbnail != null)
                {
                    newBp.Icon = Sprite.Create(bp.Thumbnail, new Rect(0, 0, bp.Thumbnail.width, bp.Thumbnail.height), Vector2.zero);
                }
                newBp.Button.onClick.AddListener(() =>
                {
                    BlueprintGUI.Instance.ShowBlueprint(newBp, TabType);
                });
                Blueprints.Add(newBp);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed to load new blueprint: {ex}");
            }
            return newBp;
        }

        public BlueprintDetailContent RemoveBlueprint(string id)
        {
            BlueprintDetailContent blueprintToRemove = Blueprints.FirstOrDefault(i => i.ID == id);
            if (blueprintToRemove != null)
            {
                Blueprints.Remove(blueprintToRemove);
                UnityEngine.Object.Destroy(blueprintToRemove.ContentHolder);
                return blueprintToRemove;
            }
            return null;
        }

        public void Register(Transform contentTransform, BlueprintLocation tabType)
        {
            TabType = tabType;
            try
            {
                BlueprintCategoryPrefab = contentTransform.Find("List/ListCategory").gameObject;
                BlueprintEntryPrefab = contentTransform.Find("List/ListEntry").gameObject;
                ScrollContentParent = contentTransform.Find("List/Viewport/Content");
                ConfirmationOverlay = new UIConfirmationOverlay();
                Transform overlayParent = contentTransform.Find("ConfirmationOverlay");
                ConfirmationOverlay.Register(overlayParent);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed in BlueprintListDisplay: {ex}");
            }
        }
    }

    internal class BlueprintDetailDisplay
    {
        public BlueprintLocation TabType { get; set; } = BlueprintLocation.Local;

        public BlueprintDetailContent SelectedBlueprintDetail { get; set; }

        public Text ID { get; set; }
        public Text Creator { get; set; }
        public Text Count { get; set; }
        public Text SnapPoints { get; set; }
        public Text TerrainMods { get; set; }
        public Image Icon { get; set; }
        public InputField Name { get; set; }
        public InputField Category { get; set; }
        public InputField Description { get; set; }

        // Main Action Buttons
        public Button ReloadButton { get; set; }

        public Button SaveButton { get; set; }
        public Button TransferButton { get; set; }
        public Button DeleteButton { get; set; }

        // Overlay screens, for confirmations.
        public UIConfirmationOverlay ConfirmationOverlay { get; set; } = new UIConfirmationOverlay();

        public void SetActive(BlueprintDetailContent blueprint)
        {
            if (ConfirmationOverlay.IsVisible())
            {
                return;
            }

            SelectedBlueprintDetail = blueprint;

            Name.onEndEdit.RemoveAllListeners();
            Category.onEndEdit.RemoveAllListeners();
            Description.onEndEdit.RemoveAllListeners();

            ID.text = blueprint.ID;
            Creator.text = blueprint.Creator;
            Count.text = blueprint.Count;
            SnapPoints.text = blueprint.SnapPoints;
            TerrainMods.text = blueprint.TerrainMods;
            if (blueprint.Icon == null)
            {
                Icon.gameObject.SetActive(false);
                Icon.sprite = null;
            }
            else
            {
                Icon.gameObject.SetActive(true);
                Icon.sprite = blueprint.Icon;
            }
            Name.text = blueprint.Name;
            Category.text = blueprint.Category;
            Description.text = blueprint.Description;

            Name.onEndEdit.AddListener((text) => { blueprint.Name = text; });
            Category.onEndEdit.AddListener((text) => { blueprint.Category = text; });
            Description.onEndEdit.AddListener((text) => { blueprint.Description = text; });

            SaveButton.onClick.RemoveAllListeners();
            TransferButton.onClick.RemoveAllListeners();
            DeleteButton.onClick.RemoveAllListeners();

            SaveButton.onClick.AddListener(() =>
            {
                ConfirmationOverlay.Show(Localization.instance.Localize("$gui_bpmarket_savebp", TabType.ToString(), Name.text), () =>
                {
                    BlueprintGUI.Instance.SaveBlueprint(blueprint, TabType);
                });
            });

            TransferButton.onClick.AddListener(() =>
            {
                ConfirmationOverlay.Show(Localization.instance.Localize("$gui_bpmarket_transferbp", TabType.ToString(), Name.text), () =>
                {
                    BlueprintGUI.Instance.TransferBlueprint(blueprint, TabType);
                });
            });

            DeleteButton.onClick.AddListener(() =>
            {
                ConfirmationOverlay.Show(Localization.instance.Localize("$gui_bpmarket_deletebp", TabType.ToString(), Name.text), () =>
                {
                    BlueprintGUI.Instance.DeleteBlueprint(blueprint, TabType);
                });
            });
        }

        public void Clear()
        {
            Name.onEndEdit.RemoveAllListeners();
            Category.onEndEdit.RemoveAllListeners();
            Description.onEndEdit.RemoveAllListeners();

            ID.text = "ID";
            Creator.text = null;
            Count.text = null;
            SnapPoints.text = null;
            TerrainMods.text = null;
            Icon.sprite = null;
            Icon.gameObject.SetActive(false);
            Name.text = null;
            Category.text = null;
            Description.text = null;
        }

        public void Register(Transform contentTransform, BlueprintLocation tabType)
        {
            TabType = tabType;
            try
            {
                // Registering confirmation overlay.
                ConfirmationOverlay = new UIConfirmationOverlay();
                Transform overlayParent = contentTransform.Find("ConfirmationOverlay");
                ConfirmationOverlay.Register(overlayParent);

                Transform detail = contentTransform.Find("Detail");
                ID = detail.Find("ID").GetComponent<Text>();
                Creator = detail.Find("Creator").GetComponent<Text>();
                Count = detail.Find("Count").GetComponent<Text>();
                SnapPoints = detail.Find("SnapPoints").GetComponent<Text>();
                TerrainMods = detail.Find("TerrainMods").GetComponent<Text>();
                Icon = detail.Find("Thumbnail").GetComponent<Image>();
                Icon.gameObject.SetActive(false);
                Name = detail.Find("Name").GetComponent<InputField>();
                Category = detail.Find("Category").GetComponent<InputField>();
                Description = detail.Find("Description").GetComponent<InputField>();

                ReloadButton = detail.Find("RefreshButton").GetComponent<Button>();
                SaveButton = detail.Find("SaveButton").GetComponent<Button>();
                TransferButton = detail.Find("TransferButton").GetComponent<Button>();
                DeleteButton = detail.Find("DeleteButton").GetComponent<Button>();

                // Type dependend actions
                if (tabType == BlueprintLocation.Local)
                {
                    TransferButton.GetComponentInChildren<Text>().text = "$gui_bpmarket_upload";
                }
                if (tabType == BlueprintLocation.Temporary)
                {
                    ReloadButton.gameObject.SetActive(false);
                    TransferButton.gameObject.SetActive(false);
                }
                if (tabType == BlueprintLocation.Server)
                {
                    TransferButton.GetComponentInChildren<Text>().text = "$gui_bpmarket_download";
                }

                // Add valheim refresh icon
                var img = ReloadButton.transform.Find("Image").GetComponent<Image>();
                img.sprite = GUIManager.Instance.GetSprite("refresh_icon");
                var outline = img.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;

                // Reload button is global
                ReloadButton.onClick.AddListener(() =>
                {
                    BlueprintGUI.Instance.ReloadBlueprints(TabType);
                });
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed in BlueprintDetailDisplay: {ex}");
            }
        }
    }

    internal class BlueprintDetailContent
    {
        public GameObject ContentHolder { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Creator { get; set; }
        public string Count { get; set; }
        public string SnapPoints { get; set; }
        public string TerrainMods { get; set; }
        public string Description { get; set; }
        public Text Text { get; set; }
        public Sprite Icon { get; set; }
        public Button Button { get; set; }
    }

    internal class UIConfirmationOverlay
    {
        public Transform ContentHolder { get; set; }
        public Text ConfirmationDisplayText { get; set; }
        public Button CancelButton { get; set; }
        public Button ConfirmButton { get; set; }

        public void Show(string displayText, Action confirmAction)
        {
            ContentHolder.gameObject.SetActive(true);
            ConfirmationDisplayText.text = displayText;
            ConfirmButton.onClick.AddListener(() =>
            {
                confirmAction?.Invoke();
                Close();
            });
        }

        public void Close()
        {
            ContentHolder.gameObject.SetActive(false);
            ConfirmButton.onClick.RemoveAllListeners();
        }

        public bool IsVisible()
        {
            return ContentHolder.gameObject.activeSelf;
        }

        public void Register(Transform overlayTransform)
        {
            ContentHolder = overlayTransform;
            ConfirmationDisplayText = overlayTransform.Find("ConfirmText").GetComponent<Text>();
            ConfirmButton = overlayTransform.Find("ConfirmationButton").GetComponent<Button>();
            CancelButton = overlayTransform.Find("CancelButton").GetComponent<Button>();
            CancelButton.onClick.AddListener(Close);
        }
    }

    internal class ActionAppliedOverlay
    {
        public Transform ContentHolder { get; set; }
        public Text DisplayText { get; set; }
        public Button OKButton { get; set; }

        public void Show()
        {
            ContentHolder.gameObject.SetActive(true);
            OKButton.gameObject.SetActive(false);
            DisplayText.text = Localization.instance.Localize("$gui_bpmarket_working");
        }

        public void SetResult(bool success, string message)
        {
            if (success)
            {
                Close();
            }
            else
            {
                OKButton.gameObject.SetActive(true);
                DisplayText.text = message;
            }
        }

        public void Close()
        {
            ContentHolder.gameObject.SetActive(false);
        }

        public void Register(Transform overlayTransform)
        {
            ContentHolder = overlayTransform;
            DisplayText = overlayTransform.Find("DisplayText").GetComponent<Text>();
            OKButton = overlayTransform.Find("OKButton").GetComponent<Button>();
            OKButton.onClick.AddListener(Close);
        }
    }
}