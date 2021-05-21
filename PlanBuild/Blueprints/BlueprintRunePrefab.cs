using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintRunePrefab
    {
        public BlueprintRunePrefab()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(PlanBuild.GetAssetPath("bundles/blueprints"));

            PieceManager.Instance.AddPieceTable(assetBundle.LoadAsset<GameObject>("_BlueprintPieceTable"));

            GameObject runeprefab = assetBundle.LoadAsset<GameObject>("BlueprintRune");
            CustomItem rune = new CustomItem(runeprefab, fixReference: false);
            ItemManager.Instance.AddItem(rune);

            CustomRecipe runeRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "BlueprintRune",
                Amount = 1,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddRecipe(runeRecipe);

            GameObject makebp_prefab = assetBundle.LoadAsset<GameObject>("make_blueprint");
            PrefabManager.Instance.AddPrefab(makebp_prefab);
            GameObject placebp_prefab = assetBundle.LoadAsset<GameObject>("piece_blueprint");
            PrefabManager.Instance.AddPrefab(placebp_prefab);

            TextAsset[] textAssets = assetBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }
            assetBundle.Unload(false);

        }
    }
}
