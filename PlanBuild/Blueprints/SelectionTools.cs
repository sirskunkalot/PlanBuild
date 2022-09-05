using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class SelectionTools
    {
        public static void Copy(Selection selection, bool captureVanillaSnapPoints)
        {
            var bp = new Blueprint();
            bp.ID = $"__{BlueprintManager.TemporaryBlueprints.Count + 1:000}";
            bp.Creator = Player.m_localPlayer.GetPlayerName();
            bp.Name = bp.ID;
            bp.Category = BlueprintAssets.CategoryClipboard;
            if (!bp.Capture(selection, captureVanillaSnapPoints))
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bp.ID}");
                selection.Clear();
                return;
            }
            bp.CreatePiece();
            bp.CreateThumbnail(flush: false);
            BlueprintManager.TemporaryBlueprints.Add(bp.ID, bp);
            Player.m_localPlayer.UpdateKnownRecipesList();
            Player.m_localPlayer.UpdateAvailablePiecesList();
            int cat = (int)PieceManager.Instance.GetPieceCategory(BlueprintAssets.CategoryClipboard);
            List<Piece> reorder = Player.m_localPlayer.m_buildPieces.m_availablePieces[cat].OrderByDescending(x => x.name).ToList();
            Player.m_localPlayer.m_buildPieces.m_availablePieces[cat] = reorder;
            Player.m_localPlayer.m_buildPieces.m_selectedCategory = (Piece.PieceCategory)cat;
            Player.m_localPlayer.m_buildPieces.SetSelected(new Vector2Int(0, 0));
            Player.m_localPlayer.SetupPlacementGhost();
            BlueprintGUI.RefreshBlueprints(BlueprintLocation.Temporary);
        }

        public static void Cut(Selection selection, bool captureVanillaSnapPoints)
        {
            Copy(selection, captureVanillaSnapPoints);
            Delete(selection);
        }

        public static void Delete(Selection selection)
        {
            var ZDOs = new List<ZDO>();
            var toClear = selection.ToList();
            foreach (var zdoid in toClear)
            {
                var go = ZNetScene.instance.FindInstance(zdoid);
                if (go && go.TryGetComponent(out ZNetView zNetView))
                {
                    ZDOs.Add(zNetView.m_zdo);
                    zNetView.ClaimOwnership();
                    ZNetScene.instance.Destroy(go);
                }
            }

            var action = new UndoRemove(ZDOs);
            UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);
        }

        public static void Save(Selection selection, string name, string category, string description, bool captureVanillaSnapPoints)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var bp = new Blueprint();

            if (!bp.Capture(selection, captureVanillaSnapPoints))
            {
                return;
            }

            bp.ID = bp.CreateIDString();
            bp.Creator = Player.m_localPlayer.GetPlayerName();
            bp.Name = name;
            bp.Category = string.IsNullOrEmpty(category) ? BlueprintAssets.CategoryBlueprints : category;
            bp.Description = description;
            bp.FileLocation = Path.Combine(Config.BlueprintSaveDirectoryConfig.Value, bp.ID + ".blueprint");
            bp.ThumbnailLocation = bp.FileLocation.Replace(".blueprint", ".png");

            if (BlueprintManager.LocalBlueprints.TryGetValue(bp.ID, out var oldbp))
            {
                oldbp.DestroyBlueprint();
                BlueprintManager.LocalBlueprints.Remove(bp.ID);
            }

            if (!bp.ToFile())
            {
                return;
            }

            if (!bp.CreatePiece())
            {
                return;
            }

            BlueprintManager.LocalBlueprints.Add(bp.ID, bp);
            bp.CreateThumbnail();
            BlueprintManager.RegisterKnownBlueprints();
            BlueprintGUI.RefreshBlueprints(BlueprintLocation.Local);
        }

        public static void SaveWithGUI(Selection selection, bool captureVanillaSnapPoints)
        {
            var bpname = $"blueprint{BlueprintManager.LocalBlueprints.Count + 1:000}";
            SaveGUI.ShowSaveGUI(bpname, (name, category, description) =>
            {
                Save(selection, name, category, description, captureVanillaSnapPoints);
                selection.Clear();
            }, () =>
            {
                selection.Clear();
            });
        }

        private static class SaveGUI
        {
            private static Action<string, string, string> OkAction;
            private static Action CancelAction;
            private static GameObject Panel;
            private static InputField Name;
            private static InputField Category;
            private static InputField Description;

            public static void ShowSaveGUI(string bpname, Action<string, string, string> okAction, Action cancelAction)
            {
                OkAction = okAction;
                CancelAction = cancelAction;

                // Panel

                Panel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 0f),
                    width: 420f,
                    height: 500f,
                    draggable: false);
                Panel.SetActive(false);
                Panel.AddComponent<SaveGUIBehaviour>();

                var layout = Panel.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(15, 15, 15, 15);
                layout.spacing = 20f;
                
                // Name

                var name = new GameObject("Name");
                name.transform.SetParent(Panel.transform);
                name.AddComponent<LayoutElement>().preferredHeight = 70f;

                GUIManager.Instance.CreateText(
                    text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_name"),
                    parent: name.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, 0f),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 380f,
                    height: 40f,
                    addContentSizeFitter: false);

                var nameInput = GUIManager.Instance.CreateInputField(
                    parent: name.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 0f),
                    contentType: InputField.ContentType.Standard,
                    placeholderText: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_name_placeholder"),
                    fontSize: 20,
                    width: 380f,
                    height: 40f);

                Name = nameInput.GetComponent<InputField>();
                Name.text = bpname;

                // Category

                var cat = new GameObject("Category");
                cat.transform.SetParent(Panel.transform);
                cat.AddComponent<LayoutElement>().preferredHeight = 70f;

                GUIManager.Instance.CreateText(
                    text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_category"),
                    parent: cat.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, 0f),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 380f,
                    height: 40f,
                    addContentSizeFitter: false);

                var catInput = GUIManager.Instance.CreateInputField(
                    parent: cat.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 0f),
                    contentType: InputField.ContentType.Alphanumeric,
                    placeholderText: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_category_placeholder"),
                    fontSize: 20,
                    width: 380f,
                    height: 40f);

                Category = catInput.GetComponent<InputField>();
                Category.text = BlueprintAssets.CategoryBlueprints;

                // Description

                var desc = new GameObject("Description");
                desc.transform.SetParent(Panel.transform);
                desc.AddComponent<LayoutElement>().preferredHeight = 170f;

                GUIManager.Instance.CreateText(
                    text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_description"),
                    parent: desc.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, 0f),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 380f,
                    height: 40f,
                    addContentSizeFitter: false);

                var descInput = GUIManager.Instance.CreateInputField(
                    parent: desc.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 0f),
                    contentType: InputField.ContentType.Standard,
                    placeholderText: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_description_placeholder"),
                    fontSize: 20,
                    width: 380f,
                    height: 140f);

                Description = descInput.GetComponent<InputField>();
                Description.lineType = InputField.LineType.MultiLineNewline;

                // Buttons

                var buttons = new GameObject("Buttons");
                buttons.transform.SetParent(Panel.transform);
                buttons.AddComponent<LayoutElement>().preferredHeight = 50f;

                var cancelButton = GUIManager.Instance.CreateButton(
                    text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_cancel"),
                    parent: buttons.transform,
                    anchorMin: new Vector2(1f, 0.5f),
                    anchorMax: new Vector2(1f, 0.5f),
                    position: new Vector2(-100f, 0f),
                    width: 100f,
                    height: 40f);
                cancelButton.GetComponent<Button>().onClick.AddListener(OnCancel);

                var okButton = GUIManager.Instance.CreateButton(
                    text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_save"),
                    parent: buttons.transform,
                    anchorMin: new Vector2(0f, 0.5f),
                    anchorMax: new Vector2(0f, 0.5f),
                    position: new Vector2(100f, 0f),
                    width: 100f,
                    height: 40f);
                okButton.GetComponent<Button>().onClick.AddListener(OnOk);

                Panel.SetActive(true);
                Name.Select();
                GUIManager.BlockInput(true);
            }

            private static void OnOk()
            {
                OkAction.Invoke(Name.text.Trim(), Category.text.Trim(), Description.text.Trim());
                Panel.SetActive(false);
                Object.Destroy(Panel);
                GUIManager.BlockInput(false);
            }

            private static void OnCancel()
            {
                CancelAction.Invoke();
                Panel.SetActive(false);
                Object.Destroy(Panel);
                GUIManager.BlockInput(false);
            }

            private class SaveGUIBehaviour : MonoBehaviour
            {
                private void Update()
                {
                    if (Input.GetKeyUp(KeyCode.Return) && !Description.isFocused)
                    {
                        OnOk();
                    }
                    if (Input.GetKeyUp(KeyCode.Escape))
                    {
                        OnCancel();
                    }

                    // jees, what a horrible way to do that. need to implement generic code someday
                    if (Input.GetKeyDown(KeyCode.Tab) && Name.isFocused)
                    {
                        Category.Select();
                    }
                    if (Input.GetKeyDown(KeyCode.Tab) && Category.isFocused)
                    {
                        Description.Select();
                    }
                }
            }
        }
    }
}
