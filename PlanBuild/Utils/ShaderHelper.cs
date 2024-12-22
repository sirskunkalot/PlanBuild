﻿using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanBuild.Utils
{
    public class ShaderHelper
    {
        public enum ShaderState
        {
            Supported,
            Floating,
            Skuld
        }

        private static readonly Dictionary<string, Material> OriginalMaterialDict = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> SupportedMaterialDict = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> UnsupportedMaterialDict = new Dictionary<string, Material>();

        private static Shader _planShader;
        public static Shader PlanShader
        {
            get
            {
                return _planShader ??= PrefabManager.Cache.GetPrefab<Shader>("Lux Lit Particles/ Bumped");;
            }
        }

        public static void ClearCache()
        {
            UnsupportedMaterialDict.Clear();
            SupportedMaterialDict.Clear();
        }

        public static Texture2D GetTexture(Color color)
        {
            Texture2D texture2D = new Texture2D(1, 1);
            texture2D.SetPixel(0, 0, color);
            return texture2D;
        }

        public static Texture2D CreateScaledTexture(Texture2D texture, int width)
        {
            Texture2D copyTexture = new Texture2D(texture.width, texture.height, texture.format, false);
            copyTexture.SetPixels(texture.GetPixels());
            copyTexture.Apply();
            ScaleTexture(copyTexture, width);
            return copyTexture;
        }

        public static void ScaleTexture(Texture2D texture, int width)
        {
            Texture2D copyTexture = new Texture2D(texture.width, texture.height, texture.format, false);
            copyTexture.SetPixels(texture.GetPixels());
            copyTexture.Apply();

            int height = (int)Math.Round((float)width * texture.height / texture.width);
            texture.Reinitialize(width, height);
            texture.Apply();

            Color[] rpixels = texture.GetPixels(0);
            float incX = 1.0f / width;
            float incY = 1.0f / height;
            for (int px = 0; px < rpixels.Length; px++)
            {
                rpixels[px] = copyTexture.GetPixelBilinear(incX * ((float)px % width), incY * Mathf.Floor((float)px / width));
            }
            texture.SetPixels(rpixels, 0);
            texture.Apply();

            UnityEngine.Object.Destroy(copyTexture);

            /*for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var xp = 1f * x / width;
                    var yp = 1f * y / height;
                    var xo = (int)Mathf.Round(xp * copyTexture.width); // Other X pos
                    var yo = (int)Mathf.Round(yp * copyTexture.height); // Other Y pos
                    Color origPixel = copyTexture.GetPixel(xo, yo);
                    //origPixel.a = 1f;
                    texture.SetPixel(x, y, origPixel);
                }
            }
            texture.Apply();
            UnityEngine.Object.Destroy(copyTexture);*/
        }

        public static List<Renderer> GetRenderers(GameObject gameObject)
        {
            List<Renderer> result = new List<Renderer>();
            result.AddRange(gameObject.GetComponentsInChildren<MeshRenderer>());
            result.AddRange(gameObject.GetComponentsInChildren<SkinnedMeshRenderer>());
            return result;
        }

        public static void UpdateTextures(GameObject gameObject, ShaderState shaderState)
        {
            if (gameObject.TryGetComponent(out WearNTear wearNTear) && wearNTear.m_oldMaterials != null)
            {
                wearNTear.ResetHighlight();
            }

            foreach (Renderer renderer in GetRenderers(gameObject))
            {
                if (renderer.sharedMaterial != null)
                {
                    Material[] sharedMaterials = renderer.sharedMaterials;
                    UpdateMaterials(shaderState, sharedMaterials);

                    renderer.sharedMaterials = sharedMaterials;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        private static String AdjustMaterialName(String name)
        {
            return name.Split(' ')[0];
        }

        private static void UpdateMaterials(ShaderState shaderState, Material[] sharedMaterials)
        {
            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                Material originalMaterial = sharedMaterials[j];
                if (originalMaterial == null)
                {
                    continue;
                }
                String adjustedMaterialName = AdjustMaterialName(originalMaterial.name);
                if (!OriginalMaterialDict.ContainsKey(adjustedMaterialName))
                {
                    OriginalMaterialDict[adjustedMaterialName] = originalMaterial;
                }
                sharedMaterials[j] = GetMaterial(shaderState, originalMaterial);
            }
        }

        private static Material GetMaterial(ShaderState shaderState, Material originalMaterial)
        {
            float transparency = Config.TransparencyConfig.Value;
            transparency *= transparency; //x² mapping for finer control
            switch (shaderState)
            {
                case ShaderState.Skuld:
                    return OriginalMaterialDict[AdjustMaterialName(originalMaterial.name)];

                case ShaderState.Supported:
                    if (!SupportedMaterialDict.TryGetValue(originalMaterial.name, out Material supportedMaterial))
                    {
                        supportedMaterial = new Material(originalMaterial)
                        {
                            name = originalMaterial.name
                        };
                        supportedMaterial.SetOverrideTag("RenderType", "Transparent");
                        supportedMaterial.shader = PlanShader;
                        Color supportedMaterialColor = Config.SupportedPlanColorConfig.Value;
                        supportedMaterialColor.a *= transparency;
                        supportedMaterial.color = supportedMaterialColor;
                        supportedMaterial.EnableKeyword("_EMISSION");
                        supportedMaterial.DisableKeyword("DIRECTIONAL");
                        SupportedMaterialDict[originalMaterial.name] = supportedMaterial;
                    }
                    return supportedMaterial;

                case ShaderState.Floating:
                    if (!UnsupportedMaterialDict.TryGetValue(originalMaterial.name, out Material unsupportedMaterial))
                    {
                        unsupportedMaterial = new Material(originalMaterial)
                        {
                            name = originalMaterial.name
                        };
                        unsupportedMaterial.SetOverrideTag("RenderType", "Transparent");
                        unsupportedMaterial.shader = PlanShader;
                        Color unsupportedMaterialColor = Config.UnsupportedColorConfig.Value;
                        unsupportedMaterialColor.a *= transparency;
                        unsupportedMaterial.color = unsupportedMaterialColor;
                        unsupportedMaterial.EnableKeyword("_EMISSION");
                        unsupportedMaterial.DisableKeyword("DIRECTIONAL");
                        UnsupportedMaterialDict[originalMaterial.name] = unsupportedMaterial;
                    }
                    return unsupportedMaterial;

                default:
                    throw new ArgumentException("Unknown shaderState: " + shaderState);
            }
        }

        internal static void SetEmissionColor(GameObject gameObject, Color color)
        {
            foreach (Renderer renderer in GetRenderers(gameObject))
            {
                if (renderer.sharedMaterials.Length != 0)
                {
                    SetEmissionColor(renderer.materials, color);
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
                    colourArray[y * width + x] = bkgColor;
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
                        if (slice[x, y] && (!slice[x - 1, y] ||
                                            !slice[x + 1, y] ||
                                            !slice[x, y - 1] ||
                                            !slice[x, y + 1]))
                        {
                            // heightmap is read y,x from bottom left
                            // texture is read x,y from top left
                            // magic equation to find correct array index
                            int ind = (height - y - 1) * width + (width - x - 1);

                            colourArray[ind] = bandColor;
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