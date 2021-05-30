using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanBuild
{
    public class ShaderHelper
    {
        public enum ShaderState
        { 
            Supported,
            Floating,
            Skuld
        }

        private static readonly Dictionary<string, Material> originalMaterialDict = new Dictionary<string, Material>(); 
        public static Shader planShader;
        public static ConfigEntry<Color> unsupportedColorConfig;
        public static ConfigEntry<Color> supportedPlanColorConfig;
        internal static ConfigEntry<float> transparencyConfig; 
          
        public static Texture2D GetTexture(Color color)
        {
            Texture2D texture2D = new Texture2D(1, 1);
            texture2D.SetPixel(0, 0, color);
            return texture2D;
        }

        public static void UpdateTextures(GameObject m_placementplan, ShaderState shaderState)
        {
            
            Color unsupportedColor = unsupportedColorConfig.Value;
            Color supportedColor = supportedPlanColorConfig.Value; 
            float transparency = transparencyConfig.Value;  
            transparency *= transparency; //x² mapping for finer control
            MeshRenderer[] meshRenderers = m_placementplan.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (!(meshRenderer.sharedMaterial == null))
                {
                    Material[] sharedMaterials = meshRenderer.sharedMaterials;
                    UpdateMaterials(shaderState,  unsupportedColor, supportedColor, transparency, sharedMaterials);

                    meshRenderer.sharedMaterials = sharedMaterials;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = m_placementplan.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer meshRenderer in skinnedMeshRenderers)
            {
                if (!(meshRenderer.sharedMaterial == null))
                {
                    Material[] sharedMaterials = meshRenderer.sharedMaterials;
                    UpdateMaterials(shaderState,  unsupportedColor, supportedColor, transparency, sharedMaterials);

                    meshRenderer.sharedMaterials = sharedMaterials;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        private static void UpdateMaterials(ShaderState shaderState, Color planColor, Color supportedPlanColor, float transparency, Material[] sharedMaterials)
        { 
            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                Material originalMaterial = sharedMaterials[j];
                Material material = new Material(originalMaterial)
                {
                    name = originalMaterial.name
                };
                if (!originalMaterialDict.ContainsKey(material.name))
                {
                    originalMaterialDict[material.name] = originalMaterial;
                } 
                switch(shaderState)
                {
                    case ShaderState.Skuld:
                        material = originalMaterialDict[originalMaterial.name];
                        break;
                    default:
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.shader = planShader;
                        Color color = (shaderState == ShaderState.Supported ? supportedPlanColor : planColor);
                        color.a *= transparency;
                        material.color = color;
                        material.EnableKeyword("_EMISSION");
                        material.DisableKeyword("DIRECTIONAL");
                        break;
                }
                sharedMaterials[j] = material;

            }
        }

        internal static void SetEmissionColor(GameObject gameObject, Color color)
        {
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (!(meshRenderer.sharedMaterial == null))
                {
                    SetEmissionColor(meshRenderer.sharedMaterials, color);
                }
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer meshRenderer in skinnedMeshRenderers)
            {
                if (!(meshRenderer.sharedMaterial == null))
                {
                    SetEmissionColor(meshRenderer.sharedMaterials, color);
                }
            }
        }

        private static void SetEmissionColor(Material[] sharedMaterials, Color color)
        {
            foreach(Material material in sharedMaterials)
            {
                if(material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color);
                }
            }
        }
    }
}
