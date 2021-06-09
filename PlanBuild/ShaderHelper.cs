using BepInEx.Configuration;
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
                    UpdateMaterials(shaderState, unsupportedColor, supportedColor, transparency, sharedMaterials);

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
                    UpdateMaterials(shaderState, unsupportedColor, supportedColor, transparency, sharedMaterials);

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
                switch (shaderState)
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
            foreach (Material material in sharedMaterials)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color);
                }
            }
        }

        public static Texture2D FromHeightmap(Heightmap terrain)
        {
            return FromHeightmap(terrain, Color.gray, Color.black);
        }

        // define all parameters
        public static Texture2D FromHeightmap(Heightmap terrain, Color bandColor, Color bkgColor)
        {
            // dimensions
            int width = terrain.m_width;
            int height = terrain.m_width;

            // heightmap data

            // Create Output Texture2D with heightmap dimensions
            Texture2D topoMap = new Texture2D(width, height)
            {
                anisoLevel = 16
            };

            // array for storing colours to be applied to texture
            Color[] colourArray = new Color[width * height];

            // Set background
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    colourArray[(y * width) + x] = bkgColor;
                }
            }

            // Initial Min/Max values for normalized terrain heightmap values
            float minHeight = 1f;
            float maxHeight = 0;

            // Find lowest and highest points
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float terrainHeight = terrain.GetHeight(x, y);
                    if (minHeight > terrainHeight)
                    {
                        minHeight = terrainHeight;
                    }
                    if (maxHeight < terrainHeight)
                    {
                        maxHeight = terrainHeight;
                    }
                }
            }

            // Create height band list
            float bandDistance = 1f;

            List<float> bands = new List<float>();

            // Get ranges
            float r = minHeight + bandDistance;
            while (r < maxHeight)
            {
                bands.Add(r);
                r += bandDistance;
            }

            // Create slice buffer
            bool[,] slice = new bool[width, height];

            // Draw bands
            for (int b = 0; b < bands.Count; b++)
            {
                // Get Slice
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (terrain.GetHeight(x, y) >= bands[b])
                        {
                            slice[x, y] = true;
                        }
                        else
                        {
                            slice[x, y] = false;
                        }
                    }
                }

                // Detect edges on slice and write to output
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (slice[x, y] == true)
                        {
                            if (
                                slice[x - 1, y] == false ||
                                slice[x + 1, y] == false ||
                                slice[x, y - 1] == false ||
                                slice[x, y + 1] == false)
                            {
                                // heightmap is read y,x from bottom left
                                // texture is read x,y from top left
                                // magic equation to find correct array index
                                int ind = ((height - y - 1) * width) + (width - x - 1);

                                colourArray[ind] = bandColor;
                            }
                        }
                    }
                }
            }

            // apply colour array to texture
            topoMap.SetPixels(colourArray);
            topoMap.Apply();

            // Return result
            return topoMap;
        }
    }
}