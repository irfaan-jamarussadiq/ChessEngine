using ChessEngine.Models;

namespace ChessEngine.Interfaces
{
    public interface IPositionEvaluator
    {
        PositionEvaluation Evaluate(Board board);
    }
}
