﻿using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal class BlueprintRunePrefab
    {
        public const string PieceTableName = "_BlueprintPieceTable";
        public const string BlueprintRuneName = "BlueprintRune";
        public const string MakeBlueprintName = "make_blueprint";
        public GameObject runeprefab;
        

        public BlueprintRunePrefab(AssetBundle assetBundle)
        {
            PieceManager.Instance.AddPieceTable(assetBundle.LoadAsset<GameObject>(PieceTableName));

            runeprefab = assetBundle.LoadAsset<GameObject>(BlueprintRuneName);
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

            GameObject makebp_prefab = assetBundle.LoadAsset<GameObject>(MakeBlueprintName);
            PrefabManager.Instance.AddPrefab(makebp_prefab);
            GameObject placebp_prefab = assetBundle.LoadAsset<GameObject>(Blueprint.BlueprintPrefabName);
            PrefabManager.Instance.AddPrefab(placebp_prefab);

            TextAsset[] textAssets = assetBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            } 
        }
    }
}
