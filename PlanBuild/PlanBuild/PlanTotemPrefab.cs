using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.PlanBuild;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Plans
{
    internal class PlanTotemPrefab
    {
        public const string PlanTotemPieceName = "piece_plan_totem";
        public static ConfigEntry<Color> glowColorConfig;
        private KitbashObject planTotemKitbash;

        public PlanTotemPrefab(AssetBundle planbuildBundle)
        {
            planTotemKitbash = KitbashManager.Instance.AddKitbash(planbuildBundle.LoadAsset<GameObject>(PlanTotemPieceName), new KitbashConfig
            {
                FixReferences = true,
                KitbashSources = new List<KitbashSourceConfig> {
                    new KitbashSourceConfig
                    {
                        Name = "totem",
                        TargetParentPath = "new",
                        SourcePrefab = "guard_stone",
                        SourcePath = "new/default",
                        Scale = Vector3.one * 0.6f
                    },
                    new KitbashSourceConfig
                    {
                        Name= "chest",
                        TargetParentPath = "new",
                        SourcePrefab = "piece_chest_private",
                        SourcePath = "New",
                        Position = new Vector3(0,0, 0.591f),
                        Scale = Vector3.one * 0.9f,
                        Rotation = Quaternion.Euler(180f, 180f, 180f)
                    },
                    new KitbashSourceConfig
                    {
                        Name = "hammer",
                        TargetParentPath = "new/pivot",
                        SourcePrefab= "Hammer",
                        SourcePath = "attach/hammer",
                        Position = new Vector3(0.07f, 1.9f, 0f),
                        Rotation = Quaternion.Euler(0f, 0f, 20f),
                        Scale = Vector3.one * 0.3f
                    }
                }
            });
            planTotemKitbash.OnKitbashApplied += () =>
            {
                GameObject connectionPrefab = PrefabManager.Instance.GetPrefab("forge_ext1").GetComponent<StationExtension>().m_connectionPrefab;
                GameObject planBuildConnectionPrefab = PrefabManager.Instance.CreateClonedPrefab("vfx_PlanBuildConnection", connectionPrefab);

                GameObject planTotemPrefab = planTotemKitbash.Prefab;

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

            CustomPiece planTotemPiece = new CustomPiece(planTotemKitbash.Prefab, new PieceConfig()
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

        internal void SettingsUpdated()
        {
            UpdateGlowColor(planTotemKitbash.Prefab);
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