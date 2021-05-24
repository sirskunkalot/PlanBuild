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
        private KitBashObject planTotemKitBash;

        public PlanTotemPrefab(AssetBundle planbuildBundle)
        {
            planTotemKitBash = KitBashManager.Instance.KitBash(planbuildBundle.LoadAsset<GameObject>("piece_plan_totem"), new KitBashConfig
            {
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
                        position = new Vector3(0.022f, 1.9f, 0f),
                        rotation = Quaternion.Euler(0f, 0f, 20f),
                        scale = Vector3.one * 0.3f
                    }
                }
            });
            planTotemKitBash.KitBashApplied += () =>
            {
                GameObject planTotemPrefab = planTotemKitBash.Prefab;

                PlanTotem planTotem = planTotemPrefab.AddComponent<PlanTotem>();
                planTotem.m_open = planTotemPrefab.transform.Find("new/chest/privatechesttop_open").gameObject;
                planTotem.m_closed = planTotemPrefab.transform.Find("new/chest/privatechesttop_closed").gameObject;
                planTotem.m_height = 2;
                planTotem.m_width = 6;
            };
            CustomPiece planTotemPiece = new CustomPiece(planTotemKitBash.Prefab, new PieceConfig()
            {
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig{ Item = "FineWood", Amount = 5 },
                    new RequirementConfig{ Item = "GreydwarfEye", Amount = 5},
                    new RequirementConfig{ Item = "SurtlingCore" },
                    new RequirementConfig{ Item = "Hammer" }
                }
            });
            PieceManager.Instance.AddPiece(planTotemPiece); 
        }

        public bool ApplyKitBash()
        {
            return planTotemKitBash.ApplyKitBash();
        }
    }
}
