using ChessEngine.Models;
using ChessEngine.Types;

namespace ChessEngine.Interfaces
{
    public interface IBoardService
    {
        public IReadOnlyList<ChessMove> LegalMovesFrom(int startRank, int startFile);
        public string MakeMove(int startRank, int startFile, int endRank, int endFile, PieceType? promotion = null);
        public Board FromFen(string fen);
        public string ToFen();
        public PieceType PieceAt(int rank, int file);
        public bool IsWhitePieceAt(int rank, int file);
    }
}
