using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.KitBash;
using PlanBuild.PlanBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine; 

namespace PlanBuild.Plans
{
    class PlanTotemPrefab
    {
        internal static ConfigEntry<Color> glowColorConfig;
        private KitBashObject planTotemKitBash;

        public PlanTotemPrefab(AssetBundle planbuildBundle)
        {
            planTotemKitBash = KitBashManager.Instance.KitBash(planbuildBundle.LoadAsset<GameObject>("piece_plan_totem"), new KitBashConfig
            {
                FixReferences = true,
                KitBashSources = new List<KitBashSourceConfig> {
                    new KitBashSourceConfig
                    {
                        name = "totem",
                        targetParentPath = "new",
                        sourcePrefab = "guard_stone",
                        sourcePath = "new/default",
                        scale = Vector3.one * 0.6f
                    },
                    new KitBashSourceConfig
                    {
                        name= "chest",
                        targetParentPath = "new",
                        sourcePrefab = "piece_chest_private",
                        sourcePath = "New",
                        position = new Vector3(0,0, 0.591f),
                        scale = Vector3.one * 0.9f,
                        rotation = Quaternion.Euler(180f, 180f, 180f)
                    },
                    new KitBashSourceConfig
                    {
                        name = "hammer",
                        targetParentPath = "new/pivot",
                        sourcePrefab= "Hammer",
                        sourcePath = "attach/hammer",
                        position = new Vector3(0.07f, 1.9f, 0f),
                        rotation = Quaternion.Euler(0f, 0f, 20f),
                        scale = Vector3.one * 0.3f
                    }
                }
            });
            planTotemKitBash.KitBashApplied += () =>
            {
                GameObject connectionPrefab = PrefabManager.Instance.GetPrefab("forge_ext1").GetComponent<StationExtension>().m_connectionPrefab; 
                GameObject planBuildConnectionPrefab = PrefabManager.Instance.CreateClonedPrefab("vfx_PlanBuildConnection", connectionPrefab);
              
                GameObject planTotemPrefab = planTotemKitBash.Prefab;

                ShaderHelper.UpdateTextures(planTotemPrefab.transform.Find("new/pivot/hammer").gameObject, ShaderHelper.ShaderState.Supported);

                PlanTotem planTotem = planTotemPrefab.AddComponent<PlanTotem>();

                PlanTotem.m_connectionPrefab = planBuildConnectionPrefab;

                planTotem.m_open = planTotemPrefab.transform.Find("new/chest/privatechesttop_open").gameObject;
                planTotem.m_closed = planTotemPrefab.transform.Find("new/chest/privatechesttop_closed").gameObject;
                planTotem.m_height = 2;
                planTotem.m_width = 6;

                MeshRenderer meshRenderer = planTotemPrefab.transform.Find("new/totem").GetComponent<MeshRenderer>();
                meshRenderer.materials
                    .Where(material => material.name.StartsWith("Guardstone_OdenGlow_mat"))
                    .First()
                    .SetColor("_EmissionColor", glowColorConfig.Value);

                CircleProjector circleProjector = planTotemPrefab.GetComponentInChildren<CircleProjector>(includeInactive: true);
                circleProjector.m_prefab = PrefabManager.Instance.GetPrefab("guard_stone").GetComponentInChildren<CircleProjector>().m_prefab;
                circleProjector.m_radius = PlanTotem.radiusConfig.Value;
            };

            CustomPiece planTotemPiece = new CustomPiece(planTotemKitBash.Prefab, new PieceConfig()
            {
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig{ Item = "FineWood", Amount = 5 ,Recover = true},
                    new RequirementConfig{ Item = "GreydwarfEye", Amount = 5, Recover = true},
                    new RequirementConfig{ Item = "SurtlingCore", Recover = true }
                }
            });
            PieceManager.Instance.AddPiece(planTotemPiece); 
        }

        public bool ApplyKitBash()
        {
            return planTotemKitBash.ApplyKitBash();
        }

        internal void SettingsUpdated()
        { 
            UpdateGlowColor(planTotemKitBash.Prefab);
        }

        public static void UpdateGlowColor(GameObject prefab)
        {
            MeshRenderer meshRenderer = prefab.transform.Find("new/totem").GetComponent<MeshRenderer>();
            meshRenderer.materials
                .Where(material => material.name.StartsWith("Guardstone_OdenGlow_mat"))
                .First()
                .SetColor("_EmissionColor", glowColorConfig.Value);
        }
    }
}
