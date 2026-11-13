namespace Rule110;

public sealed record Generation(int Time, string Cells);

public sealed record SimulationResult(
    int Iterations,
    IReadOnlyList<Generation> Generations);

public static class Rule110Machine
{
    public static SimulationResult Simulate(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parts = input.Split('#');
        if (parts.Length != 3 || parts[2].Length != 0)
        {
            throw new FormatException(
                "El formato debe ser K_binario#celdas_binarias#.");
        }

        var iterations = ParseBinaryInteger(parts[0]);
        var initialCells = parts[1];

        if (initialCells.Length < 8)
        {
            throw new ArgumentException(
                "La cinta debe contener al menos 8 celdas.", nameof(input));
        }

        if (initialCells.Any(cell => cell is not ('0' or '1')))
        {
            throw new FormatException("Las celdas deben contener solo 0 y 1.");
        }

        var generations = new List<Generation>(checked(iterations + 1))
        {
            new(0, initialCells)
        };
        var current = initialCells;

        for (var time = 1; time <= iterations; time++)
        {
            current = NextGeneration(current);
            generations.Add(new Generation(time, current));
        }

        return new SimulationResult(iterations, generations.AsReadOnly());
    }

    private static int ParseBinaryInteger(string value)
    {
        if (value.Length == 0 || value.Any(bit => bit is not ('0' or '1')))
        {
            throw new FormatException("K debe ser un entero binario no vacío.");
        }

        var result = 0;
        foreach (var bit in value)
        {
            result = checked(result * 2 + (bit - '0'));
        }

        return result;
    }

    private static string NextGeneration(string cells)
    {
        var next = new char[cells.Length];

        for (var i = 0; i < cells.Length; i++)
        {
            var left = cells[(i - 1 + cells.Length) % cells.Length];
            var center = cells[i];
            var right = cells[(i + 1) % cells.Length];

            next[i] = (left, center, right) switch
            {
                ('1', '1', '1') => '0',
                ('1', '1', '0') => '1',
                ('1', '0', '1') => '1',
                ('1', '0', '0') => '0',
                ('0', '1', '1') => '1',
                ('0', '1', '0') => '1',
                ('0', '0', '1') => '1',
                ('0', '0', '0') => '0',
                _ => throw new InvalidOperationException("Estado de celda inválido.")
            };
        }

        return new string(next);
    }
}
