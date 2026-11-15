namespace Rule110.Turing;

public sealed record MachineStep(
    int Index,
    string State,
    int HeadPosition,
    char ReadSymbol,
    string NextState,
    char WrittenSymbol,
    HeadMove Move);

public sealed record MachineRun(
    string FinalState,
    int HeadPosition,
    int Steps,
    bool Halted,
    IReadOnlyList<MachineStep> Trace);

public sealed class TuringMachine
{
    private readonly TransitionTable _table;
    private readonly List<MachineStep> _trace = new();
    private readonly int _traceLimit;

    public TuringMachine(
        TransitionTable table,
        Tape tape,
        int headPosition,
        string initialState,
        int stepLimit,
        int traceLimit = 4000)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        Tape = tape ?? throw new ArgumentNullException(nameof(tape));
        HeadPosition = headPosition;
        State = initialState ?? throw new ArgumentNullException(nameof(initialState));
        StepLimit = stepLimit;
        _traceLimit = traceLimit;
    }

    public Tape Tape { get; }

    public int HeadPosition { get; private set; }

    public string State { get; private set; }

    public int StepLimit { get; }

    public int Steps { get; private set; }

    public bool IsHalted => State is Rule110States.Accept or Rule110States.Reject;

    public MachineStep? Step()
    {
        if (IsHalted || Steps >= StepLimit)
        {
            return null;
        }

        if (!_table.TryGet(State, Tape.Read(HeadPosition), out var transition))
        {
            State = Rule110States.Reject;
            return null;
        }

        var readSymbol = Tape.Read(HeadPosition);
        Tape.Write(HeadPosition, transition.Write);

        var step = new MachineStep(
            Steps + 1,
            State,
            HeadPosition,
            readSymbol,
            transition.NextState,
            transition.Write,
            transition.Move);

        Steps++;
        State = transition.NextState;
        HeadPosition += (int)transition.Move;

        if (_trace.Count < _traceLimit)
        {
            _trace.Add(step);
        }

        return step;
    }

    public MachineRun Run()
    {
        while (Step() is not null)
        {
        }

        return new MachineRun(State, HeadPosition, Steps, IsHalted, _trace.ToArray());
    }
}

public static class Rule110States
{
    public const string Accept = "q_acc";
    public const string Reject = "q_rej";
}
