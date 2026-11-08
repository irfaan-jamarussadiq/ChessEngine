using ChessEngine.Interfaces;
using ChessEngine.Models;
using ChessEngine.Types;
using System.Numerics;

namespace ChessEngine.Services
{
    public sealed class PositionEvaluator : IPositionEvaluator
    {
        private const int MaxDepth = 5;
        private const int MaxNodes = 20_000;
        private const int MateScore = 100_000;
        private static readonly PieceType[] PieceTypes =
        {
            PieceType.Pawn,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Rook,
            PieceType.Queen
        };
        private static readonly int[] PieceValues = { 100, 320, 330, 500, 900 };
        public PositionEvaluation Evaluate(Board board)
        {
            SearchContext context = new();
            int bestScore = EvaluateStatic(board);
            int completedDepth = 0;

            for (int depth = 1; depth <= MaxDepth; depth++)
            {
                try
                {
                    bestScore = Search(board, depth, -MateScore, MateScore, 0, context);
                    completedDepth = depth;
                }
                catch (SearchLimitReachedException)
                {
                    break;
                }
            }

            return new PositionEvaluation(bestScore, completedDepth, context.Nodes);
        }

        private int Search(Board board, int depth, int alpha, int beta, int ply, SearchContext context)
        {
            if (++context.Nodes > MaxNodes)
            {
                throw SearchLimitReachedException.Instance;
            }

            List<ChessMove> moves = GetLegalMoves(board);
            if (moves.Count == 0)
            {
                if (!board.IsKingInCheck(board.WhiteToMove))
                {
                    return 0;
                }

                return board.WhiteToMove ? -MateScore + ply : MateScore - ply;
            }

            if (depth == 0)
            {
                return EvaluateStatic(board);
            }

            moves.Sort((left, right) => GetMoveOrderScore(board, right).CompareTo(GetMoveOrderScore(board, left)));
            bool maximizing = board.WhiteToMove;
            int bestScore = maximizing ? -MateScore : MateScore;

            foreach (ChessMove move in moves)
            {
                Board child = board.CloneAndApplyGeneratedMove(move);
                int score = Search(child, depth - 1, alpha, beta, ply + 1, context);

                if (maximizing)
                {
                    bestScore = Math.Max(bestScore, score);
                    alpha = Math.Max(alpha, bestScore);
                }
                else
                {
                    bestScore = Math.Min(bestScore, score);
                    beta = Math.Min(beta, bestScore);
                }

                if (alpha >= beta)
                {
                    break;
                }
            }
            return bestScore;
        }

        private static int EvaluateStatic(Board board)
        {
            int score = 0;
            for (int pieceIndex = 0; pieceIndex < PieceTypes.Length; pieceIndex++)
            {
                PieceType pieceType = PieceTypes[pieceIndex];
                int value = PieceValues[pieceIndex];
                ulong whitePieces = board.GetBitBoard(true, pieceType);
                ulong blackPieces = board.GetBitBoard(false, pieceType);
                score += BitOperations.PopCount(whitePieces) * value;
                score -= BitOperations.PopCount(blackPieces) * value;

                while (whitePieces != 0)
                {
                    int square = BitOperations.TrailingZeroCount(whitePieces);
                    score += GetPieceSquareBonus(pieceType, square, true);
                    whitePieces &= whitePieces - 1;
                }

                while (blackPieces != 0)
                {
                    int square = BitOperations.TrailingZeroCount(blackPieces);
                    score -= GetPieceSquareBonus(pieceType, square, false);
                    blackPieces &= blackPieces - 1;
                }
            }

            return score;
        }

        private static int GetPieceSquareBonus(PieceType pieceType, int square, bool isWhite)
        {
            int rank = square / 8;
            int file = square % 8;
            int relativeRank = isWhite ? rank : 7 - rank;
            int centerDistance = Math.Abs(3 - file) + Math.Abs(3 - rank);

            return pieceType switch
            {
                PieceType.Pawn => relativeRank * 7 - Math.Abs(3 - file) * 2,
                PieceType.Knight => 28 - centerDistance * 8,
                PieceType.Bishop => 18 - centerDistance * 4,
                PieceType.Queen => 8 - centerDistance * 2,
                PieceType.Rook => relativeRank == 6 ? 18 : 0,
                _ => 0
            };
        }

        private static List<ChessMove> GetLegalMoves(Board board)
        {
            List<ChessMove> moves = new();
            for (int rank = 1; rank <= 8; rank++)
            {
                for (int file = 1; file <= 8; file++)
                {
                    if (board.PieceAt(rank, file) != PieceType.Empty
                        && board.IsWhitePieceAt(rank, file) == board.WhiteToMove)
                    {
                        moves.AddRange(board.GetLegalMoves(rank, file));
                    }
                }
            }

            return moves;
        }

        private static int GetMoveOrderScore(Board board, ChessMove move)
        {
            PieceType target = board.PieceAt(move.EndRank, move.EndFile);
            PieceType moving = board.PieceAt(move.StartRank, move.StartFile);
            int score = target == PieceType.Empty ? 0 : 10_000 + GetPieceValue(target) - GetPieceValue(moving);
            if (move.Promotion.HasValue)
            {
                score += GetPieceValue(move.Promotion.Value);
            }

            return score;
        }

        private static int GetPieceValue(PieceType pieceType) => pieceType switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => MateScore,
            _ => 0
        };

        private sealed class SearchLimitReachedException : Exception
        {
            public static readonly SearchLimitReachedException Instance = new();

            private SearchLimitReachedException()
            {
            }
        }

        private sealed class SearchContext
        {
            public int Nodes { get; set; }
        }
    }
}
