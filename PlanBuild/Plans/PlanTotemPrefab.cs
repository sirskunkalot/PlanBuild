﻿using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Plans
{
    internal class PlanTotemPrefab
    {
        public const string PlanTotemPieceName = "piece_plan_totem";
        public static KitbashObject PlanTotemKitbash;

        public static void UpdateGlowColor(GameObject prefab)
        {
            if (!prefab)
            {
                return;
            }

            MeshRenderer meshRenderer = prefab.transform.Find("new/totem").GetComponent<MeshRenderer>();
            meshRenderer.materials
                .First(material => material.name.StartsWith("Guardstone_OdenGlow_mat"))
                .SetColor("_EmissionColor", Config.GlowColorConfig.Value);
        }

        public static void Create(AssetBundle planbuildBundle)
        {
            PlanTotemKitbash = KitbashManager.Instance.AddKitbash(planbuildBundle.LoadAsset<GameObject>(PlanTotemPieceName), new KitbashConfig
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
            PlanTotemKitbash.OnKitbashApplied += () =>
            {
                GameObject connectionPrefab = PrefabManager.Instance.GetPrefab("forge_ext1").GetComponent<StationExtension>().m_connectionPrefab;
                GameObject planBuildConnectionPrefab = PrefabManager.Instance.CreateClonedPrefab("vfx_PlanBuildConnection", connectionPrefab);

                GameObject planTotemPrefab = PlanTotemKitbash.Prefab;

                ShaderHelper.UpdateTextures(planTotemPrefab.transform.Find("new/pivot/hammer").gameObject, ShaderHelper.ShaderState.Supported);

                PlanTotem planTotem = planTotemPrefab.AddComponent<PlanTotem>();

                PlanTotem.m_connectionPrefab = planBuildConnectionPrefab;

                planTotem.m_name = "$piece_plan_totem_container";

                planTotem.m_open = planTotemPrefab.transform.Find("new/chest/privatechesttop_open").gameObject;
                planTotem.m_closed = planTotemPrefab.transform.Find("new/chest/privatechesttop_closed").gameObject;
                planTotem.m_height = 2;
                planTotem.m_width = 6;

                MeshRenderer meshRenderer = planTotemPrefab.transform.Find("new/totem").GetComponent<MeshRenderer>();
                meshRenderer.materials
                    .First(material => material.name.StartsWith("Guardstone_OdenGlow_mat"))
                    .SetColor("_EmissionColor", Config.GlowColorConfig.Value);

                CircleProjector circleProjector = planTotemPrefab.GetComponentInChildren<CircleProjector>(includeInactive: true);
                circleProjector.m_prefab = PrefabManager.Instance.GetPrefab("guard_stone").GetComponentInChildren<CircleProjector>().m_prefab;
                circleProjector.m_radius = Config.RadiusConfig.Value;
            };

            CustomPiece planTotemPiece = new CustomPiece(PlanTotemKitbash.Prefab, false, new PieceConfig()
            {
                PieceTable = "Hammer",
                Requirements = new []
                {
                    new RequirementConfig{ Item = "Wood", Amount = 1 ,Recover = true},
                    new RequirementConfig{ Item = "GreydwarfEye", Amount = 1, Recover = true}
                }
            });
            PieceManager.Instance.AddPiece(planTotemPiece);
        }
    }
}