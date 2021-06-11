using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SeedTotem.SeedTotemMod;
using Object = UnityEngine.Object;

namespace SeedTotem
{
    internal class SeedTotemPrefabConfig
    {
        public static ManualLogSource logger;

        public const string prefabName = "SeedTotem";
        private const string localizationName = "seed_totem";
        public const string ravenTopic = "$tutorial_" + localizationName + "_topic";
        public const string ravenText = "$tutorial_" + localizationName + "_text";
        public const string ravenLabel = "$tutorial_" + localizationName + "_label";
        private const string iconPath = "icons/seed_totem.png";
        public const string requirementsFile = "seed-totem-custom-requirements.json";
        internal static ConfigEntry<SeedTotemMod.PieceLocation> configLocation;
        private PieceTable pieceTable;
        private Piece piece;

        private GameObject currentPiece;

        public SeedTotemPrefabConfig()
        {
        }

        private static RequirementConfig[] LoadJsonFile(string filename)
        {
            RequirementConfig[] defaultRecipe = new RequirementConfig[] {
                new RequirementConfig()
                {
                    Item = "FineWood",
                    Amount = 5,
                    Recover = true
                },
                 new RequirementConfig()
                {
                    Item = "GreydwarfEye",
                    Amount = 5,
                    Recover = true
                },
                  new RequirementConfig()
                {
                    Item = "SurtlingCore",
                    Amount = 1,
                    Recover = true
                },
                   new RequirementConfig()
                {
                    Item = "AncientSeed",
                    Amount = 1,
                    Recover = true
                }
            };
            if (SeedTotem.configCustomRecipe.Value)
            {
                string assetPath = SeedTotemMod.GetAssetPath(filename);
                bool fileFound = string.IsNullOrEmpty(assetPath);
                if (fileFound)
                {
                    logger.LogWarning("File not found: " + filename + " using default recipe");
                    return defaultRecipe;
                }

                Dictionary<string, int> reqDict = ReadDict(assetPath);
                RequirementConfig[] result = new RequirementConfig[reqDict.Count];
                int i = 0;
                foreach (KeyValuePair<string, int> pair in reqDict)
                {
                    result[i] = new RequirementConfig()
                    {
                        Item = pair.Key,
                        Amount = pair.Value,
                        Recover = true
                    };
                }
                return result;
            }
            else
            {
                return defaultRecipe;
            }
        }

        private static Dictionary<string, int> ReadDict(string assetPath)
        {
            string json = File.ReadAllText(assetPath);
            Dictionary<string, int> dictionary = (Dictionary<string, int>)SimpleJson.SimpleJson.DeserializeObject(json, typeof(Dictionary<string, int>));
            return dictionary;
        }

        private SeedTotem prefabSeedTotem;
        private GameObject Prefab;

        public void UpdateCopiedPrefab(GameObject Prefab)
        {
            this.Prefab = Prefab;

            Piece piece = Prefab.GetComponent<Piece>();
            piece.m_name = "$piece_seed_totem_name";
            piece.m_description = "$piece_seed_totem_description";
            piece.m_clipGround = true;
            piece.m_groundPiece = true;
            piece.m_groundOnly = true;
            piece.m_noInWater = true;
            foreach (GuidePoint guidePoint in Prefab.GetComponentsInChildren<GuidePoint>())
            {
                guidePoint.m_text.m_key = localizationName;
                guidePoint.m_text.m_topic = ravenTopic;
                guidePoint.m_text.m_text = ravenText;
                guidePoint.m_text.m_label = ravenLabel;
            }

            prefabSeedTotem = Prefab.AddComponent<SeedTotem>();
            PrivateArea privateArea = Prefab.GetComponent<PrivateArea>();
            if (privateArea != null)
            {
                logger.LogDebug("Converting PrivateArea to SeedTotem");
                SeedTotem.CopyPrivateArea(prefabSeedTotem, privateArea);
                logger.LogDebug("Destroying redundant PrivateArea: " + privateArea);
                Object.DestroyImmediate(privateArea);
            }

            RegisterPiece();
        }

        internal void RegisterPiece()
        {
            logger.LogInfo("Registering Seed Totem Piece");
            Texture2D iconTexture = AssetUtils.LoadTexture(SeedTotemMod.GetAssetPath(iconPath));
            Sprite iconSprite = null;
            if (iconTexture == null)
            {
                logger.LogWarning("Icon missing, should be at " + iconPath + ", using default icon instead ");
            }
            else
            {
                iconSprite = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height), Vector2.zero);
            }
            PieceConfig pieceConfig = new PieceConfig()
            {
                PieceTable = configLocation.GetSerializedValue(),
                Description = "$piece_seed_totem_description",
                Requirements = LoadJsonFile(SeedTotemMod.GetAssetPath("seed-totem-custom-requirements.json"))
            };
            if (iconSprite)
            {
                pieceConfig.Icon = iconSprite;
            }

            CustomPiece customPiece = new CustomPiece(Prefab, pieceConfig);

            PieceManager.Instance.AddPiece(customPiece);
        }

        internal void UpdatePieceLocation()
        {
            logger.LogInfo("Moving Seed Totem to " + configLocation.Value);
            foreach (PieceLocation location in Enum.GetValues(typeof(PieceLocation)))
            {
                currentPiece = RemovePieceFromPieceTable(location, prefabName);
                if (currentPiece != null)
                {
                    break;
                }
            }
            if (configLocation.Value == PieceLocation.Cultivator)
            {
                GetPieceTable(configLocation.Value).m_pieces.Insert(2, currentPiece);
            }
            else
            {
                GetPieceTable(configLocation.Value).m_pieces.Add(currentPiece);
            }
            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.AddKnownPiece(currentPiece.GetComponent<Piece>());
            }
        }

        private PieceTable GetPieceTable(PieceLocation location)
        {
            string pieceTableName = $"_{location}PieceTable";
            Object[] array = Resources.FindObjectsOfTypeAll(typeof(PieceTable));
            for (int i = 0; i < array.Length; i++)
            {
                PieceTable pieceTable = (PieceTable)array[i];
                string name = pieceTable.gameObject.name;
                if (pieceTableName == name)
                {
                    return pieceTable;
                }
            }
            return null;
        }

        private GameObject GetPieceFromPieceTable(PieceLocation location, string pieceName)
        {
            PieceTable pieceTable = GetPieceTable(location);
            int currentPosition = pieceTable.m_pieces.FindIndex(piece => piece.name == pieceName);
            if (currentPosition >= 0)
            {
                logger.LogInfo("Found Piece " + pieceName + " at position " + currentPosition);
                GameObject @object = pieceTable.m_pieces[currentPosition];
                pieceTable.m_pieces.RemoveAt(currentPosition);
                return @object;
            }
            return null;
        }

        private GameObject RemovePieceFromPieceTable(PieceLocation location, string pieceName)
        {
            logger.LogDebug("Removing " + pieceName + " from " + location);
            PieceTable pieceTable = GetPieceTable(location);
            int currentPosition = pieceTable.m_pieces.FindIndex(piece => piece.name == pieceName);
            if (currentPosition >= 0)
            {
                logger.LogDebug("Found Piece " + pieceName + " at position " + currentPosition);
                GameObject @object = pieceTable.m_pieces[currentPosition];
                pieceTable.m_pieces.RemoveAt(currentPosition);
                return @object;
            }

            return null;
        }

        private Piece SetPieceTablePosition(string pieceTableName, string pieceName, int position)
        {
            logger.LogInfo("Moving " + pieceName + " to position " + position + " in " + pieceTableName);
            Object[] array = Resources.FindObjectsOfTypeAll(typeof(PieceTable));
            for (int i = 0; i < array.Length; i++)
            {
                pieceTable = (PieceTable)array[i];
                string name = pieceTable.gameObject.name;
                if (pieceTableName == name)
                {
                    logger.LogInfo("Found PieceTable " + pieceTableName);
                    int currentPosition = pieceTable.m_pieces.FindIndex(piece => piece.name == pieceName);
                    if (currentPosition >= 0)
                    {
                        logger.LogInfo("Found Piece " + pieceName + " at position " + currentPosition);
                        GameObject @object = pieceTable.m_pieces[currentPosition];
                        pieceTable.m_pieces.RemoveAt(currentPosition);
                        logger.LogInfo("Moving to position " + position);
                        pieceTable.m_pieces.Insert(position, @object);
                        return @object.GetComponent<Piece>();
                    }
                }
            }
            return null;
        }
    }
}