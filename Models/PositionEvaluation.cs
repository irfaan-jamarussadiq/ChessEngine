namespace ChessEngine.Models
{
    public sealed record PositionEvaluation(
        int Centipawns,
        int CompletedDepth,
        int Nodes,
        IReadOnlyList<PrincipalVariation> TopLines);

    public sealed record PrincipalVariation(int Centipawns, IReadOnlyList<string> Moves);
}
