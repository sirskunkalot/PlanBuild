using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WearNTear;

namespace PlanBuild
{
    internal static class Extensions
    {
        public static void Highlight(this WearNTear self, Color color, float highlightTime = 0)
        {
            if (self.m_oldMaterials == null)
            {
                self.m_oldMaterials = new List<OldMeshData>();

                foreach (Renderer highlightRenderer in self.GetHighlightRenderers())
                {
                    OldMeshData item = default;
                    item.m_materials = highlightRenderer.sharedMaterials;
                    item.m_color = new Color[item.m_materials.Length];
                    item.m_emissiveColor = new Color[item.m_materials.Length];

                    for (int i = 0; i < item.m_materials.Length; i++)
                    {
                        if (item.m_materials[i].HasProperty("_Color"))
                        {
                            item.m_color[i] = item.m_materials[i].GetColor("_Color");
                        }
                        if (item.m_materials[i].HasProperty("_EmissionColor"))
                        {
                            item.m_emissiveColor[i] = item.m_materials[i].GetColor("_EmissionColor");
                        }
                    }

                    item.m_renderer = highlightRenderer;
                    self.m_oldMaterials.Add(item);
                }
            }

            IEnumerable<OldMeshData> oldMaterialsWithRenderer =
                self.m_oldMaterials.Where(mat => (bool)mat.m_renderer);

            foreach (OldMeshData oldMaterial in oldMaterialsWithRenderer)
            {
                Material[] materials = oldMaterial.m_renderer.materials;
                var colored_materials = materials.Where(material =>
                    material.HasProperty("_EmissionColor")
                    && material.HasProperty("_Color")
                );

                foreach (Material material in colored_materials)
                {
                    material.SetColor("_EmissionColor", color * 0.3f);
                    material.color = color;
                }
            }

            self.CancelInvoke("ResetHighlight");
            if (highlightTime > 0)
            {
                self.Invoke("ResetHighlight", highlightTime);
            }
        }

        public static void ResetHighlight(this WearNTear self)
        {
            if (self.m_oldMaterials == null)
            {
                self.ResetHighlight();
                return;
            }

            IEnumerable<OldMeshData> oldMaterialsWithRenderer =
                self.m_oldMaterials.Where(mat => (bool)mat.m_renderer);

            foreach (OldMeshData oldMaterial in oldMaterialsWithRenderer)
            {
                Material[] materials = oldMaterial.m_renderer.materials;

                var materials_with_color_info = materials.Select((mat, idx) => new
                    {
                        Material = mat,
                        OriginalColor = oldMaterial.m_color[idx],
                        OriginalEmissionColor = oldMaterial.m_emissiveColor[idx]
                    }
                );

                foreach (var material in materials_with_color_info)
                {
                    material.Material.SetColor("_EmissionColor", material.OriginalEmissionColor);
                    material.Material.color = material.OriginalColor;
                }
            }

            self.m_oldMaterials = null;
            self.ResetHighlight();
        }

        public static ZDOID? GetZDOID(this Piece piece)
        {
            return piece?.m_nview?.GetZDO()?.m_uid;
        }

        public static ZDOID? GetZDOID(this WearNTear wearNTear)
        {
            return wearNTear?.m_nview?.GetZDO()?.m_uid;
        }
    }
}