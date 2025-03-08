﻿using Jotunn.Managers;
using PlanBuild.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Plans
{
    internal class PlanDB
    {
        private static PlanDB _instance;

        public static PlanDB Instance
        {
            get
            {
                return _instance ??= new PlanDB();
            }
        }

        /// <summary>
        ///     Checks if a piece table is valid for creating plan pieces from
        /// </summary>
        /// <param name="pieceTable"></param>
        /// <returns></returns>
        private static bool IsValidPieceTable(PieceTable pieceTable)
        {
            return pieceTable && !(pieceTable.name.Equals(PlanHammerPrefab.PieceTableName) || pieceTable.name.Equals(BlueprintAssets.PieceTableName) || pieceTable.name.Equals("_FeasterPieceTable"));
        }

        public readonly Dictionary<string, Piece> PlanToOriginalMap = new Dictionary<string, Piece>();
        public readonly Dictionary<string, PlanPiecePrefab> OriginalToPlanMap = new Dictionary<string, PlanPiecePrefab>();

        /// <summary>
        ///     Different pieces can have the same m_name (also across different mods), but m_knownRecipes is a HashSet, so can not handle duplicates well
        ///     This map keeps track of the duplicate mappings
        /// </summary>
        public Dictionary<string, List<Piece>> NamePiecePrefabMapping = new Dictionary<string, List<Piece>>();

        public void ScanPieceTables()
        {
            Logger.LogDebug("Scanning PieceTables for Pieces");
            PieceTable planPieceTable = PieceManager.Instance.GetPieceTable(PlanHammerPrefab.PieceTableName);

            var pieceTables = Resources.FindObjectsOfTypeAll<PieceTable>().Where(IsValidPieceTable);
            var currentPieces = new HashSet<string>();

            // create or update plan pieces
            foreach (PieceTable pieceTable in pieceTables)
            {
                foreach (GameObject piecePrefab in pieceTable.m_pieces)
                {
                    if (!piecePrefab)
                    {
                        Logger.LogWarning($"Invalid prefab in {pieceTable.name} PieceTable");
                        continue;
                    }

                    Piece piece = piecePrefab.GetComponent<Piece>();
                    if (!piece)
                    {
                        Logger.LogWarning($"Prefab {piecePrefab.name} has no Piece?!");
                        continue;
                    }

                    if (piece.name == "piece_repair")
                    {
                        continue;
                    }

                    // Track which pieces are currently in a piece table as sometimes
                    // a Vanilla prefab can have a piece but not be in a piece table
                    if (currentPieces.Contains(piece.name))
                    {
                        Logger.LogDebug($"Multiple prefabs with name: {piece.name}");
                    }
                    else
                    {
                        currentPieces.Add(piece.name);
                    }


                    try
                    {
                        if (OriginalToPlanMap.TryGetValue(piece.name, out PlanPiecePrefab existingPlan))
                        {
                            existingPlan.Piece.m_enabled = piece.m_enabled;
                            existingPlan.Piece.m_icon = piece.m_icon;
                            continue;
                        }
                        if (!CanCreatePlan(piece))
                        {
                            continue;
                        }
                        if (!EnsurePrefabRegistered(piece))
                        {
                            continue;
                        }

                        PlanPiecePrefab planPiece = new PlanPiecePrefab(piece);
                        PrefabManager.Instance.RegisterToZNetScene(planPiece.PiecePrefab);
                        PieceManager.Instance.AddPiece(planPiece);
                        PlanToOriginalMap.Add(planPiece.PiecePrefab.name, planPiece.OriginalPiece);
                        OriginalToPlanMap.Add(piece.name, planPiece);

                        if (!NamePiecePrefabMapping.TryGetValue(piece.m_name, out List<Piece> nameList))
                        {
                            nameList = new List<Piece>();
                            NamePiecePrefabMapping.Add(piece.m_name, nameList);
                        }
                        nameList.Add(piece);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Error while creating plan of {piece.name}: {e}");
                    }
                }
            }

            // (re)create plan table
            planPieceTable.m_pieces.Clear();
            foreach (var planPieceName in OriginalToPlanMap.Keys)
            {
                try
                {
                    // do not re-add pieces that are not in a piece table
                    if (!currentPieces.Contains(planPieceName))
                    {
                        continue;
                    }

                    if (OriginalToPlanMap.TryGetValue(planPieceName, out PlanPiecePrefab planPiece))
                    {
                        planPieceTable.m_pieces.Add(planPiece.PiecePrefab);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Error while adding plan {planPieceName} to table: {e}");
                }
            }

            WarnDuplicatesWithDifferentResources();
        }

        /// <summary>
        /// Small wrapper class that implements equality and stable hashCode on Dictionary
        /// </summary>
        private class PieceRequirements
        {
            private readonly Dictionary<string, int> requirements;

            public PieceRequirements(Dictionary<string, int> requirements)
            {
                this.requirements = requirements;
            }

            public override int GetHashCode()
            {
                var hash = 13;
                var orderedKVPList = this.requirements.OrderBy(kvp => kvp.Key);
                foreach (var kvp in orderedKVPList)
                {
                    hash = (hash * 7) + kvp.Key.GetHashCode();
                    hash = (hash * 7) + kvp.Value.GetHashCode();
                }
                return hash;
            }

            public override bool Equals(object obj)
            {
                PieceRequirements other = obj as PieceRequirements;
                if (other == null)
                {
                    return false;
                }
                return this.requirements.Count == other.requirements.Count && !this.requirements.Except(other.requirements).Any();
            }

            public override string ToString()
            {
                return string.Join(", ", requirements.Select(x => x.Key + ":" + x.Value));
            }
        }

        private void WarnDuplicatesWithDifferentResources()
        {
            var warnDict = new Dictionary<string, IEnumerable<IGrouping<PieceRequirements, Piece>>>();
            foreach (var entry in NamePiecePrefabMapping)
            {
                List<Piece> pieces = entry.Value;
                if (pieces.Count == 1)
                {
                    continue;
                }

                var grouping = pieces.GroupBy(x => GetResourceMap(x));
                if (grouping.Count() > 1)
                {
                    warnDict[entry.Key] = grouping;
                }
            }

            if (warnDict.Any())
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Warning for mod developers:\nMultiple pieces with the same m_name but different resource requirements, this will cause issues with Player.m_knownRecipes!");
                foreach (var entry in warnDict)
                {
                    builder.AppendLine($"Piece.m_name: {entry.Key}");
                    foreach (var groupEntry in entry.Value)
                    {
                        builder.AppendLine($" Requirements: {groupEntry.Key}");
                        builder.AppendLine($" Pieces: {string.Join(", ", groupEntry.Select(x => x.name))}\n");
                    }
                }
                Logger.LogWarning(builder.ToString());
            }
        }

        private PieceRequirements GetResourceMap(Piece y)
        {
            var result = new Dictionary<string, int>(y.m_resources.Length);
            foreach (Piece.Requirement req in y.m_resources)
            {
                result[req.m_resItem.m_itemData.m_shared.m_name] = req.m_amount;
            }
            return new PieceRequirements(result);
        }

        internal IEnumerable<PlanPiecePrefab> GetPlanPiecePrefabs()
        {
            return OriginalToPlanMap.Values;
        }

        private bool EnsurePrefabRegistered(Piece piece)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(piece.gameObject.name);
            if (prefab)
            {
                return true;
            }
            Logger.LogWarning("Piece " + piece.name + " in Hammer not fully registered? Could not find prefab " + piece.gameObject.name);
            if (!ZNetScene.instance.m_prefabs.Contains(piece.gameObject))
            {
                Logger.LogWarning(" Not registered in ZNetScene.m_prefabs! Adding now");
                ZNetScene.instance.m_prefabs.Add(piece.gameObject);
            }
            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(piece.gameObject.name.GetStableHashCode()))
            {
                Logger.LogWarning(" Not registered in ZNetScene.m_namedPrefabs! Adding now");
                ZNetScene.instance.m_namedPrefabs[piece.gameObject.name.GetStableHashCode()] = piece.gameObject;
            }
            //Prefab was added incorrectly, make sure the game doesn't delete it when logging out
            GameObject prefabParent = piece.gameObject.transform.parent?.gameObject;
            if (!prefabParent)
            {
                Logger.LogWarning(" Prefab has no parent?! Adding to Jotunn");
                PrefabManager.Instance.AddPrefab(piece.gameObject);
            }
            else if (prefabParent.scene.buildIndex != -1)
            {
                Logger.LogWarning(" Prefab container not marked as DontDestroyOnLoad! Marking now");
                Object.DontDestroyOnLoad(prefabParent);
            }
            return PrefabManager.Instance.GetPrefab(piece.gameObject.name) != null;
        }

        /// <summary>
        ///     Tries to find the vanilla piece from a plan prefab name
        /// </summary>
        /// <param name="name">Name of the plan prefab</param>
        /// <param name="originalPiece">Vanilla piece of the plan piece</param>
        /// <returns>true if a vanilla piece was found</returns>
        internal bool FindOriginalByPrefabName(string name, out Piece originalPiece)
        {
            return PlanToOriginalMap.TryGetValue(name, out originalPiece);
        }

        /// <summary>
        ///     Tries to find all vanilla pieces with a piece component name
        /// </summary>
        /// <param name="m_name">In-game name of the piece component</param>
        /// <param name="originalPieces">List of vanilla pieces with that piece name</param>
        /// <returns></returns>
        internal bool FindOriginalByPieceName(string m_name, out List<Piece> originalPieces)
        {
            return NamePiecePrefabMapping.TryGetValue(m_name, out originalPieces);
        }

        /// <summary>
        ///     Tries to find the plan prefab from a prefab name
        /// </summary>
        /// <param name="name">Name of the prefab</param>
        /// <param name="planPiecePrefab">Plan prefab</param>
        /// <returns>true if a plan prefab was found for that prefabs name</returns>
        internal bool FindPlanByPrefabName(string name, out PlanPiecePrefab planPiecePrefab)
        {
            int index = name.IndexOf("(Clone)", StringComparison.Ordinal);
            if (index != -1)
            {
                name = name.Substring(0, index);
            }
            return OriginalToPlanMap.TryGetValue(name, out planPiecePrefab);
        }

        public bool CanCreatePlan(Piece piece)
        {
            return piece.GetComponent<Plant>() == null
                   && piece.GetComponent<TerrainOp>() == null
                   && piece.GetComponent<TerrainModifier>() == null
                   && piece.GetComponent<Ship>() == null
                   && piece.GetComponent<PlanPiece>() == null
                   && !piece.name.Equals(PlanTotemPrefab.PlanTotemPieceName);
        }
    }
}
