namespace Rule110;

public sealed record Generation(int Time, string Cells);

public sealed record SimulationResult(
    int Iterations,
    IReadOnlyList<Generation> Generations);
