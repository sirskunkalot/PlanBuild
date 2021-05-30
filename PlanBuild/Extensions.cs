using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static WearNTear;

namespace PlanBuild
{
    static class Extensions
    {

        public static void Highlight(this WearNTear self, Color color)
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
            foreach (OldMeshData oldMaterial in self.m_oldMaterials)
            {
                if ((bool)oldMaterial.m_renderer)
                {
                    Material[] materials = oldMaterial.m_renderer.materials;
                    foreach (Material obj in materials)
                    {
                        obj.SetColor("_EmissionColor", color * 0.4f);
                        obj.color = color;
                    }
                }
            }
            self.CancelInvoke("ResetHighlight");
            self.Invoke("ResetHighlight", 0.2f);
        }
         
    }
}
