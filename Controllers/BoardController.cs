using ChessEngine.Interfaces;
using ChessEngine.Models;
using ChessEngine.Types;
using ChessEngine.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChessEngine.Controllers
{
    public class BoardController : Controller
    {
        private readonly IBoardService _boardService;
        private readonly IPositionEvaluator _positionEvaluator;

        public BoardController(IBoardService boardService, IPositionEvaluator positionEvaluator)
        {
            _boardService = boardService;
            _positionEvaluator = positionEvaluator;
        }

        [HttpPost]
        public IActionResult Evaluate([FromBody] PositionEvaluationRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Fen))
            {
                return BadRequest(new { error = "A FEN position is required" });
            }

            try
            {
                Board board = _boardService.FromFen(request.Fen);
                PositionEvaluation evaluation = _positionEvaluator.Evaluate(board);
                return Ok(new
                {
                    centipawns = evaluation.Centipawns,
                    depth = evaluation.CompletedDepth,
                    nodes = evaluation.Nodes,
                    lines = evaluation.TopLines.Select(line => new
                    {
                        centipawns = line.Centipawns,
                        moves = line.Moves
                    })
                });
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        public IActionResult Play()
        {
            BoardViewModel vm = new() { 
                FenString = Board.StartingPosition, 
                BoardSize = 64,
            };

            Board board = _boardService.FromFen(Board.StartingPosition);
            vm.IsCheck = board.IsKingInCheck(board.WhiteToMove);
            bool hasLegalMoves = board.HasAnyLegalMoves();
            vm.IsCheckmate = vm.IsCheck && !hasLegalMoves;
            vm.IsStalemate = !vm.IsCheck && !hasLegalMoves;
            var checkedKing = vm.IsCheck ? board.GetKingSquare(board.WhiteToMove) : null;
            vm.CheckedKingRank = checkedKing?.Rank;
            vm.CheckedKingFile = checkedKing?.File;
            ICollection<SquareViewModel> squares = new List<SquareViewModel>(64);
            for (int rank = 8; rank >= 1; rank--)
            {
                for (int file = 1; file <= 8; file++)
                {
                    var pieceType = _boardService.PieceAt(rank, file);
                    bool isWhite = _boardService.IsWhitePieceAt(rank, file);
                    SquareViewModel square = new()
                    {
                        Rank = rank,
                        File = file,
                        Piece = pieceType == PieceType.Empty ? null : new PieceViewModel
                        {
                            Name = (isWhite ? "white_" : "black_") + pieceType.ToString().ToLower(),
                            Color = isWhite ? "white" : "black"
                        }
                    };
                    squares.Add(square);
                }

            }

            vm.Squares = squares;
            return View(vm);
        }

        [HttpPost]
        public IActionResult LegalMoves([FromBody] LegalMovesRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Fen))
            {
                return BadRequest(new { error = "A FEN position is required" });
            }

            try
            {
                _boardService.FromFen(request.Fen);
                var moves = _boardService.LegalMovesFrom(request.Rank, request.File)
                    .Select(move => new
                    {
                        rank = move.EndRank,
                        file = move.EndFile,
                        isCapture = _boardService.PieceAt(move.EndRank, move.EndFile) != PieceType.Empty
                            || (_boardService.PieceAt(move.StartRank, move.StartFile) == PieceType.Pawn
                                && move.StartFile != move.EndFile),
                        promotion = move.Promotion?.ToString().ToLowerInvariant()
                    });
                return Ok(moves);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        [HttpPost]
        public IActionResult MakeMove([FromBody] MakeMoveRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Fen))
            {
                return BadRequest(new { error = "A FEN position is required" });
            }

            try
            {
                Board position = _boardService.FromFen(request.Fen);
                bool isWhiteMove = position.WhiteToMove;
                int moveNumber = position.FullmoveNumber;
                string san = _boardService.MakeMove(request.StartRank, request.StartFile, request.EndRank, request.EndFile, request.Promotion);
                bool isCheck = position.IsKingInCheck(position.WhiteToMove);
                bool hasLegalMoves = position.HasAnyLegalMoves();
                var checkedKing = isCheck ? position.GetKingSquare(position.WhiteToMove) : null;
                return Ok(new
                {
                    fen = _boardService.ToFen(),
                    san,
                    moveNumber,
                    isWhiteMove,
                    isCheck,
                    isCheckmate = isCheck && !hasLegalMoves,
                    isStalemate = !isCheck && !hasLegalMoves,
                    checkedKingRank = checkedKing?.Rank,
                    checkedKingFile = checkedKing?.File
                });
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return BadRequest(new { error = exception.Message });
            }
        }
    }

    public sealed record PositionEvaluationRequest(string Fen);
}
