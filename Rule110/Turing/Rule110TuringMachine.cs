namespace Rule110.Turing;

/// <summary>
/// Resultado de ejecutar la Máquina de Turing de la Regla 110.
/// </summary>
public sealed record TuringSimulationResult(
    int Iterations,
    IReadOnlyList<Generation> Generations,
    string FinalState,
    int Steps,
    bool Halted,
    bool WithinLimit,
    IReadOnlyList<MachineStep> Trace,
    IReadOnlyList<(int Position, char Symbol)> FinalTape);

/// <summary>
/// Fachada de la Máquina de Turing: prepara la cinta, ejecuta la tabla δ y
/// extrae las generaciones que quedaron registradas en ella.
/// </summary>
public static class Rule110TuringMachine
{
    public const int DefaultStepLimit = 40_000_000;

    private static TransitionTable? _table;

    public static TransitionTable Table => _table ??= Rule110TuringProgram.BuildTable();

    public static TuringSimulationResult Simulate(
        string input,
        int traceLimit = 4000,
        int stepLimit = DefaultStepLimit)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parsed = TapeInput.Parse(input, requireMinimumCells: false);
        return Simulate(parsed, traceLimit, stepLimit);
    }

    public static TuringSimulationResult Simulate(
        TapeInput parsed,
        int traceLimit = 4000,
        int stepLimit = DefaultStepLimit)
    {
        ArgumentNullException.ThrowIfNull(parsed);

        var tape = new Tape();
        tape.Write(0, Rule110TuringProgram.LeftEnd);

        var word = parsed.Word;
        for (var index = 0; index < word.Length; index++)
        {
            tape.Write(index + 1, word[index]);
        }

        var machine = new TuringMachine(
            Table,
            tape,
            headPosition: 1,
            initialState: Rule110TuringProgram.InitialState,
            stepLimit: stepLimit,
            traceLimit: traceLimit);

        var run = machine.Run();
        var generations = ExtractGenerations(tape);
        var withinLimit = run.Halted && run.Steps < stepLimit;

        return new TuringSimulationResult(
            parsed.Iterations,
            generations,
            run.FinalState,
            run.Steps,
            run.Halted,
            withinLimit,
            run.Trace,
            tape.InOrder().ToArray());
    }

    /// <summary>
    /// Las generaciones quedan registradas en la cinta como bloques binarios
    /// separados por el delimitador '#'.
    /// </summary>
    private static IReadOnlyList<Generation> ExtractGenerations(Tape tape)
    {
        var separators = tape.Cells
            .Where(entry => entry.Value == Rule110TuringProgram.Separator)
            .Select(entry => entry.Key)
            .OrderBy(static position => position)
            .ToArray();

        var generations = new List<Generation>(separators.Length);

        for (var index = 0; index + 1 < separators.Length; index++)
        {
            var start = separators[index] + 1;
            var length = separators[index + 1] - separators[index] - 1;
            generations.Add(new Generation(index, tape.ReadRange(start, length)));
        }

        return generations;
    }
}
