namespace ChessEngine.ViewModels
{
    public class SquareViewModel
    {
        public int Rank { get; set; }
        public int File { get; set; }
        public PieceViewModel? Piece { get; set; }
    }
}