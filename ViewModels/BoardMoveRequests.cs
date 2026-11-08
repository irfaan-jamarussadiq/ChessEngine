using ChessEngine.Types;

namespace ChessEngine.ViewModels
{
    public class LegalMovesRequest
    {
        public string Fen { get; set; } = string.Empty;
        public int Rank { get; set; }
        public int File { get; set; }
    }

    public class MakeMoveRequest
    {
        public string Fen { get; set; } = string.Empty;
        public int StartRank { get; set; }
        public int StartFile { get; set; }
        public int EndRank { get; set; }
        public int EndFile { get; set; }
        public PieceType? Promotion { get; set; }
    }
}
