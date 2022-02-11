using System.Linq;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal static class BlueprintPiece
    {
        public const string zdoBlueprintID = "BlueprintID";
        public const string zdoBlueprintPiece = "BlueprintPiece";
        public const string zdoAdditionalInfo = "AdditionalText";

        internal static ZDOID GetPieceID(this Piece piece)
        {
            if (!piece.TryGetComponent<ZNetView>(out var znet) && znet.IsValid())
            {
                return ZDOID.None;
            }
            return znet.m_zdo.m_uid;
        }

        internal static ZDOID GetBlueprintID(this Piece piece)
        {
            if (!piece.TryGetComponent<ZNetView>(out var znet) && znet.IsValid())
            {
                return ZDOID.None;
            }
            return znet.m_zdo.GetZDOID(zdoBlueprintID);
        }

        internal static void PartOfBlueprint(this Piece piece, ZDOID blueprintID, PieceEntry entry)
        {
            if (!piece.TryGetComponent<ZNetView>(out var znet) && znet.IsValid())
            {
                return;
            }

            ZDO pieceZDO = znet.m_zdo;
            pieceZDO.Set(zdoBlueprintID, blueprintID);
            pieceZDO.Set(zdoAdditionalInfo, entry.additionalInfo);
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
