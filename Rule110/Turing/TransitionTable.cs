namespace Rule110.Turing;

public enum HeadMove
{
    Left = -1,
    Right = 1
}

public sealed record TransitionRow(
    string State,
    char Read,
    string NextState,
    char Write,
    HeadMove Move,
    string Module);

public sealed class TransitionTable
{
    private readonly Dictionary<(string State, char Symbol), Transition> _transitions = new();

    public TransitionTable(IEnumerable<TransitionRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            var key = (row.State, row.Read);
            if (!_transitions.TryAdd(key, new Transition(row.NextState, row.Write, row.Move)))
            {
                throw new InvalidOperationException(
                    $"Transición duplicada para el estado '{row.State}' y el símbolo '{row.Read}'.");
            }
        }

        Rows = rows.ToArray();
    }

    public IReadOnlyList<TransitionRow> Rows { get; }

    public int Count => Rows.Count;

    public bool TryGet(string state, char symbol, out Transition transition) =>
        _transitions.TryGetValue((state, symbol), out transition!);

    public string Describe()
    {
        var lines = new List<string>
        {
            "TABLA DE TRANSICIONES δ : Q × Γ → Q × Γ × {L,R}",
            "M = (Q, Σ, Γ, δ, q0, q_acc, q_rej)",
            string.Empty
        };

        foreach (var module in Rows.Select(static row => row.Module).Distinct())
        {
            lines.Add($"--- MÓDULO {module} ---");
            lines.Add("Estado | Lee | → Estado | Escribe | Movimiento");
            foreach (var row in Rows.Where(row => row.Module == module))
            {
                lines.Add(
                    $"{row.State} | {row.Read} | {row.NextState} | {row.Write} | {row.Move}");
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public readonly record struct Transition(string NextState, char Write, HeadMove Move);
