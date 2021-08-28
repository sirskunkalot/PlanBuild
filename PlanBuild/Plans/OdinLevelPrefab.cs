using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Plans
{
    internal class OdinLevelPrefab
    {
        public OdinLevelPrefab(AssetBundle planbuildBundle)
        {
            odinLevelPrefab = KitbashManager.Instance.AddKitbash(planbuildBundle.LoadAsset<GameObject>("piece_odin_level"), new KitbashConfig
            {
                Layer = "piece",
                KitbashSources = new List<KitbashSourceConfig>
                {
                    new KitbashSourceConfig
                    {
                        SourcePrefab = "SurtlingCore",
                        SourcePath = "attach/core",
                        TargetParentPath = "new",
                        Scale = Vector3.one * 0.1104584f
                    }
                }
            });
            odinLevelPrefab.OnKitbashApplied += InitLaserGrid;
            CustomPiece odinLevelPiece = new CustomPiece(odinLevelPrefab.Prefab, new PieceConfig
            {
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[]
             {
                 new RequirementConfig { Item = "SurtlingCore" , Recover = true }
             }
            });
            PieceManager.Instance.AddPiece(odinLevelPiece);
            odinLevelPiece.PiecePrefab.AddComponent<OdinLevel>();
        }

        private int radius = 10;
        private float distance = 1f;
        private KitbashObject odinLevelPrefab;

        private void InitLaserGrid()
        {
            Material defaultLine = Resources.FindObjectsOfTypeAll<Material>().First((Material k) => k.name == "Default-Line");
            int sections = 1 + (int)Math.Ceiling(((float)radius * 2) / distance);
            var laserGrid = new List<GameObject>();
            int i = 0;
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    Color color = (x == 0 && y == 0) ? Color.red : Color.gray;
                    laserGrid.Add(CreateLaser(i++, new Vector3(-2, x, y), new Vector3(2, x, y), defaultLine, color));
                    laserGrid.Add(CreateLaser(i++, new Vector3(x, -2, y), new Vector3(x, 2, y), defaultLine, color));
                    laserGrid.Add(CreateLaser(i++, new Vector3(x, y, -2), new Vector3(x, y, 2), defaultLine, color));
                }
            }
        }

        private GameObject CreateLaser(int i, Vector3 first, Vector3 second, Material material, Color color)
        {
            GameObject gameObject = new GameObject("laser_" + i, typeof(LineRenderer));
            LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = material;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.SetPositions(new Vector3[] { first, second });
            gameObject.transform.SetParent(odinLevelPrefab.Prefab.transform);
            return gameObject;
        }
    }
}