using ChessEngine.Interfaces;
using ChessEngine.Models;
using ChessEngine.Types;
using System.Globalization;
using System.Numerics;

namespace ChessEngine.Services
{
    public class BoardService(Board board) : IBoardService
    {
        private readonly Board _board = board;

        public IReadOnlyList<ChessMove> LegalMovesFrom(int startRank, int startFile)
        {
            return _board.GetLegalMoves(startRank, startFile);
        }

        public string MakeMove(int startRank, int startFile, int endRank, int endFile, PieceType? promotion = null)
        {
            return _board.MakeMove(new ChessMove(startRank, startFile, endRank, endFile, promotion));
        }

        public Board FromFen(string fen)
        {
            string[] parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            ValidateFenParts(parts);
            _board.Clear();

            string[] ranks = parts[0].Split('/');
            for (int rankIndex = 0; rankIndex < ranks.Length; rankIndex++)
            {
                int rank = 8 - rankIndex;
                int file = 1;
                foreach (char c in ranks[rankIndex])
                {
                    if (char.IsAsciiDigit(c))
                    {
                        file += c - '0';
                    }
                    else
                    {
                        bool isWhite = char.IsUpper(c);
                        _board.AddPiece(isWhite, GetPieceType(c), rank, file);
                        file++;
                    }
                }
            }

            bool whiteToMove = parts[1] == "w";
            if (BitOperations.PopCount(_board.GetBitBoard(true, PieceType.King)) != 1
                || BitOperations.PopCount(_board.GetBitBoard(false, PieceType.King)) != 1)
            {
                throw new ArgumentException("Invalid FEN string: position must contain exactly one king per side");
            }

            string castlingRights = parts[2] == "-" ? "-" : string.Concat("KQkq".Where(parts[2].Contains));
            int? enPassantRank = parts[3] == "-" ? null : parts[3][1] - '0';
            int? enPassantFile = parts[3] == "-" ? null : parts[3][0] - 'a' + 1;
            if (enPassantRank.HasValue)
            {
                int pawnRank = enPassantRank.Value + (whiteToMove ? -1 : 1);
                bool lastMoverWhite = !whiteToMove;
                if (_board.PieceAt(enPassantRank.Value, enPassantFile!.Value) != PieceType.Empty
                    || _board.PieceAt(pawnRank, enPassantFile.Value) != PieceType.Pawn
                    || _board.IsWhitePieceAt(pawnRank, enPassantFile.Value) != lastMoverWhite)
                {
                    throw new ArgumentException("Invalid FEN string: en-passant target does not match the position");
                }
            }

            int halfmoveClock = int.Parse(parts[4], CultureInfo.InvariantCulture);
            int fullmoveNumber = int.Parse(parts[5], CultureInfo.InvariantCulture);
            _board.SetPositionMetadata(whiteToMove, castlingRights, enPassantRank, enPassantFile, halfmoveClock, fullmoveNumber);

            return _board;
        }

        private static void ValidateFenParts(string[] parts)
        {
            if (parts.Length != 6)
            {
                throw new ArgumentException("Invalid FEN string: missing fields");
            }

            string[] ranks = parts[0].Split('/');
            if (ranks.Length != 8)
            {
                throw new ArgumentException("Invalid FEN string: piece placement must contain eight ranks");
            }

            foreach (string rank in ranks)
            {
                int squares = 0;
                foreach (char c in rank)
                {
                    if (char.IsAsciiDigit(c) && c is >= '1' and <= '8')
                    {
                        squares += c - '0';
                    }
                    else if ("prnbqkPRNBQK".Contains(c))
                    {
                        squares++;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid FEN string: invalid character '{c}' in piece placement");
                    }
                }

                if (squares != 8)
                {
                    throw new ArgumentException("Invalid FEN string: each rank must contain eight squares");
                }
            }

            string activeColor = parts[1];
            if (activeColor != "w" && activeColor != "b")
            {
                throw new ArgumentException("Invalid FEN string: active color must be 'w' or 'b'");
            }

            string castlingRights = parts[2];
            if (castlingRights != "-"
                && (castlingRights.Length is < 1 or > 4
                    || castlingRights.Any(c => !"KQkq".Contains(c))
                    || castlingRights.Distinct().Count() != castlingRights.Length))
            {
                throw new ArgumentException("Invalid FEN string: castling rights must be a combination of 'K', 'Q', 'k', 'q' or '-'");
            }

            string enPassant = parts[3];
            if (enPassant != "-"
                && (enPassant.Length != 2
                    || enPassant[0] is < 'a' or > 'h'
                    || enPassant[1] is not ('3' or '6')
                    || (activeColor == "w" && enPassant[1] != '6')
                    || (activeColor == "b" && enPassant[1] != '3')))
            {
                throw new ArgumentException("Invalid FEN string: invalid en-passant target");
            }

            if (!int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out int halfmoveClock) || halfmoveClock < 0
                || !int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out int fullmoveNumber) || fullmoveNumber < 1)
            {
                throw new ArgumentException("Invalid FEN string: invalid move counters");
            }
        }

        private static PieceType GetPieceType(char c)
        {
            return c switch
            {
                'p' or 'P' => PieceType.Pawn,
                'r' or 'R' => PieceType.Rook,
                'n' or 'N' => PieceType.Knight,
                'b' or 'B' => PieceType.Bishop,
                'q' or 'Q' => PieceType.Queen,
                'k' or 'K' => PieceType.King,
                _ => PieceType.Empty,
            };
        }

        public string ToFen()
        {
            return _board.ToFen();
        }

        public PieceType PieceAt(int rank, int file)
        {
            return _board.PieceAt(rank, file);
        }

        public bool IsWhitePieceAt(int rank, int file)
        {
            return _board.IsWhitePieceAt(rank, file);
        }
    }
}
