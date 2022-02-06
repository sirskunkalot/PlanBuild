namespace PlanBuild.Blueprints
{
    internal static class BlueprintPiece
    {
        public const string zdoBlueprintID = "BlueprintID";
        public const string zdoBlueprintPiece = "BlueprintPiece";
        public const string zdoAdditionalInfo = "AdditionalText";

        internal static ZDOID GetBlueprintPieceID(this Piece piece)
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
    }
}
