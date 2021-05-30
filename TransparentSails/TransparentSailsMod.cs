// TransparentSails
// a Valheim mod skeleton using Jötunn
// 
// File:    TransparentSails.cs
// Project: TransparentSails

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TransparentSails
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("BepIn.Sarcen.ValheimRAFT", BepInDependency.DependencyFlags.SoftDependency)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class TransparentSailsMod : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.TransparentSails";
        public const string PluginName = "Transparent Sails";
        public const string PluginVersion = "1.1.0";
        public ConfigEntry<int> nexusID;
        private static ManualLogSource logger;

        private const string hideButton = "TransparentSail_Toggle";

        public static ConfigEntry<When> configWhen;
        public static ConfigEntry<string> configToggleHotKey;
        public static ConfigEntry<bool> configToggleDefault;
        public static ConfigEntry<float> configShaderTransparency;
        public static bool hotkeyToggle;

        public static ConfigEntry<int> configDitheringModulo;
        public static ConfigEntry<int> configDitheringX;
        public static ConfigEntry<int> configDitheringY;

        private static readonly Dictionary<int, bool> wasTransparentDict = new Dictionary<int, bool>();

        public static HashSet<string> textureNames = new HashSet<string>(new string[] { "sail_hide", "sail_white", "sail_diffuse" });

        public static Dictionary<string, Texture2D> originalTextures = new Dictionary<string, Texture2D>(textureNames.Count);
        public static Dictionary<string, Texture2D> transparentTextures = new Dictionary<string, Texture2D>(textureNames.Count);

        private readonly Harmony harmony = new Harmony(PluginGUID);

        public enum When
        {
            Steering,
            CurrentBoat,
            AllBoats
        }

        public void Awake()
        {
            logger = Logger;
            if (Chainloader.PluginInfos.ContainsKey("BepIn.Sarcen.ValheimRAFT"))
            {
                harmony.PatchAll(typeof(ValheimRAFT_Patch));
            }
            nexusID = Config.Bind<int>("General", "NexusID", 924, new ConfigDescription("Nexus mod ID for updates", new AcceptableValueList<int>(new int[] { 924 })));


            configWhen = Config.Bind<When>("General", "When should the sails be transparent", When.CurrentBoat, new ConfigDescription("When should the sails be transparent?"));
            configToggleHotKey = Config.Bind<string>("General", "Toggle Hotkey", "H", new ConfigDescription("Hotkey to toggle transparency", new AcceptableValueList<string>(GetAcceptableKeyCodes())));
            configToggleDefault = Config.Bind<bool>("General", "Toggle default state", true, new ConfigDescription("Default state of the hide toggle when starting Valheim"));

            configDitheringModulo = Config.Bind<int>("Dithering mask", "Modulo", 2, new ConfigDescription("Sail Transparancy Dithering Modulo"));
            configDitheringX = Config.Bind<int>("Dithering mask", "X", 1, new ConfigDescription("Sail Transparancy Dithering X"));
            configDitheringY = Config.Bind<int>("Dithering mask", "Y", 1, new ConfigDescription("Sail Transparancy Dithering Y"));

            configShaderTransparency = Config.Bind<float>("Transparency", "Shader transparency", 0.7f, new ConfigDescription("Additional shader transparency, fades the pixels", new AcceptableValueRange<float>(0f, 1f)));

            hotkeyToggle = configToggleDefault.Value;

            configDitheringModulo.SettingChanged += OnSettingChanged;
            configDitheringX.SettingChanged += OnSettingChanged;
            configDitheringY.SettingChanged += OnSettingChanged;
            configWhen.SettingChanged += OnSettingChanged;
            configShaderTransparency.SettingChanged += OnSettingChanged;

            configToggleHotKey.SettingChanged += OnHotKeyChanged;

            PrefabManager.OnPrefabsRegistered += OnPrefabsLoaded;
            RegisterInputs();

            harmony.PatchAll(typeof(Ship_Awake_Patch));
            harmony.PatchAll(typeof(Ship_UpdateSailSize_Patch));
        }

        private string[] GetAcceptableKeyCodes()
        {
            Array keyCodes = Enum.GetValues(typeof(KeyCode));
            int i = 0;
            string[] acceptable = new string[keyCodes.Length];
            foreach (System.Object keyCode in keyCodes)
            {
                acceptable[i++] = keyCode.ToString();
            }
            return acceptable;
        }

        private bool CheckInput()
        {
            return (!Chat.instance || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !InventoryGui.IsVisible()
                && !StoreGui.IsVisible()
                && !Menu.IsVisible()
                && !Minimap.IsOpen()
                && Player.m_localPlayer != null
                && !Player.m_localPlayer.InCutscene();
        }

        public void Update()
        {
            // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
            // we need to check that ZInput is ready to use first.
            if (ZInput.instance == null)
            {
                return;
            }

            if (!CheckInput())
            {
                return;
            }

            if (ZInput.GetButtonDown(hideButton))
            {
                hotkeyToggle = !hotkeyToggle;
                logger.LogInfo("Toggle TransparentSails is " + (hotkeyToggle ? "on" : "off"));
            }
        }

        private void OnHotKeyChanged(object sender, EventArgs e)
        {
            RegisterInputs();
        }

        private void RegisterInputs()
        {
            logger.LogDebug("Registering TransparentSails input: " + configToggleHotKey.Value);
            InputManager.Instance.AddButton(PluginGUID, new Jotunn.Configs.ButtonConfig()
            {
                Name = hideButton,
                Key = (KeyCode)Enum.Parse(typeof(KeyCode), configToggleHotKey.Value)
            });

        }

        private void OnPrefabsLoaded()
        {
            SaveOriginalTexture(PrefabManager.Instance.GetPrefab("Raft").GetComponentInChildren<Ship>());
            SaveOriginalTexture(PrefabManager.Instance.GetPrefab("Karve").GetComponentInChildren<Ship>());
            SaveOriginalTexture(PrefabManager.Instance.GetPrefab("VikingShip").GetComponentInChildren<Ship>());
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            if (configDitheringModulo.Value == 0)
            {
                logger.LogWarning("Modulo 0 is invalid!");
            }
            transparentTextures.Clear();
            wasTransparentDict.Clear();
        }

        private static Texture2D ReadableTexture(Texture2D texture)
        {

            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                                texture.width,
                                texture.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Default);


            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

            Graphics.Blit(texture, tmp);

            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            Sprite.Create(texture, new Rect(), Vector2.zero);

            myTexture2D.name = texture.name;

            return myTexture2D;
        }

        private static Texture2D CreateTransparentTexture(Texture2D texture, int dithering, int ditheringXOffset, int ditheringYOffset)
        {
            if (dithering == 0)
            {
                return texture;
            }


            float transparency = configShaderTransparency.Value;

            Texture2D transparentTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

            int i = 0;

            for (int x = 0; x < texture.width; x++)
            {
                i += ditheringXOffset;
                for (int y = 0; y < texture.height; y++)
                {
                    i += ditheringYOffset;
                    Color originalColor = texture.GetPixel(x, y);

                    float a;
                    {
                        if (i % dithering > 0)
                        {
                            a = 0f;
                        }
                        else
                        {
                            a = originalColor.a * transparency;
                        }

                    }
                    transparentTexture.SetPixel(x, y, new Color(originalColor.r, originalColor.g, originalColor.b, a));
                }
            }
            transparentTexture.Apply();
            transparentTexture.name = texture.name;
            return transparentTexture;
        }


        private static void SaveOriginalTexture(Ship ship)
        {
            GameObject sailObject = ship.m_sailObject;

            SkinnedMeshRenderer meshRenderer = sailObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (meshRenderer)
            {
                foreach (Material material in meshRenderer.materials)
                {
                    Texture2D texture = material.mainTexture as Texture2D;
                    if (textureNames.Contains(texture.name))
                    {
                        originalTextures[texture.name] = texture;
                        return;
                    }

                }
            }
        }

        public static void UpdateSail(int instanceId, bool shouldBeTransparent, GameObject sailObject)
        {
            if (wasTransparentDict.TryGetValue(instanceId, out bool isTransparent) && isTransparent == shouldBeTransparent)
            {
                return;
            }
            wasTransparentDict[instanceId] = shouldBeTransparent;
            SkinnedMeshRenderer meshRenderer = sailObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (meshRenderer)
            {
                foreach (Material material in meshRenderer.materials)
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    Texture2D texture = material.mainTexture as Texture2D;
                    if (textureNames.Contains(texture.name))
                    {
                        if (!transparentTextures.ContainsKey(texture.name))
                        {
                            logger.LogInfo("Creating texture for " + texture.name);
                            transparentTextures[texture.name] = CreateTransparentTexture(
                                    ReadableTexture(originalTextures[texture.name]),
                                    configDitheringModulo.Value,
                                    configDitheringX.Value,
                                    configDitheringY.Value
                            );
                        }

                        logger.LogInfo("Mast " + instanceId + ": Overwriting sail texture: " + texture.name);
                        if (shouldBeTransparent)
                        {
                            material.mainTexture = transparentTextures[texture.name];
                        }
                        else
                        {
                            material.mainTexture = originalTextures[texture.name];
                        }

                    }
                }
            }
        }

        [HarmonyPatch(typeof(Ship), "Awake")]
        class Ship_Awake_Patch
        {
            //Reset any broken textures
            static void Prefix(Ship __instance, Cloth ___m_sailCloth)
            {
                GameObject sailObject = __instance.m_sailObject;

                SkinnedMeshRenderer meshRenderer = sailObject.GetComponentInChildren<SkinnedMeshRenderer>();
                if (meshRenderer)
                {
                    foreach (Material material in meshRenderer.materials)
                    {
                        Texture2D texture = material.mainTexture as Texture2D;
                        if (textureNames.Contains(texture.name))
                        {
                            material.mainTexture = originalTextures[texture.name];
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Ship), "UpdateSailSize")]
        class Ship_UpdateSailSize_Patch
        {
            static void Postfix(Ship __instance, Cloth ___m_sailCloth)
            {
                if (Player.m_localPlayer == null)
                {
                    return;
                }
                GameObject sailObject = __instance.m_sailObject;
                int instanceId = sailObject.GetInstanceID();

                bool shouldBeTransparent = ShouldBeTransparent(__instance, ___m_sailCloth);

                UpdateSail(instanceId, shouldBeTransparent, sailObject);
            }

        }

        public static bool ShouldBeTransparent(Ship ship, Cloth m_sailCloth)
        {
            if (!hotkeyToggle)
            {
                return false;
            }
            switch (configWhen.Value)
            {
                case When.Steering:
                    if (Player.m_localPlayer.GetControlledShip() != ship)
                    {
                        return false;
                    }
                    break;
                case When.CurrentBoat:
                    if (!ship.IsPlayerInBoat(Player.m_localPlayer))
                    {
                        return false;
                    }
                    break;
            }
            return m_sailCloth.enabled;
        }

    }
}
