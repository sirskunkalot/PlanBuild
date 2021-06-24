using Jotunn.Managers;
using Jotunn.Utils;
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

        private GameObject MenuPrefab;
        private GameObject ContainerPrefab;
        private GameObject IconPrefab;

        public GameObject Window { get; set; }
        public void Toggle(bool shutWindow = false, bool openWindow = false)
        {
            // Requesting window shut.
            if (shutWindow)
            {
                Window.SetActive(false);
                return;
            }
            // Requesting open window.
            if (openWindow)
            {
                Window.SetActive(true);
                return;
            }
            // Toggle current state
            Window.SetActive(!Window.activeSelf);
        }

        public BlueprintMenuElements MenuElements { get; set; }

        public BlueprintTab CurrentTab { get; set; }

        public BlueprintTab MyTab { get; set; } = new BlueprintTab();

        public BlueprintTab ServerTab { get; set; } = new BlueprintTab();

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = new BlueprintGUI();
            }

            if (!GUIManager.IsHeadless())
            {
                AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("blueprintmenuui", typeof(PlanBuildPlugin).Assembly);
                Instance.MenuPrefab = bundle.LoadAsset<GameObject>("BlueprintMenu");
                Instance.ContainerPrefab = bundle.LoadAsset<GameObject>("BPDetailsContainer");
                Instance.IconPrefab = bundle.LoadAsset<GameObject>("BPIcon");
                bundle.Unload(false);

                GUIManager.OnPixelFixCreated += Instance.Register;
            }
        }

        public void Register()
        {
            if (!Window)
            {
                // Assigning the main window, so we can disable/enable it as we please.
                Window = UnityEngine.Object.Instantiate(MenuPrefab, GUIManager.PixelFix.transform);
                Window.SetActive(false);
                try
                {
                    RectTransform windowRectTrans = Window.GetComponent<RectTransform>();

                    // The window is positioned in the center of the screen --
                    Vector2 bottomLeftCorner = new Vector2((-1 * (Screen.width / 2)), (-1 * (Screen.height / 2)));
                    Vector2 skillWindowSize = new Vector2(600, 400);
                    Vector2 bottomAlignedWindow = new Vector2(-(skillWindowSize.x / 2), bottomLeftCorner.y + skillWindowSize.y / 2);

                    // Half of the screen, - half of our window, centered position.
                    Vector2 centerOScreen = new Vector2((Screen.width / 2) - (windowRectTrans.rect.size.x / 2), (Screen.height / 2) - (windowRectTrans.rect.size.y / 2));
                    windowRectTrans.anchoredPosition = new Vector2(0, 0);

                    // Simple drag and drop script. -- allows for drag/drop of any ui component.
                    Window.AddComponent<UIDragDrop>();
                    Jotunn.Logger.LogDebug($"Blueprint menu ui position was set: {windowRectTrans.anchoredPosition.x}, {windowRectTrans.anchoredPosition.y}");

                    try
                    {
                        MenuElements = new BlueprintMenuElements();
                        MenuElements.CloseButton = Window.transform.Find("CloseButton").GetComponent<Button>();
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug("Failed in the menu elements");
                    }

                    try
                    {
                        MyTab.TabElements.Register(Window.transform, tabName: "MyTab", buttonSearchName: "MyTabButton");
                        MyTab.ListDisplay.Register(MyTab.TabElements.TabTransform, ContainerPrefab, TabsEnum.MyBlueprints);
                        MyTab.DetailDisplay.Register(MyTab.TabElements.TabTransform, ContainerPrefab, TabsEnum.MyBlueprints);
                        MyTab.TabElements.TabButton.onClick.AddListener(() =>
                        {
                            CurrentTab = MyTab;
                        });
                        CurrentTab = MyTab;
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug("Failed in myTab");
                    }

                    try
                    {
                        ServerTab.TabElements.Register(Window.transform, tabName: "ServerTab", buttonSearchName: "ServerTabButton");
                        ServerTab.ListDisplay.Register(ServerTab.TabElements.TabTransform, ContainerPrefab, TabsEnum.ServerBlueprints);
                        ServerTab.DetailDisplay.Register(ServerTab.TabElements.TabTransform, ContainerPrefab, TabsEnum.ServerBlueprints);
                        ServerTab.TabElements.TabButton.onClick.AddListener(() =>
                        {
                            CurrentTab = ServerTab;
                        });
                    }
                    catch (Exception ex)
                    {
                        Jotunn.Logger.LogDebug("Failed in ServerTab");
                    }

                    // temp add local blueprints
                    foreach (var entry in BlueprintManager.Instance.Blueprints.OrderBy(x => x.Key))
                    {
                        MyTab.ListDisplay.AddBlueprint(entry.Key, entry.Value.ToGUIString());
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogDebug($"Failed to load Blueprint Window. {ex}");
                }
            }
        }

        public static void SetActiveDetails(BluePrintDetailContent blueprint, TabsEnum tab)
        {
            BlueprintTab tabToUse = null;
            switch (tab)
            {
                case TabsEnum.MyBlueprints:
                    tabToUse = Instance.MyTab;
                    break;
                case TabsEnum.ServerBlueprints:
                    tabToUse = Instance.ServerTab;
                    break;
                default:
                    break;
            }
            if (tabToUse == null) return;
            tabToUse.DetailDisplay.SetActive(blueprint);
        }

        public static void PushBlueprint(BluePrintDetailContent blueprint, TabsEnum originTab)
        {
            BlueprintTab tabToUse = null;
            switch (originTab)
            {
                // Switch to the other tab.
                case TabsEnum.MyBlueprints:
                    tabToUse = Instance.ServerTab;
                    break;
                case TabsEnum.ServerBlueprints:
                    tabToUse = Instance.MyTab;
                    break;
                default:
                    break;
            }
            if (tabToUse == null) return;
            tabToUse.ListDisplay.AddBlueprint(blueprint.Id, blueprint.Description.text);
        }

        public static void SyncBlueprint(BluePrintDetailContent blueprint, TabsEnum originTab)
        {
            // Probably don't need this..
            BlueprintTab tabToUse = null;
            switch (originTab)
            {
                case TabsEnum.MyBlueprints:
                    tabToUse = Instance.MyTab;
                    break;
                case TabsEnum.ServerBlueprints:
                    tabToUse = Instance.ServerTab;
                    break;
                default:
                    break;
            }
            if (tabToUse == null) return;
            // tabToUse.ListDisplay.AddBlueprint(blueprint.Id, blueprint.Description.text);
            // Save it off.
        }

        public static void DeleteBlueprint(BluePrintDetailContent blueprint, TabsEnum originTab)
        {
            BlueprintTab tabToUse = null;
            switch (originTab)
            {
                case TabsEnum.MyBlueprints:
                    tabToUse = Instance.MyTab;
                    break;
                case TabsEnum.ServerBlueprints:
                    tabToUse = Instance.ServerTab;
                    break;
                default:
                    break;
            }
            if (tabToUse == null) return;
            tabToUse.ListDisplay.RemoveBlueprint(blueprint.Id);
        }
    }

    public enum TabsEnum { MyBlueprints, ServerBlueprints }
    public class BlueprintMenuElements
    {
        public Button CloseButton { get; set; }
    }

    public class BlueprintTab
    {
        // Moved things out to seperate classes to make it easier to understand the flow.
        public BlueprintTabElements TabElements { get; set; } = new BlueprintTabElements();

        // Holds Lists of Blueprints within the tab.
        public BlueprintListDisplay ListDisplay { get; set; } = new BlueprintListDisplay();

        // Holds Detail of the selected blueprint.
        public BlueprintDetailDisplay DetailDisplay { get; set; } = new BlueprintDetailDisplay();
    }

    public class BlueprintTabElements
    {
        public Transform TabTransform { get; set; }
        public Button TabButton { get; set; }
        public Text TabText { get; set; }
        // This is to indicate it is the activate tab.
        public Image TabButtonSelector { get; set; }

        public void Register(Transform window, string tabName, string buttonSearchName)
        {
            try
            {
                TabTransform = window.Find($"{tabName}");
                TabButton = window.Find($"{buttonSearchName}").GetComponent<Button>();
                TabText = window.Find($"{buttonSearchName}/Label").GetComponent<Text>();
                TabButtonSelector = window.Find($"{buttonSearchName}/Enabled").GetComponent<Image>();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Failed in BlueprintTabElements");
            }
        }
    }

    public class BlueprintListDisplay
    {
        public TabsEnum TabType { get; set; } = TabsEnum.MyBlueprints;

        private GameObject BlueprintDetailPrefab { get; set; }

        // Parent for the Content Holder - Where we push new things.
        public Transform ScrollContentParent { get; set; }

        public UIConfirmationOverlay ConfirmationOverlay { get; set; } = new UIConfirmationOverlay();

        // All the blueprints that exist in this tab's list.
        public List<BluePrintDetailContent> Blueprints { get; set; } = new List<BluePrintDetailContent>();

        public BluePrintDetailContent AddBlueprint(string id, string description)
        {
            if (Blueprints.Any(i => i.Id == id))
            {
                Jotunn.Logger.LogDebug($"blueprint already exists here.");
                return null;
            }

            BluePrintDetailContent newBp = new BluePrintDetailContent();
            newBp.Id = id;
            try
            {
                newBp.ContentHolder = UnityEngine.Object.Instantiate(BlueprintDetailPrefab, ScrollContentParent);
                newBp.IconButton = newBp.ContentHolder.transform.Find("IconButton").GetComponent<Button>();
                newBp.Icon = newBp.ContentHolder.transform.Find("IconButton/BPImage").GetComponent<Image>();
                newBp.SortUpButton = newBp.ContentHolder.transform.Find("SortUpButton").GetComponent<Button>();
                newBp.SortDownButton = newBp.ContentHolder.transform.Find("SortDownButton").GetComponent<Button>();
                newBp.Description = newBp.ContentHolder.transform.Find("Text").GetComponent<Text>();
                // Set the description text
                newBp.Description.text = description;
                newBp.IconButton.onClick.AddListener(() =>
                {
                    BlueprintGUI.SetActiveDetails(newBp, TabType);
                });
                Blueprints.Add(newBp);

                FixSortButtons();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Failed to load new blueprint. {ex}");
            }
            return newBp;
        }

        public BluePrintDetailContent RemoveBlueprint(string id)
        {
            BluePrintDetailContent blueprintToRemove = Blueprints.FirstOrDefault(i => i.Id == id);
            if (blueprintToRemove != null)
            {
                Blueprints.Remove(blueprintToRemove);
                // I said see-yah later.
                GameObject.Destroy(blueprintToRemove.ContentHolder);
                FixSortButtons();
                return blueprintToRemove;
            }
            return null;
        }

        public void MoveUp(string id)
        {
            BluePrintDetailContent toMoveup = Blueprints.FirstOrDefault(i => i.Id == id);
            if (toMoveup == null) return;

            int indexOfBp = Blueprints.IndexOf(toMoveup);
            if (indexOfBp == 0) return;

            BluePrintDetailContent detailToSwapWith = Blueprints[indexOfBp - 1];
            Swap(toMoveup, detailToSwapWith);
        }

        public void MoveDown(string id)
        {
            BluePrintDetailContent toMoveup = Blueprints.FirstOrDefault(i => i.Id == id);
            if (toMoveup == null) return;

            int indexOfBp = Blueprints.IndexOf(toMoveup);
            if (indexOfBp >= Blueprints.Count) return;

            BluePrintDetailContent detailToSwapWith = Blueprints[indexOfBp + 1];
            Swap(toMoveup, detailToSwapWith);
        }

        private void Swap(BluePrintDetailContent from, BluePrintDetailContent to)
        {
            string idCopy = from.Id;
            Sprite spriteToCopy = from.Icon.sprite;
            string descriptionToCopy = from.Description.text;

            from.Id = to.Id;
            from.Icon.sprite = to.Icon.sprite;
            from.Description.text = to.Description.text;

            from.Id = idCopy;
            from.Icon.sprite = spriteToCopy;
            to.Description.text = descriptionToCopy;
        }

        public void FixSortButtons()
        {
            foreach (var blueprintDetail in Blueprints)
            {
                blueprintDetail.SortUpButton.gameObject.SetActive(true);
                blueprintDetail.SortDownButton.gameObject.SetActive(true);

                // Remove any old listeners
                blueprintDetail.SortDownButton.onClick.RemoveAllListeners();
                blueprintDetail.SortUpButton.onClick.RemoveAllListeners();

                blueprintDetail.SortUpButton.onClick.AddListener(() =>
                {
                    MoveUp(blueprintDetail.Id);
                });

                blueprintDetail.SortDownButton.onClick.AddListener(() =>
                {
                    MoveDown(blueprintDetail.Id);
                });
            }

            BluePrintDetailContent firstPrintDetailContent = Blueprints.FirstOrDefault();
            if (firstPrintDetailContent != null)
            {
                // Set the first to not have sort down (enable the sort down button).
                firstPrintDetailContent.SortUpButton.gameObject.SetActive(false);
            }
            BluePrintDetailContent lastBlueprintDetail = Blueprints.LastOrDefault();
            if (lastBlueprintDetail != null)
            {
                // Set the first to not have sort down (enable the sort down button).
                lastBlueprintDetail.SortDownButton.gameObject.SetActive(false);
            }
        }

        public void Register(Transform tabTrans, GameObject uiBlueprintDetailPrefab, TabsEnum tabType)
        {
            TabType = tabType;
            try
            {
                BlueprintDetailPrefab = uiBlueprintDetailPrefab;
                ScrollContentParent = tabTrans.Find("BlueprintScrollView/Viewport/Content");
                ConfirmationOverlay = new UIConfirmationOverlay();
                Transform overlayParent = tabTrans.Find("BlueprintConfirmationOverlay");
                ConfirmationOverlay.Register(overlayParent);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Failed in BlueprintListDisplay");
            }
        }
    }

    public class BlueprintDetailDisplay
    {
        public TabsEnum TabType { get; set; } = TabsEnum.MyBlueprints;

        private GameObject BlueprintIconPrefab { get; set; }

        //Use Id passed to link more details or whatever..
        public BluePrintDetailContent SelectedBlueprintDetail { get; set; }

        // Inputs Fields.
        public InputField Name { get; set; }
        public InputField Creator { get; set; }
        public InputField Description { get; set; }

        // Main Action Buttons
        public Button SyncButton { get; set; }
        public Button PushButton { get; set; }
        public Button DeleteButton { get; set; }

        // Overlay screens, for confirmations.
        public UIConfirmationOverlay ConfirmationOverlay { get; set; } = new UIConfirmationOverlay();

        // Parent for the Content Holder - Where we push new things.
        public Transform IconScrollContentParent { get; set; }

        // Managed Lists
        // Images of the Blueprint Selected.
        public List<Sprite> Icons { get; set; } = new List<Sprite>();

        public void SetActive(BluePrintDetailContent blueprint)
        {
            // Grab additional details from the id..or append model.
            SelectedBlueprintDetail = blueprint;
            Name.text = blueprint.Description.text;
            Description.text = blueprint.Description.text;

            SyncButton.onClick.RemoveAllListeners();
            PushButton.onClick.RemoveAllListeners();
            DeleteButton.onClick.RemoveAllListeners();

            SyncButton.onClick.AddListener(() =>
            {
                BlueprintGUI.SyncBlueprint(blueprint, TabType);
            });

            PushButton.onClick.AddListener(() =>
            {
                BlueprintGUI.PushBlueprint(blueprint, TabType);
            });

            DeleteButton.onClick.AddListener(() =>
            {
                BlueprintGUI.DeleteBlueprint(blueprint, TabType);
            });
        }

        public void Register(Transform tabTrans, GameObject uiBlueprintIconPrefab, TabsEnum tabType)
        {
            TabType = tabType;
            try
            {
                BlueprintIconPrefab = uiBlueprintIconPrefab;
                IconScrollContentParent = tabTrans.Find("IconScrollView");

                // Registering confirmation overlay.
                ConfirmationOverlay = new UIConfirmationOverlay();
                Transform overlayParent = tabTrans.Find("ConfirmationOverlay");
                ConfirmationOverlay.Register(overlayParent);

                Name = tabTrans.Find("Name").GetComponent<InputField>();
                Creator = tabTrans.Find("Creator").GetComponent<InputField>();
                Description = tabTrans.Find("Description").GetComponent<InputField>();

                SyncButton = tabTrans.Find("SyncButton").GetComponent<Button>();
                PushButton = tabTrans.Find("PushButton").GetComponent<Button>();
                DeleteButton = tabTrans.Find("DeleteButton").GetComponent<Button>();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug("Failed in BlueprintDetailDisplay");
            }
        }
    }

    public class BluePrintDetailContent
    {
        public GameObject ContentHolder { get; set; }
        public string Id { get; set; }
        // UI Elements.
        public Text Description { get; set; }
        public Image Icon { get; set; }
        // Use this as select button.
        public Button IconButton { get; set; }
        public Button SortUpButton { get; set; }
        public Button SortDownButton { get; set; }
    }

    public class UIConfirmationOverlay
    {
        public Transform ContentHolder { get; set; }
        public Text ConfirmationDisplayText { get; set; }
        public Button CancelButton { get; set; }
        public Button ConfirmButton { get; set; }

        public void Register(Transform overlayTransform)
        {
            ContentHolder = overlayTransform;
            ConfirmationDisplayText = overlayTransform.Find("ConfirmText").GetComponent<Text>();
            CancelButton = overlayTransform.Find("CancelButton").GetComponent<Button>();
            ConfirmButton = overlayTransform.Find("ConfirmationButton").GetComponent<Button>();
        }
    }
}