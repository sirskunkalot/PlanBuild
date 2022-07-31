using System;
using Jotunn.Managers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Utils;
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
            selection.Clear();
            Player.m_localPlayer.UpdateKnownRecipesList();
            Player.m_localPlayer.UpdateAvailablePiecesList();
            int cat = (int)PieceManager.Instance.GetPieceCategory(BlueprintAssets.CategoryClipboard);
            List<Piece> reorder = Player.m_localPlayer.m_buildPieces.m_availablePieces[cat].OrderByDescending(x => x.name).ToList();
            Player.m_localPlayer.m_buildPieces.m_availablePieces[cat] = reorder;
            Player.m_localPlayer.m_buildPieces.m_selectedCategory = (Piece.PieceCategory)cat;
            Player.m_localPlayer.m_buildPieces.SetSelected(new Vector2Int(0, 0));
            Player.m_localPlayer.SetupPlacementGhost();
        }

        public static void Save(Selection selection)
        {
            var bp = new Blueprint();
            var bpname = $"blueprint{BlueprintManager.LocalBlueprints.Count + 1:000}";
            if (!bp.Capture(selection))
            {
                Jotunn.Logger.LogWarning($"Could not capture blueprint {bpname}");
                selection.Clear();
                return;
            }
            ShowSaveGUI(bpname, (name, category, description) =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return;
                }

                var playerName = Player.m_localPlayer.GetPlayerName();
                var fileName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
                var id = fileName.Replace(' ', '_').Trim();
                if (Config.AddPlayerNameConfig.Value)
                {
                    id = $"{playerName}_{id}";
                }

                bp.ID = id;
                bp.Creator = playerName;
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
                selection.Clear();
                bp.CreateThumbnail();
                BlueprintManager.RegisterKnownBlueprints();
                BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);
            });
        }
        
        public static void Delete(Selection selection)
        {
            var ZDOs = new List<ZDO>();
            var toClear = selection.ToList();
            selection.Clear();
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
            
            var action = new UndoActions.UndoRemove(ZDOs);
            UndoManager.Instance.Add(Config.BlueprintUndoQueueNameConfig.Value, action);
        }
        
        private static void ShowSaveGUI(string bpname, Action<string, string, string> okAction)
        {
            var panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f),
                width: 600,
                height: 500,
                draggable: false);
            panel.SetActive(false);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 20f;
            
            // Label

            var saveLabel = GUIManager.Instance.CreateText(
                text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_saveblueprint"),
                parent: panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 500f,
                height: 50f,
                addContentSizeFitter: false);
            saveLabel.GetComponent<Text>().alignment = TextAnchor.UpperCenter;
            saveLabel.AddComponent<LayoutElement>().preferredHeight = 80f;

            // Name

            var name = new GameObject("Name");
            name.transform.SetParent(panel.transform);
            name.AddComponent<LayoutElement>().preferredHeight = 70f;

            var nameLabel = GUIManager.Instance.CreateText(
                text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_name"),
                parent: name.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(100f, 0f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 150f,
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
                width: 400f,
                height: 40f);
            
            var nameValue = nameInput.GetComponent<InputField>();
            nameValue.text = bpname;

            // Category

            var cat = new GameObject("Category");
            cat.transform.SetParent(panel.transform);
            cat.AddComponent<LayoutElement>().preferredHeight = 70f;

            var catLabel = GUIManager.Instance.CreateText(
                text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_category"),
                parent: cat.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(100f, 0f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 150f,
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
                width: 400f,
                height: 40f);

            var catValue = catInput.GetComponent<InputField>();
            catValue.text = BlueprintAssets.CategoryBlueprints;
            
            // Description

            var desc = new GameObject("Description");
            desc.transform.SetParent(panel.transform);
            desc.AddComponent<LayoutElement>().preferredHeight = 150f;

            var descLabel = GUIManager.Instance.CreateText(
                text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_description"),
                parent: desc.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(100f, 0f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 150f,
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
                width: 400f,
                height: 120f);

            var descValue = descInput.GetComponent<InputField>();
            descValue.lineType = InputField.LineType.MultiLineNewline;

            // Buttons

            var buttons = new GameObject("Buttons");
            buttons.transform.SetParent(panel.transform);
            buttons.AddComponent<LayoutElement>().preferredHeight = 80f;

            var okButton = GUIManager.Instance.CreateButton(
                text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_confirm"),
                parent: buttons.transform,
                anchorMin: new Vector2(0f, 0.5f),
                anchorMax: new Vector2(0f, 0.5f),
                position: new Vector2(100f, 0f),
                width: 100f,
                height: 40f); 
            okButton.GetComponent<Button>().onClick.AddListener(() => OnClick(okAction));

            var cancelButton = GUIManager.Instance.CreateButton(
                text: LocalizationManager.Instance.TryTranslate("$gui_bpmarket_cancel"),
                parent: buttons.transform,
                anchorMin: new Vector2(1f, 0.5f),
                anchorMax: new Vector2(1f, 0.5f),
                position: new Vector2(-100f, 0f),
                width: 100f,
                height: 40f);
            cancelButton.GetComponent<Button>().onClick.AddListener(() => OnClick(null));

            void OnClick(Action<string, string, string> action)
            {
                action?.Invoke(nameValue.text.Trim(), catValue.text.Trim(), descValue.text.Trim());
                panel.SetActive(false);
                Object.Destroy(panel);
                GUIManager.BlockInput(false);
            }

            panel.SetActive(true);
            GUIManager.BlockInput(true);
        }
    }
}
