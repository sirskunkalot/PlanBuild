using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintRunePrefab
    {
        public const string PieceTableName = "_BlueprintPieceTable";
        public const string CategoryTools = "Tools";
        public const string CategoryBlueprints = "Blueprints";

        public const string BlueprintRuneName = "BlueprintRune";

        public const string BlueprintSnapPointName = "piece_bpsnappoint";
        public const string BlueprintCenterPointName = "piece_bpcenterpoint";
        public const string BlueprintCaptureName = "piece_bpcapture";
        public const string BlueprintDeleteName = "piece_bpdelete";
        public const string BlueprintTerrainName = "piece_bpterrain";
        public const string StandingBlueprintRuneName = "piece_world_standing_blueprint_rune";
        public const string BlueprintRuneStackName = "piece_world_blueprint_rune_stack";

        public static string BlueprintRuneItemName;

        public BlueprintRunePrefab(AssetBundle assetBundle)
        {
            // Rune piece table
            CustomPieceTable table = new CustomPieceTable(PieceTableName, new PieceTableConfig
            {
                UseCategories = false,
                UseCustomCategories = true,
                CustomCategories = new string[]
                {
                    CategoryTools, CategoryBlueprints
                }
            });
            PieceManager.Instance.AddPieceTable(table);

            // Rune item
            GameObject runeprefab = assetBundle.LoadAsset<GameObject>(BlueprintRuneName);
            CustomItem item = new CustomItem(runeprefab, false, new ItemConfig
            {
                Amount = 1,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddItem(item);
            BlueprintRuneItemName = item.ItemDrop.m_itemData.m_shared.m_name;

            // Tool pieces
            CustomPiece piece;
            GameObject prefab;
            foreach (string pieceName in new string[]
            {
                BlueprintCaptureName, BlueprintSnapPointName, BlueprintCenterPointName,
                BlueprintDeleteName, BlueprintTerrainName
            })
            {
                prefab = assetBundle.LoadAsset<GameObject>(pieceName);
                piece = new CustomPiece(prefab, new PieceConfig
                {
                    PieceTable = PieceTableName,
                    Category = CategoryTools
                });
                piece.PiecePrefab.AddComponent<ToolPiece>();
                PieceManager.Instance.AddPiece(piece);
            }

            // World runes
            foreach (string pieceName in new string[]
            {
                StandingBlueprintRuneName, BlueprintRuneStackName
            })
            {
                prefab = assetBundle.LoadAsset<GameObject>(pieceName);
                piece = new CustomPiece(prefab, new PieceConfig
                {
                    PieceTable = "Hammer",
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig
                        {
                            Item = "Stone",
                            Amount = 5,
                            Recover= true
                        }
                    }
                });
                piece.PiecePrefab.AddComponent<WorldBlueprintRune>();
                piece.FixReference = true;
                PieceManager.Instance.AddPiece(piece);
            }

            // Blueprint stub
            GameObject placebp_prefab = assetBundle.LoadAsset<GameObject>(Blueprint.PieceBlueprintName);
            PrefabManager.Instance.AddPrefab(placebp_prefab);
        }
    }
}