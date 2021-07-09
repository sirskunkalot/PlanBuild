using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Plans;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintRunePrefab
    {
        public const string PieceTableName = "_BlueprintPieceTable";
        public const string CategoryTools = "Tools";
        public const string CategoryBlueprints = "Blueprints";

        public const string BlueprintRuneName = "BlueprintRune";

        public const string BlueprintSnapPointName = "piece_blueprint_snappoint";
        public const string BlueprintCenterPointName = "piece_blueprint_centerpoint";
        public const string StandingBlueprintRune = "piece_world_standing_blueprint_rune";
        public const string BlueprintRuneStack = "piece_world_blueprint_rune_stack";
        public const string MakeBlueprintName = "make_blueprint";
        public const string DeletePlansName = "delete_plans";

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
                MakeBlueprintName, BlueprintSnapPointName, BlueprintCenterPointName,
                DeletePlansName
            })
            {
                prefab = assetBundle.LoadAsset<GameObject>(pieceName);
                piece = new CustomPiece(prefab, new PieceConfig
                {
                    PieceTable = PieceTableName,
                    Category = CategoryTools
                });
                PieceManager.Instance.AddPiece(piece);
            }
            // World runes
            foreach (string pieceName in new string[]
            {
                StandingBlueprintRune, BlueprintRuneStack
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