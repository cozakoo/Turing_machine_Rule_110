using Rule110.Turing;

namespace Rule110.Verification;

/// <summary>
/// Verificación cruzada entre la Máquina de Turing y la implementación de
/// referencia de la Regla 110.
/// </summary>
public static class Rule110CrossValidator
{
    public const int DefaultSeed = 20260916;

    public static CrossValidationResult Validate(int iterations = 100, int seed = DefaultSeed)
    {
        var random = new Random(seed);

        for (var attempt = 0; attempt < iterations; attempt++)
        {
            var cells = 8 + random.Next(25);
            var generations = random.Next(6);
            var buffer = new char[cells];

            for (var index = 0; index < cells; index++)
            {
                buffer[index] = random.Next(2) == 0 ? '0' : '1';
            }

            var input = $"{Convert.ToString(generations, 2)}#{new string(buffer)}#";
            var parsed = TapeInput.Parse(input, requireMinimumCells: false);
            var machine = Rule110TuringMachine.Simulate(parsed);
            var reference = Rule110Oracle.Simulate(parsed);

            if (machine.Generations.Count != reference.Generations.Count)
            {
                return new CrossValidationResult(false, attempt, input, "cantidad de generaciones");
            }

            for (var index = 0; index < machine.Generations.Count; index++)
            {
                if (machine.Generations[index].Cells != reference.Generations[index].Cells)
                {
                    return new CrossValidationResult(false, attempt, input, $"generación {index}");
                }
            }
        }

        return new CrossValidationResult(true, iterations, string.Empty, string.Empty);
    }
}

public sealed record CrossValidationResult(
    bool Matches,
    int Cases,
    string FailingInput,
    string Detail);
