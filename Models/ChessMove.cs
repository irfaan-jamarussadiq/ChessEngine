using ChessEngine.Types;

namespace ChessEngine.Models
{
    public sealed record ChessMove(int StartRank, int StartFile, int EndRank, int EndFile, PieceType? Promotion = null);
}
