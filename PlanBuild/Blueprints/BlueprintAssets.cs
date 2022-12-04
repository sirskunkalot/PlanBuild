using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints.Components;
using PlanBuild.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class BlueprintAssets
    {
        public const string BlueprintTooltipName = "BlueprintTooltip";
        public static GameObject BlueprintTooltip;

        public const string BlueprintGUIName = "BlueprintGUI";
        public const string SelectionGUIName = "SelectionGUI";
        public const string SelectionSaveGUIName = "SelectionSaveGUI";
        public const string TerrainModGUIName = "TerrainModGUI";

        public const string StandingBlueprintRuneName = "piece_world_standing_blueprint_rune";
        public const string BlueprintRuneStackName = "piece_world_blueprint_rune_stack";

        public const string BlueprintRuneName = "BlueprintRune";
        public const string BlueprintRuneItemName = "$item_blueprintrune";
        public const string PieceTableName = "_BlueprintPieceTable";
        public const string CategoryTools = "Tools";
        public const string CategoryClipboard = "Clipboard";
        public const string CategoryBlueprints = "Blueprints";

        public const string PiecePlaceholderName = "piece_bpplaceholder";
        public static GameObject PlaceholderObject;

        public const string PieceSnapPointName = "piece_bpsnappoint";
        public const string PieceSnapPointInstanceName = "piece_bpsnappointinstance";
        public const string PieceCenterPointName = "piece_bpcenterpoint";
        public const string PieceCenterPointInstanceName = "piece_bpcenterpointinstance";
        public const string PieceTerrainModName = "piece_bpterrainmod";
        public const string PieceTerrainModInstanceName = "piece_bpterrainmodinstance";
        public const string PieceCaptureName = "piece_bpcapture";
        public const string PieceSelectAddName = "piece_bpselectadd";
        public const string PieceSelectRemoveName = "piece_bpselectremove";
        public const string PieceSelectEditName = "piece_bpselectedit";
        public const string PieceDeletePlansName = "piece_bpdelete";
        public const string PieceDeleteObjectsName = "piece_bpobjects";
        public const string PieceTerrainName = "piece_bpterrain";
        public const string PiecePaintName = "piece_bppaint";

        public static void Load(AssetBundle assetBundle)
        {
            // Asset Bundle GameObjects
            GameObject[] prefabArray = assetBundle.LoadAllAssets<GameObject>();
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(prefabArray.Length);
            for (int i = 0; i < prefabArray.Length; ++i)
            {
                prefabs.Add(prefabArray[i].name, prefabArray[i]);
            }

            // Blueprint GUI
            BlueprintGUI.Init(prefabs[BlueprintGUIName]);

            // SelectionGUI
            SelectionGUI.Init(prefabs[SelectionGUIName]);

            // SelectionSaveGUI
            SelectionSaveGUI.Init(prefabs[SelectionSaveGUIName]);

            // TerrainModGUI
            TerrainModGUI.Init(prefabs[TerrainModGUIName]);

            // Blueprint Tooltip
            BlueprintTooltip = prefabs[BlueprintTooltipName];
            void InitTooltipGUI()
            {
                global::Utils.FindChild(BlueprintTooltip.transform, "Text").GetComponent<Text>().font =
                    GUIManager.Instance.AveriaSerif;
                GUIManager.OnCustomGUIAvailable -= InitTooltipGUI;
            }
            GUIManager.OnCustomGUIAvailable += InitTooltipGUI;

            // Blueprint KeyHints
            GUIManager.OnCustomGUIAvailable += CreateCustomKeyHints;

            // World Runes
            foreach (string pieceName in new[]
            {
                StandingBlueprintRuneName, BlueprintRuneStackName
            })
            {
                CustomPiece piece = new CustomPiece(prefabs[pieceName], false, new PieceConfig
                {
                    PieceTable = "Hammer",
                    Requirements = new[] {
                        new RequirementConfig
                        {
                            Item = "Stone",
                            Amount = 5,
                            Recover = true
                        }
                    }
                });
                piece.PiecePrefab.AddComponent<WorldBlueprintRune>();
                piece.FixReference = true;
                PieceManager.Instance.AddPiece(piece);
            }

            // Rune PieceTable
            CustomPieceTable table = new CustomPieceTable(PieceTableName, new PieceTableConfig
            {
                CanRemovePieces = false,
                UseCategories = false,
                UseCustomCategories = true,
                CustomCategories = new[]
                {
                    CategoryTools, CategoryClipboard, CategoryBlueprints
                }
            });
            PieceManager.Instance.AddPieceTable(table);

            // Blueprint Rune
            CustomItem item = new CustomItem(prefabs[BlueprintRuneName], false, new ItemConfig
            {
                Amount = 1,
                Requirements = new[]
                {
                    new RequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            item.ItemDrop.m_itemData.m_shared.m_buildPieces = table.PieceTable;
            ItemManager.Instance.AddItem(item);

            // Stub Piece
            var stub = prefabs[Blueprint.PieceBlueprintName];
            PrefabManager.Instance.AddPrefab(stub);

            // Placeholder Piece
            ZNetView.m_forceDisableInit = true;
            PlaceholderObject = Object.Instantiate(stub);
            ZNetView.m_forceDisableInit = false;
            PlaceholderObject.name = PiecePlaceholderName;
            PrefabManager.Instance.AddPrefab(PlaceholderObject);
            var pieceplaceholder = PlaceholderObject.GetComponent<Piece>();
            pieceplaceholder.m_name = "$piece_bpplaceholder";
            pieceplaceholder.m_enabled = true;
            pieceplaceholder.m_description = "$piece_bpplaceholder_desc";
            pieceplaceholder.m_icon = Sprite.Create(new Texture2D(1, 1), Rect.zero, Vector2.zero);

            // Tool pieces
            foreach (string pieceName in new[]
            {
                PieceCaptureName, PieceSelectAddName, PieceSelectRemoveName,
                PieceSelectEditName, PieceSnapPointName, PieceCenterPointName,
                PieceTerrainModName, PieceDeletePlansName, PieceTerrainName,
                PieceDeleteObjectsName, PiecePaintName
            })
            {
                CustomPiece piece = new CustomPiece(prefabs[pieceName], false, new PieceConfig
                {
                    PieceTable = PieceTableName,
                    Category = CategoryTools
                });
                PieceManager.Instance.AddPiece(piece);

                // Add tool component per Tool
                switch (pieceName)
                {
                    case PieceCaptureName:
                        prefabs[pieceName].AddComponent<CaptureComponent>();
                        break;

                    case PieceSelectAddName:
                        prefabs[pieceName].AddComponent<SelectAddComponent>();
                        break;

                    case PieceSelectRemoveName:
                        prefabs[pieceName].AddComponent<SelectRemoveComponent>();
                        break;

                    case PieceSelectEditName:
                        prefabs[pieceName].AddComponent<SelectEditComponent>();
                        break;

                    case PieceSnapPointName:
                        var snap = prefabs[pieceName].AddComponent<MarkerComponent>();
                        snap.PieceInstanceName = PieceSnapPointInstanceName;
                        var snapfab = new CustomPrefab(prefabs[PieceSnapPointInstanceName], false);
                        PrefabManager.Instance.AddPrefab(snapfab);
                        break;

                    case PieceCenterPointName:
                        var center = prefabs[pieceName].AddComponent<MarkerComponent>();
                        center.PieceInstanceName = PieceCenterPointInstanceName;
                        var centerfab = new CustomPrefab(prefabs[PieceCenterPointInstanceName], false);
                        PrefabManager.Instance.AddPrefab(centerfab);
                        break;

                    case PieceTerrainModName:
                        var terrain = prefabs[pieceName].AddComponent<MarkerComponent>();
                        terrain.PieceInstanceName = PieceTerrainModInstanceName;
                        var terrainfab = new CustomPrefab(prefabs[PieceTerrainModInstanceName], false);
                        PrefabManager.Instance.AddPrefab(terrainfab);
                        prefabs[PieceTerrainModInstanceName].AddComponent<TerrainModMarker>();
                        prefabs[PieceTerrainModInstanceName].AddComponent<ShapedProjector>();
                        break;

                    case PieceDeletePlansName:
                        prefabs[pieceName].AddComponent<DeletePlansComponent>();
                        break;

                    case PieceDeleteObjectsName:
                        prefabs[pieceName].AddComponent<DeleteObjectsComponent>();
                        break;

                    case PieceTerrainName:
                        prefabs[pieceName].AddComponent<TerrainComponent>();
                        break;

                    case PiecePaintName:
                        prefabs[pieceName].AddComponent<PaintComponent>();
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        ///     Create custom KeyHints for the static Blueprint Rune pieces
        /// </summary>
        private static void CreateCustomKeyHints()
        {
            // Mode Switch

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "BuildMenu", HintToken = "$hud_buildmenu" }
                }
            });

            // Capture

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceCaptureName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcapture" },
                    new ButtonConfig { Name = Config.AltModifierButton.Name, Config = Config.AltModifierConfig, HintToken = "$hud_bpcapture_vanilla" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_bpcapture_highlight" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Add selection

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceSelectAddName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_add" },
                    new ButtonConfig { Name = Config.ToggleButton.Name, Config = Config.ToggleConfig, HintToken = "$hud_blueprint_select_add_switch" },
                    new ButtonConfig { Name = Config.AltModifierButton.Name, Config = Config.AltModifierConfig, HintToken = "$hud_blueprint_select_add_connected" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_blueprint_select_add_radius" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_blueprint_select_add_between" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" }
                }
            });

            // Remove selection

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceSelectRemoveName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_remove" },
                    new ButtonConfig { Name = Config.ToggleButton.Name, Config = Config.ToggleConfig, HintToken = "$hud_blueprint_select_remove_switch" },
                    new ButtonConfig { Name = Config.AltModifierButton.Name, Config = Config.AltModifierConfig, HintToken = "$hud_blueprint_select_remove_connected" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_blueprint_select_remove_radius" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_blueprint_select_clear" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" }
                }
            });

            // Edit selection

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceSelectEditName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_blueprint_select_edit" }
                }
            });

            // Snap Point

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceSnapPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpsnappoint" },
                    new ButtonConfig { Name = "Remove", HintToken = "$hud_bpremovemarker" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" }
                }
            });

            // Center point

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceCenterPointName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpterrainmod" },
                    new ButtonConfig { Name = "Remove", HintToken = "$hud_bpremovemarker" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" }
                }
            });

            // Terrain mod

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceTerrainModName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpcenterpoint" },
                    new ButtonConfig { Name = "Remove", HintToken = "$hud_bpremovemarker" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" }
                }
            });

            // Remove

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceDeletePlansName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpdelete" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_bpdelete_radius" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" }
                }
            });

            // Terrain

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceTerrainName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpterrain_flatten" },
                    new ButtonConfig { Name = Config.ToggleButton.Name, Config = Config.ToggleConfig, HintToken = "$hud_bpterrain_marker" },
                    new ButtonConfig { Name = Config.AltModifierButton.Name, Config = Config.AltModifierConfig, HintToken = "$hud_bpterrain_alt" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_bpterrain_ctrl" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Delete

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PieceDeleteObjectsName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpobjects_deleteveg" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_bpobjects_deletepieces" },
                    new ButtonConfig { Name = Config.AltModifierButton.Name, Config = Config.AltModifierConfig, HintToken = "$hud_bpobjects_deleteall" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            // Paint

            KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = BlueprintRuneName,
                Piece = PiecePaintName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bppaint_reset" },
                    new ButtonConfig { Name = Config.ToggleButton.Name, Config = Config.ToggleConfig, HintToken = "$hud_bppaint_marker" },
                    new ButtonConfig { Name = Config.AltModifierButton.Name, Config = Config.AltModifierConfig, HintToken = "$hud_bppaint_alt" },
                    new ButtonConfig { Name = Config.CtrlModifierButton.Name, Config = Config.CtrlModifierConfig, HintToken = "$hud_bppaint_ctrl" },
                    new ButtonConfig { Name = Config.ShiftModifierButton.Name, Config = Config.ShiftModifierConfig, HintToken = "$hud_bpcamera" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bpradius" }
                }
            });

            GUIManager.OnCustomGUIAvailable -= CreateCustomKeyHints;
        }
    }
}