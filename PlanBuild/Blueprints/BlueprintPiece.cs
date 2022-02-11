using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal static class BlueprintPiece
    {
        public const string zdoBlueprintName = "BlueprintName";
        public const string zdoBlueprintID = "BlueprintID";
        public const string zdoBlueprintPiece = "BlueprintPiece";
        public const string zdoAdditionalInfo = "AdditionalText";

        public static ZDOID GetPieceID(this Piece piece)
        {
            return piece?.m_nview?.GetZDO()?.m_uid ?? ZDOID.None;
        }

        public static ZDOID GetBlueprintID(this Piece piece)
        {
            var zdo = piece?.m_nview?.GetZDO();
            if (zdo == null || !zdo.IsValid())
            {
                return ZDOID.None;
            }
            return zdo.GetZDOID(zdoBlueprintID);
        }

        public static void AddToBlueprint(this Piece piece, ZDOID blueprintID, PieceEntry entry)
        {
            var zdo = piece?.m_nview?.GetZDO();
            if (zdo == null || !zdo.IsValid())
            {
                return;
            }
            zdo.Set(zdoBlueprintID, blueprintID);
            zdo.Set(zdoAdditionalInfo, entry.additionalInfo);
        }
        
        /// <summary>
        ///     Remove a <see cref="Piece"/> instance ZDO from its Blueprint <see cref="ZDOIDSet"/>
        /// </summary>
        /// <param name="piece"></param>
        public static void RemoveFromBlueprint(this Piece piece)
        {
            ZDOID blueprintID = piece.GetBlueprintID();
            if (blueprintID == ZDOID.None)
            {
                return;
            }

            ZDO blueprintZDO = ZDOMan.instance.GetZDO(blueprintID);
            if (blueprintZDO == null)
            {
                return;
            }
            ZDOIDSet blueprintPieces = BlueprintManager.Instance.GetBlueprintPieces(blueprintZDO);
            blueprintPieces?.Remove(piece.GetPieceID());
            if (blueprintPieces == null || !blueprintPieces.Any())
            {
                GameObject blueprintObject = ZNetScene.instance.FindInstance(blueprintID);
                if (blueprintObject)
                {
                    ZNetScene.instance.Destroy(blueprintObject);
                }
            }
            else
            {
                blueprintZDO.Set(zdoBlueprintPiece, blueprintPieces.ToZPackage().GetArray());
            }
        }
    }
}
