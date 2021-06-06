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
        public const string BlueprintRuneName = "BlueprintRune";
        public const string BlueprintSnapPointName = "piece_blueprint_snappoint"; 
        public const string BlueprintCenterPointName = "piece_blueprint_centerpoint";
        public const string BlueprintStandingBlueprintRune = "piece_standing_blueprint_rune";
        public const string MakeBlueprintName = "make_blueprint";
        public const string UndoBlueprintName = "undo_blueprint";
        public const string DeletePlansName = "delete_plans";
        public static string BlueprintRuneItemName;
        public GameObject runeprefab;
         
        public BlueprintRunePrefab(AssetBundle assetBundle)
        {
            PieceManager.Instance.AddPieceTable(assetBundle.LoadAsset<GameObject>(PieceTableName));

            runeprefab = assetBundle.LoadAsset<GameObject>(BlueprintRuneName);
            CustomItem rune = new CustomItem(runeprefab, fixReference: false); 
            ItemManager.Instance.AddItem(rune); 
            BlueprintRuneItemName = rune.ItemDrop.m_itemData.m_shared.m_name;
            rune.ItemDrop.m_itemData.m_shared.m_buildPieces = PieceManager.Instance.GetPieceTable(PlanPiecePrefab.PlanHammerPieceTableName);
            CustomRecipe runeRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = BlueprintRuneName,
                Amount = 1,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddRecipe(runeRecipe);

            GameObject makebp_prefab = assetBundle.LoadAsset<GameObject>(MakeBlueprintName);
            PrefabManager.Instance.AddPrefab(makebp_prefab);
            GameObject placebp_prefab = assetBundle.LoadAsset<GameObject>(Blueprint.BlueprintPrefabName);
            PrefabManager.Instance.AddPrefab(placebp_prefab); 
            PrefabManager.Instance.AddPrefab(assetBundle.LoadAsset<GameObject>(BlueprintSnapPointName)); 
            PrefabManager.Instance.AddPrefab(assetBundle.LoadAsset<GameObject>(BlueprintCenterPointName));
            PrefabManager.Instance.AddPrefab(assetBundle.LoadAsset<GameObject>(UndoBlueprintName));
            PrefabManager.Instance.AddPrefab(assetBundle.LoadAsset<GameObject>(DeletePlansName));
            CustomPiece blueprintRunestone = new CustomPiece(assetBundle.LoadAsset<GameObject>(BlueprintStandingBlueprintRune), new PieceConfig
            {
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[] {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 10,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "SurtlingCore",
                        Recover = true
                    }
                }
            });
            PieceManager.Instance.AddPiece(blueprintRunestone);
            blueprintRunestone.PiecePrefab.AddComponent<WorldBlueprintManager>();

            PrefabManager.Instance.AddPrefab(assetBundle.LoadAsset<GameObject>(BlueprintManager.PanelName));
        }
    }
}
