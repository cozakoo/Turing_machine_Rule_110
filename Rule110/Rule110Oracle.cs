namespace Rule110;

/// <summary>
/// Implementación de referencia (oráculo) de la Regla 110 con frontera cíclica.
/// Se usa como criterio de contraste para validar la Máquina de Turing.
/// </summary>
public static class Rule110Oracle
{
    public static SimulationResult Simulate(TapeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var generations = new List<Generation>(checked(input.Iterations + 1))
        {
            new(0, input.Cells)
        };

        var current = input.Cells;

        for (var time = 1; time <= input.Iterations; time++)
        {
            current = NextGeneration(current);
            generations.Add(new Generation(time, current));
        }

        return new SimulationResult(input.Iterations, generations.AsReadOnly());
    }

    public static SimulationResult Simulate(string input) =>
        Simulate(TapeInput.Parse(input, requireMinimumCells: false));

    private static string NextGeneration(string cells)
    {
        var next = new char[cells.Length];

        for (var index = 0; index < cells.Length; index++)
        {
            var left = cells[(index - 1 + cells.Length) % cells.Length];
            var center = cells[index];
            var right = cells[(index + 1) % cells.Length];

            next[index] = (left, center, right) switch
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
