namespace ChessEngine.ViewModels
{
    public class BoardViewModel
    {
        public int BoardSize { get; set; } = 64;
        public string FenString { get; set; } = string.Empty;
        public int? CheckedKingRank { get; set; }
        public int? CheckedKingFile { get; set; }
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }
        public bool IsStalemate { get; set; }
        public ICollection<SquareViewModel> Squares { get; set; } = new List<SquareViewModel>(64);
    }
}
