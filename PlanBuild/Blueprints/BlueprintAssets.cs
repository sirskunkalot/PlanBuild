using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Blueprints
{
    internal class BlueprintAssets
    {
        public const string BlueprintTooltipName = "BlueprintTooltip";
        public static GameObject BlueprintTooltip;

        public const string StandingBlueprintRuneName = "piece_world_standing_blueprint_rune";
        public const string BlueprintRuneStackName = "piece_world_blueprint_rune_stack";

        public const string BlueprintRuneName = "BlueprintRune";
        public const string BlueprintRuneItemName = "$item_blueprintrune";
        public const string PieceTableName = "_BlueprintPieceTable";
        public const string CategoryTools = "Tools";
        public const string CategoryBlueprints = "Blueprints";

        public const string PieceSnapPointName = "piece_bpsnappoint";
        public const string PieceCenterPointName = "piece_bpcenterpoint";
        public const string PieceCaptureName = "piece_bpcapture";
        public const string PieceEditName = "piece_bpedit";
        public const string PieceDeletePlansName = "piece_bpdelete";
        public const string PieceDeleteObjectsName = "piece_bpobjects";
        public const string PieceTerrainName = "piece_bpterrain";
        public const string PiecePaintName = "piece_bppaint";

        public const string PieceSelectAddName = "piece_bpselectadd";
        public const string PieceSelectRemoveName = "piece_bpselectremove";
        public const string PieceSelectSaveName = "piece_bpselectsave";
        
        public static void Load(AssetBundle assetBundle)
        {
            // Asset Bundle GameObjects
            GameObject[] prefabArray = assetBundle.LoadAllAssets<GameObject>();
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(prefabArray.Length);
            for (int i = 0; i < prefabArray.Length; ++i)
            {
                prefabs.Add(prefabArray[i].name, prefabArray[i]);
            }

            // Blueprint Tooltip
            BlueprintTooltip = prefabs[BlueprintTooltipName];
            void GUIManagerOnOnCustomGUIAvailable()
            {
                global::Utils.FindChild(BlueprintTooltip.transform, "Text").GetComponent<Text>().font =
                    GUIManager.Instance.AveriaSerif;
                GUIManager.OnCustomGUIAvailable -= GUIManagerOnOnCustomGUIAvailable;
            }
            GUIManager.OnCustomGUIAvailable += GUIManagerOnOnCustomGUIAvailable;

            // World Runes
            foreach (string pieceName in new string[]
            {
                StandingBlueprintRuneName, BlueprintRuneStackName
            })
            {
                CustomPiece piece = new CustomPiece(prefabs[pieceName], false, new PieceConfig
                {
                    PieceTable = "Hammer",
                    Requirements = new RequirementConfig[] {
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
                CustomCategories = new string[]
                {
                    CategoryTools, CategoryBlueprints
                }
            });
            PieceManager.Instance.AddPieceTable(table);

            // Blueprint Rune
            CustomItem item = new CustomItem(prefabs[BlueprintRuneName], false, new ItemConfig
            {
                Amount = 1,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            item.ItemDrop.m_itemData.m_shared.m_buildPieces = table.PieceTable;
            ItemManager.Instance.AddItem(item);

            // Stub Piece
            PrefabManager.Instance.AddPrefab(prefabs[Blueprint.PieceBlueprintName]);

            // Tool pieces
            foreach (string pieceName in new string[]
            {
                PieceCaptureName, PieceSelectAddName, PieceSelectRemoveName, PieceSelectSaveName,
                PieceSnapPointName, PieceCenterPointName, PieceEditName,
                PieceDeletePlansName, PieceTerrainName, PieceDeleteObjectsName,
                PiecePaintName
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

                    case PieceSelectSaveName:
                        prefabs[pieceName].AddComponent<SelectSaveComponent>();
                        break;
                        
                    case PieceEditName:
                        prefabs[pieceName].AddComponent<EditComponent>();
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
    }
}