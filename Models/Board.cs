using ChessEngine.Types;
using System.Text;

namespace ChessEngine.Models
{
    public class Board
    {
        public const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private readonly ulong[] bitboards;
        private readonly static int whitePawnIndex = 0;
        private readonly static int whiteQueenIndex = 1;
        private readonly static int whiteKingIndex = 2;
        private readonly static int whiteBishopIndex = 3;
        private readonly static int whiteKnightIndex = 4;
        private readonly static int whiteRookIndex = 5;
        private readonly static int blackPawnIndex = 6;
        private readonly static int blackQueenIndex = 7;
        private readonly static int blackKingIndex = 8;
        private readonly static int blackBishopIndex = 9;
        private readonly static int blackKnightIndex = 10;
        private readonly static int blackRookIndex = 11;

        public bool WhiteToMove { get; private set; } = true;
        public string CastlingRights { get; private set; } = "KQkq";
        public int? EnPassantTargetRank { get; private set; }
        public int? EnPassantTargetFile { get; private set; }
        public int HalfmoveClock { get; private set; }
        public int FullmoveNumber { get; private set; } = 1;

        public Board()
        {
            bitboards = new ulong[12];
        }

        public void Clear()
        {
            Array.Clear(bitboards);
            WhiteToMove = true;
            CastlingRights = "KQkq";
            EnPassantTargetRank = null;
            EnPassantTargetFile = null;
            HalfmoveClock = 0;
            FullmoveNumber = 1;
        }

        public Board Clone()
        {
            Board copy = new();
            Array.Copy(bitboards, copy.bitboards, bitboards.Length);
            copy.SetPositionMetadata(WhiteToMove, CastlingRights, EnPassantTargetRank, EnPassantTargetFile, HalfmoveClock, FullmoveNumber);
            return copy;
        }

        internal Board CloneAndApplyGeneratedMove(ChessMove move)
        {
            Board copy = Clone();
            copy.ApplyMoveUnchecked(move);
            return copy;
        }

        public void SetPositionMetadata(bool whiteToMove, string castlingRights, int? enPassantTargetRank, int? enPassantTargetFile, int halfmoveClock, int fullmoveNumber)
        {
            WhiteToMove = whiteToMove;
            CastlingRights = castlingRights;
            EnPassantTargetRank = enPassantTargetRank;
            EnPassantTargetFile = enPassantTargetFile;
            HalfmoveClock = halfmoveClock;
            FullmoveNumber = fullmoveNumber;
        }

        public void SetUpStartingPosition()
        {
            bitboards[whitePawnIndex] = 0x000000000000FF00UL;
            bitboards[whiteQueenIndex] = 0x0000000000000008UL;
            bitboards[whiteKingIndex] = 0x0000000000000010UL;
            bitboards[whiteBishopIndex] = 0x0000000000000024UL;
            bitboards[whiteKnightIndex] = 0x0000000000000042UL;
            bitboards[whiteRookIndex] = 0x0000000000000081UL;
            bitboards[blackPawnIndex] = 0x00FF000000000000UL;
            bitboards[blackQueenIndex] = 0x0800000000000000UL;
            bitboards[blackKingIndex] = 0x1000000000000000UL;
            bitboards[blackBishopIndex] = 0x2400000000000000UL;
            bitboards[blackKnightIndex] = 0x4200000000000000UL;
            bitboards[blackRookIndex] = 0x8100000000000000UL;
        }

        private static int MapBitBoardIndex(bool isWhite, PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => isWhite ? whitePawnIndex : blackPawnIndex,
                PieceType.King => isWhite ? whiteKingIndex : blackKingIndex,
                PieceType.Queen => isWhite ? whiteQueenIndex : blackQueenIndex,
                PieceType.Bishop => isWhite ? whiteBishopIndex : blackBishopIndex,
                PieceType.Knight => isWhite ? whiteKnightIndex : blackKnightIndex,
                PieceType.Rook => isWhite ? whiteRookIndex : blackRookIndex,
                _ => throw new ArgumentException("Invalid piece type", nameof(pieceType)),
            };
        }

        public ulong GetBitBoard(bool isWhite, PieceType pieceType)
        {
            if (pieceType == PieceType.Empty)
            {
                throw new ArgumentException("pieceType cannot be Empty", nameof(pieceType));
            }
            
            int bitBoardIndex = MapBitBoardIndex(isWhite, pieceType);
            return bitboards[bitBoardIndex];
        }

        public void MakeRawMove(int startRank, int startFile, int endRank, int endFile)
        {
            int bitIndex = GetBitIndex(startRank, startFile);
            int pieceIndex = -1;

            for (int i = 0; i < bitboards.Length; i++)
            {
                if ((bitboards[i] & (1UL << bitIndex)) != 0)
                {
                    pieceIndex = i;
                    break;
                }
            }

            if (pieceIndex < 0)
            {
                throw new InvalidOperationException("No piece found at the starting square");
            }

            PieceType pieceType = GetPieceTypeFromBitBoardIndex(pieceIndex);
            bool isWhite = pieceIndex < 6;

            RemovePiece(startRank, startFile);
            AddPiece(isWhite, pieceType, endRank, endFile);
        }

        public IReadOnlyList<ChessMove> GetPseudoLegalMoves(int startRank, int startFile)
        {
            PieceType piece = PieceAt(startRank, startFile);
            if (piece == PieceType.Empty || IsWhitePieceAt(startRank, startFile) != WhiteToMove)
            {
                return Array.Empty<ChessMove>();
            }

            List<ChessMove> moves = new();
            bool isWhite = WhiteToMove;
            switch (piece)
            {
                case PieceType.Pawn:
                    AddPawnMoves(moves, startRank, startFile, isWhite);
                    break;
                case PieceType.Knight:
                    foreach ((int rankOffset, int fileOffset) in new[] { (2, 1), (2, -1), (-2, 1), (-2, -1), (1, 2), (1, -2), (-1, 2), (-1, -2) })
                    {
                        AddStepMove(moves, startRank, startFile, startRank + rankOffset, startFile + fileOffset);
                    }
                    break;
                case PieceType.Bishop:
                    AddSlidingMoves(moves, startRank, startFile, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) });
                    break;
                case PieceType.Rook:
                    AddSlidingMoves(moves, startRank, startFile, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) });
                    break;
                case PieceType.Queen:
                    AddSlidingMoves(moves, startRank, startFile, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1), (1, 0), (-1, 0), (0, 1), (0, -1) });
                    break;
                case PieceType.King:
                    for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
                    {
                        for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
                        {
                            if (rankOffset != 0 || fileOffset != 0)
                            {
                                AddStepMove(moves, startRank, startFile, startRank + rankOffset, startFile + fileOffset);
                            }
                        }
                    }
                    AddCastlingMoves(moves, startRank, startFile, isWhite);
                    break;
            }

            return moves;
        }

        public bool IsSquareAttacked(int targetRank, int targetFile, bool byWhite)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                for (int file = 1; file <= 8; file++)
                {
                    PieceType piece = PieceAt(rank, file);
                    if (piece == PieceType.Empty || IsWhitePieceAt(rank, file) != byWhite)
                    {
                        continue;
                    }

                    int rankDistance = targetRank - rank;
                    int fileDistance = targetFile - file;
                    int absRankDistance = Math.Abs(rankDistance);
                    int absFileDistance = Math.Abs(fileDistance);

                    if (piece == PieceType.Pawn && rankDistance == (byWhite ? 1 : -1) && absFileDistance == 1)
                    {
                        return true;
                    }

                    if (piece == PieceType.Knight && ((absRankDistance == 2 && absFileDistance == 1) || (absRankDistance == 1 && absFileDistance == 2)))
                    {
                        return true;
                    }

                    if (piece == PieceType.King && absRankDistance <= 1 && absFileDistance <= 1 && (absRankDistance != 0 || absFileDistance != 0))
                    {
                        return true;
                    }

                    bool diagonal = absRankDistance == absFileDistance && absRankDistance > 0;
                    bool straight = (rankDistance == 0) != (fileDistance == 0);
                    bool attacksLine = piece switch
                    {
                        PieceType.Bishop => diagonal,
                        PieceType.Rook => straight,
                        PieceType.Queen => diagonal || straight,
                        _ => false,
                    };

                    if (attacksLine && IsPathClear(rank, file, targetRank, targetFile))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IReadOnlyList<ChessMove> GetLegalMoves(int startRank, int startFile)
        {
            IReadOnlyList<ChessMove> candidates = GetPseudoLegalMoves(startRank, startFile);
            List<ChessMove> legalMoves = new();
            foreach (ChessMove move in candidates)
            {
                if (IsCastlingMove(move)
                    && (IsSquareAttacked(move.StartRank, move.StartFile, !WhiteToMove)
                        || IsSquareAttacked(move.StartRank, (move.StartFile + move.EndFile) / 2, !WhiteToMove)))
                {
                    continue;
                }

                Board nextPosition = Clone();
                nextPosition.ApplyMoveUnchecked(move);
                if (!nextPosition.IsKingInCheck(WhiteToMove))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        public bool IsKingInCheck(bool whiteKing)
        {
            (int Rank, int File)? kingSquare = GetKingSquare(whiteKing);
            return !kingSquare.HasValue
                || IsSquareAttacked(kingSquare.Value.Rank, kingSquare.Value.File, !whiteKing);
        }

        public (int Rank, int File)? GetKingSquare(bool whiteKing)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                for (int file = 1; file <= 8; file++)
                {
                    if (PieceAt(rank, file) == PieceType.King && IsWhitePieceAt(rank, file) == whiteKing)
                    {
                        return (rank, file);
                    }
                }
            }

            return null;
        }

        public string MakeMove(ChessMove requestedMove)
        {
            ChessMove? legalMove = GetLegalMoves(requestedMove.StartRank, requestedMove.StartFile)
                .FirstOrDefault(move => move.EndRank == requestedMove.EndRank
                    && move.EndFile == requestedMove.EndFile
                    && move.Promotion == requestedMove.Promotion);

            if (legalMove is null)
            {
                throw new InvalidOperationException("The requested move is not legal in this position");
            }

            string notation = GetSan(legalMove);
            ApplyMoveUnchecked(legalMove);
            return notation;
        }

        private string GetSan(ChessMove move)
        {
            PieceType movingPiece = PieceAt(move.StartRank, move.StartFile);
            bool isCapture = PieceAt(move.EndRank, move.EndFile) != PieceType.Empty
                || (movingPiece == PieceType.Pawn && move.StartFile != move.EndFile);
            StringBuilder notation = new();

            if (IsCastlingMove(move))
            {
                notation.Append(move.EndFile > move.StartFile ? "O-O" : "O-O-O");
            }
            else
            {
                if (movingPiece == PieceType.Pawn)
                {
                    if (isCapture)
                    {
                        notation.Append((char)('a' + move.StartFile - 1));
                    }
                }
                else
                {
                    notation.Append(GetSanPieceLetter(movingPiece));
                    notation.Append(GetDisambiguation(move, movingPiece));
                }

                if (isCapture)
                {
                    notation.Append('x');
                }

                notation.Append((char)('a' + move.EndFile - 1)).Append(move.EndRank);
                if (move.Promotion.HasValue)
                {
                    notation.Append('=').Append(GetSanPieceLetter(move.Promotion.Value));
                }
            }

            Board resultingPosition = Clone();
            resultingPosition.ApplyMoveUnchecked(move);
            if (resultingPosition.IsKingInCheck(resultingPosition.WhiteToMove))
            {
                notation.Append(resultingPosition.HasAnyLegalMoves() ? '+' : '#');
            }

            return notation.ToString();
        }

        private string GetDisambiguation(ChessMove move, PieceType movingPiece)
        {
            List<ChessMove> competingMoves = new();
            for (int rank = 1; rank <= 8; rank++)
            {
                for (int file = 1; file <= 8; file++)
                {
                    if ((rank == move.StartRank && file == move.StartFile)
                        || PieceAt(rank, file) != movingPiece
                        || IsWhitePieceAt(rank, file) != WhiteToMove)
                    {
                        continue;
                    }

                    if (GetLegalMoves(rank, file).Any(candidate => candidate.EndRank == move.EndRank && candidate.EndFile == move.EndFile))
                    {
                        competingMoves.Add(new ChessMove(rank, file, move.EndRank, move.EndFile));
                    }
                }
            }

            if (competingMoves.Count == 0)
            {
                return string.Empty;
            }

            bool sharesFile = competingMoves.Any(candidate => candidate.StartFile == move.StartFile);
            bool sharesRank = competingMoves.Any(candidate => candidate.StartRank == move.StartRank);
            if (!sharesFile)
            {
                return ((char)('a' + move.StartFile - 1)).ToString();
            }

            if (!sharesRank)
            {
                return move.StartRank.ToString();
            }

            return $"{(char)('a' + move.StartFile - 1)}{move.StartRank}";
        }

        public bool HasAnyLegalMoves()
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                for (int file = 1; file <= 8; file++)
                {
                    if (PieceAt(rank, file) != PieceType.Empty
                        && IsWhitePieceAt(rank, file) == WhiteToMove
                        && GetLegalMoves(rank, file).Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static char GetSanPieceLetter(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.King => 'K',
                PieceType.Queen => 'Q',
                PieceType.Rook => 'R',
                PieceType.Bishop => 'B',
                PieceType.Knight => 'N',
                _ => throw new ArgumentOutOfRangeException(nameof(pieceType)),
            };
        }

        private void ApplyMoveUnchecked(ChessMove move)
        {
            PieceType movingPiece = PieceAt(move.StartRank, move.StartFile);
            bool movingWhite = IsWhitePieceAt(move.StartRank, move.StartFile);
            PieceType capturedPiece = PieceAt(move.EndRank, move.EndFile);
            bool isEnPassantCapture = movingPiece == PieceType.Pawn
                && move.StartFile != move.EndFile
                && capturedPiece == PieceType.Empty;
            int capturedRank = isEnPassantCapture ? move.EndRank + (movingWhite ? -1 : 1) : move.EndRank;

            UpdateCastlingRights(move, movingPiece, capturedPiece, capturedRank);
            RemovePiece(move.StartRank, move.StartFile);
            if (capturedPiece != PieceType.Empty || isEnPassantCapture)
            {
                RemovePiece(capturedRank, move.EndFile);
            }

            if (IsCastlingMove(move))
            {
                int rookStartFile = move.EndFile == 7 ? 8 : 1;
                int rookEndFile = move.EndFile == 7 ? 6 : 4;
                RemovePiece(move.StartRank, rookStartFile);
                AddPiece(movingWhite, PieceType.Rook, move.StartRank, rookEndFile);
            }

            AddPiece(movingWhite, move.Promotion ?? movingPiece, move.EndRank, move.EndFile);

            EnPassantTargetRank = movingPiece == PieceType.Pawn && Math.Abs(move.EndRank - move.StartRank) == 2
                ? (move.StartRank + move.EndRank) / 2
                : null;
            EnPassantTargetFile = EnPassantTargetRank.HasValue ? move.StartFile : null;
            HalfmoveClock = movingPiece == PieceType.Pawn || capturedPiece != PieceType.Empty || isEnPassantCapture
                ? 0
                : HalfmoveClock + 1;
            if (!movingWhite)
            {
                FullmoveNumber++;
            }

            WhiteToMove = !WhiteToMove;
        }

        private void UpdateCastlingRights(ChessMove move, PieceType movingPiece, PieceType capturedPiece, int capturedRank)
        {
            HashSet<char> rights = CastlingRights.ToHashSet();
            if (movingPiece == PieceType.King)
            {
                rights.Remove(IsWhitePieceAt(move.StartRank, move.StartFile) ? 'K' : 'k');
                rights.Remove(IsWhitePieceAt(move.StartRank, move.StartFile) ? 'Q' : 'q');
            }

            if (movingPiece == PieceType.Rook)
            {
                RemoveCastlingRightForRook(rights, move.StartRank, move.StartFile);
            }

            if (capturedPiece == PieceType.Rook)
            {
                RemoveCastlingRightForRook(rights, capturedRank, move.EndFile);
            }

            CastlingRights = string.Concat("KQkq".Where(rights.Contains));
            if (CastlingRights.Length == 0)
            {
                CastlingRights = "-";
            }
        }

        private static void RemoveCastlingRightForRook(HashSet<char> rights, int rank, int file)
        {
            if (rank == 1 && file == 1) rights.Remove('Q');
            if (rank == 1 && file == 8) rights.Remove('K');
            if (rank == 8 && file == 1) rights.Remove('q');
            if (rank == 8 && file == 8) rights.Remove('k');
        }

        private static bool IsCastlingMove(ChessMove move)
        {
            return Math.Abs(move.EndFile - move.StartFile) == 2
                && move.StartRank == move.EndRank
                && move.StartFile == 5;
        }

        private void AddPawnMoves(List<ChessMove> moves, int startRank, int startFile, bool isWhite)
        {
            int direction = isWhite ? 1 : -1;
            int promotionRank = isWhite ? 8 : 1;
            int nextRank = startRank + direction;

            if (IsInsideBoard(nextRank, startFile) && PieceAt(nextRank, startFile) == PieceType.Empty)
            {
                AddPawnMove(moves, startRank, startFile, nextRank, startFile, promotionRank);
                int startRankForColor = isWhite ? 2 : 7;
                int doubleStepRank = startRank + direction * 2;
                if (startRank == startRankForColor && PieceAt(doubleStepRank, startFile) == PieceType.Empty)
                {
                    moves.Add(new ChessMove(startRank, startFile, doubleStepRank, startFile));
                }
            }

            for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
            {
                int targetFile = startFile + fileOffset;
                if (!IsInsideBoard(nextRank, targetFile))
                {
                    continue;
                }

                PieceType targetPiece = PieceAt(nextRank, targetFile);
                if (targetPiece != PieceType.Empty && IsWhitePieceAt(nextRank, targetFile) != isWhite && targetPiece != PieceType.King)
                {
                    AddPawnMove(moves, startRank, startFile, nextRank, targetFile, promotionRank);
                }
                else if (targetPiece == PieceType.Empty
                    && EnPassantTargetRank == nextRank
                    && EnPassantTargetFile == targetFile
                    && PieceAt(startRank, targetFile) == PieceType.Pawn
                    && IsWhitePieceAt(startRank, targetFile) != isWhite)
                {
                    moves.Add(new ChessMove(startRank, startFile, nextRank, targetFile));
                }
            }
        }

        private static void AddPawnMove(List<ChessMove> moves, int startRank, int startFile, int endRank, int endFile, int promotionRank)
        {
            if (endRank == promotionRank)
            {
                foreach (PieceType promotion in new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight })
                {
                    moves.Add(new ChessMove(startRank, startFile, endRank, endFile, promotion));
                }
            }
            else
            {
                moves.Add(new ChessMove(startRank, startFile, endRank, endFile));
            }
        }

        private void AddStepMove(List<ChessMove> moves, int startRank, int startFile, int endRank, int endFile)
        {
            if (!IsInsideBoard(endRank, endFile))
            {
                return;
            }

            PieceType targetPiece = PieceAt(endRank, endFile);
            if (targetPiece == PieceType.Empty || (IsWhitePieceAt(endRank, endFile) != WhiteToMove && targetPiece != PieceType.King))
            {
                moves.Add(new ChessMove(startRank, startFile, endRank, endFile));
            }
        }

        private void AddSlidingMoves(List<ChessMove> moves, int startRank, int startFile, IEnumerable<(int Rank, int File)> directions)
        {
            foreach ((int rankOffset, int fileOffset) in directions)
            {
                int rank = startRank + rankOffset;
                int file = startFile + fileOffset;
                while (IsInsideBoard(rank, file))
                {
                    PieceType targetPiece = PieceAt(rank, file);
                    if (targetPiece == PieceType.Empty)
                    {
                        moves.Add(new ChessMove(startRank, startFile, rank, file));
                    }
                    else
                    {
                        if (IsWhitePieceAt(rank, file) != WhiteToMove && targetPiece != PieceType.King)
                        {
                            moves.Add(new ChessMove(startRank, startFile, rank, file));
                        }
                        break;
                    }

                    rank += rankOffset;
                    file += fileOffset;
                }
            }
        }

        private void AddCastlingMoves(List<ChessMove> moves, int startRank, int startFile, bool isWhite)
        {
            int homeRank = isWhite ? 1 : 8;
            if (startRank != homeRank || startFile != 5)
            {
                return;
            }

            char kingSideRight = isWhite ? 'K' : 'k';
            if (CastlingRights.Contains(kingSideRight)
                && PieceAt(homeRank, 8) == PieceType.Rook
                && IsWhitePieceAt(homeRank, 8) == isWhite
                && PieceAt(homeRank, 6) == PieceType.Empty
                && PieceAt(homeRank, 7) == PieceType.Empty)
            {
                moves.Add(new ChessMove(homeRank, 5, homeRank, 7));
            }

            char queenSideRight = isWhite ? 'Q' : 'q';
            if (CastlingRights.Contains(queenSideRight)
                && PieceAt(homeRank, 1) == PieceType.Rook
                && IsWhitePieceAt(homeRank, 1) == isWhite
                && PieceAt(homeRank, 2) == PieceType.Empty
                && PieceAt(homeRank, 3) == PieceType.Empty
                && PieceAt(homeRank, 4) == PieceType.Empty)
            {
                moves.Add(new ChessMove(homeRank, 5, homeRank, 3));
            }
        }

        private bool IsPathClear(int startRank, int startFile, int endRank, int endFile)
        {
            int rankStep = Math.Sign(endRank - startRank);
            int fileStep = Math.Sign(endFile - startFile);
            int rank = startRank + rankStep;
            int file = startFile + fileStep;
            while (rank != endRank || file != endFile)
            {
                if (PieceAt(rank, file) != PieceType.Empty)
                {
                    return false;
                }

                rank += rankStep;
                file += fileStep;
            }

            return true;
        }

        private static bool IsInsideBoard(int rank, int file)
        {
            return rank is >= 1 and <= 8 && file is >= 1 and <= 8;
        }

        public void AddPiece(bool isWhite, PieceType pieceType, int rank, int file)
        {
            int index = GetBitIndex(rank, file);
            int bitBoardIndex = MapBitBoardIndex(isWhite, pieceType);

            bitboards[bitBoardIndex] |= (1UL << index);
        }

        public void RemovePiece(int rank, int file)
        {
            int index = GetBitIndex(rank, file);
            ulong mask = ~(1UL << index);
            for (int i = 0; i < bitboards.Length; i++)
            {
                bitboards[i] &= mask;
            }
        }

        private static int GetBitIndex(int rank, int file)
        {
            if (rank < 1 || rank > 8)
                throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be in the range [1, 8]");

            if (file < 1 || file > 8)
                throw new ArgumentOutOfRangeException(nameof(file), "File must be in the range [1, 8]");

            return (8 * (rank - 1) + (file - 1));
        }

        private static PieceType GetPieceTypeFromBitBoardIndex(int index)
        {
            return (index % 6) switch
            {
                0 => PieceType.Pawn,
                1 => PieceType.Queen,
                2 => PieceType.King,
                3 => PieceType.Bishop,
                4 => PieceType.Knight,
                5 => PieceType.Rook,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public string ToFen()
        {
            StringBuilder fenBuilder = new StringBuilder();

            for (int rank = 8; rank >= 1; rank--)
            {
                int emptySquares = 0;
                for (int file = 1; file <= 8; file++)
                {
                    PieceType piece = PieceAt(rank, file);
                    if (piece == PieceType.Empty)
                    {
                        emptySquares++;
                        continue;
                    }

                    if (emptySquares > 0)
                    {
                        fenBuilder.Append(emptySquares);
                        emptySquares = 0;
                    }

                    char symbol = GetFenCharacter(piece);
                    fenBuilder.Append(IsWhitePieceAt(rank, file) ? char.ToUpperInvariant(symbol) : symbol);
                }

                if (emptySquares > 0)
                {
                    fenBuilder.Append(emptySquares);
                }

                if (rank > 1)
                {
                    fenBuilder.Append('/');
                }
            }

            fenBuilder.Append(WhiteToMove ? " w " : " b ");
            fenBuilder.Append(string.IsNullOrEmpty(CastlingRights) ? "-" : CastlingRights);
            fenBuilder.Append(' ');
            if (EnPassantTargetRank.HasValue && EnPassantTargetFile.HasValue)
            {
                fenBuilder.Append((char)('a' + EnPassantTargetFile.Value - 1));
                fenBuilder.Append(EnPassantTargetRank.Value);
            }
            else
            {
                fenBuilder.Append('-');
            }

            fenBuilder.Append(' ').Append(HalfmoveClock).Append(' ').Append(FullmoveNumber);
            return fenBuilder.ToString();
        }

        private static char GetFenCharacter(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => 'p',
                PieceType.Knight => 'n',
                PieceType.Bishop => 'b',
                PieceType.Rook => 'r',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => throw new ArgumentOutOfRangeException(nameof(pieceType)),
            };
        }

        public PieceType PieceAt(int rank, int file)
        {
            int bitIndex = GetBitIndex(rank, file);
            ulong mask = (1UL << bitIndex);
            for (int i = 0; i < bitboards.Length; i++)
            {
                if ((bitboards[i] & mask) != 0)
                {
                    return GetPieceTypeFromBitBoardIndex(i);
                }
            }

            return PieceType.Empty;
        }

        public bool IsWhitePieceAt(int rank, int file)
        {
            int bitIndex = GetBitIndex(rank, file);
            ulong mask = (1UL << bitIndex);
            for (int i = 0; i < bitboards.Length; i++)
            {
                if ((bitboards[i] & mask) != 0)
                {
                    return i <= 5;
                }
            }

            return false;
        }

        public void PrintBoard()
        {
            for (int rank = 8; rank >= 1; rank--)
            {
                for (int file = 1; file <= 8; file++)
                {
                    PieceType piece = PieceAt(rank, file);
                    Console.Write($"{piece,-10}");
                }

                Console.WriteLine();
            }
        }
    }
}
