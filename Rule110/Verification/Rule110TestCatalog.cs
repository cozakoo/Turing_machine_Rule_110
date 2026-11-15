using Rule110.Turing;

namespace Rule110.Verification;

public sealed record Rule110TestCase(
    int Number,
    string Input,
    int Iterations,
    int CellCount,
    string ExpectedFinalState,
    string Description);

public sealed record Rule110InvalidCase(
    int Number,
    string Input,
    string Description);

public sealed record Rule110Report(IReadOnlyList<string> Lines, int Total, int Failed);

/// <summary>
/// Catálogo de casos de prueba del trabajo práctico: casos del enunciado,
/// casos borde, configuraciones periódicas y caóticas, entradas inválidas y
/// verificación cruzada contra la implementación de referencia.
/// </summary>
public static class Rule110TestCatalog
{
    public static IReadOnlyList<Rule110TestCase> Cases { get; } = new[]
    {
        new Rule110TestCase(
            1,
            "11#000100#",
            3,
            6,
            "110100",
            "Ejemplo del enunciado: 000100 -> 001100 -> 011100 -> 110100."),
        new Rule110TestCase(
            2,
            "100#000000010000000#",
            4,
            15,
            "000111110000000",
            "Ejemplo del enunciado (K = 4)."),
        new Rule110TestCase(
            3,
            "1110#01101001#",
            14,
            8,
            "01101110",
            "Ejemplo del enunciado (K = 14)."),
        new Rule110TestCase(
            4,
            "0#00000000#",
            0,
            8,
            "00000000",
            "K = 0: la máquina se detiene sin calcular generaciones."),
        new Rule110TestCase(
            5,
            "1#00000000#",
            1,
            8,
            "00000000",
            "K = 1 sobre una cinta uniforme de ceros."),
        new Rule110TestCase(
            6,
            "1#11111111#",
            1,
            8,
            "00000000",
            "Cinta uniforme de unos (111 -> 0)."),
        new Rule110TestCase(
            7,
            "101#11111111#",
            5,
            8,
            "00000000",
            "Cinta uniforme de unos con 5 pasos: se estabiliza en ceros."),
        new Rule110TestCase(
            8,
            "1#00000001#",
            1,
            8,
            "00000011",
            "Un único 1: propagación del borde cíclico."),
        new Rule110TestCase(
            9,
            "1000#01010101#",
            8,
            8,
            "00000000",
            "Patrón periódico alternado (período 2)."),
        new Rule110TestCase(
            10,
            "11#00010000#",
            3,
            8,
            "11010000",
            "Ejemplo del enunciado extendido a N = 8."),
        new Rule110TestCase(
            11,
            "1#10101010#",
            1,
            8,
            "11111111",
            "Patrón alternado: cada vecindad 101 o 010 produce 1."),
        new Rule110TestCase(
            12,
            "110#000000010000000100000001#",
            6,
            24,
            "011100110111001101110011",
            "Tres 1 separados en una cinta de 24 celdas."),
        new Rule110TestCase(
            13,
            "1000#110011001100110011001100#",
            8,
            24,
            "011101110111011101110111",
            "Patrón de período 4 en una cinta de 24 celdas."),
        new Rule110TestCase(
            14,
            "1111#000100010001000100010001#",
            15,
            24,
            "110111011101110111011101",
            "Configuración caótica de 24 celdas."),
        new Rule110TestCase(
            15,
            "100000#00000000000000000000000000000001#",
            32,
            32,
            "01111110110111110101101000011111",
            "Cinta de 32 celdas con un único 1 y 32 pasos."),
        new Rule110TestCase(
            16,
            "1010#0110100110010110100110010110#",
            10,
            28,
            "1111101111101001101111101110",
            "Configuración pseudoaleatoria de 28 celdas."),
        new Rule110TestCase(
            17,
            "1#1111111100000000#",
            1,
            16,
            "1000000100000001",
            "Dos bloques uniforme: verificación de la frontera cíclica."),
        new Rule110TestCase(
            18,
            "11#00000000000000000000000000000001#",
            3,
            32,
            "00000000000000000000000000001101",
            "Cinta de 32 celdas con un único 1 en el extremo."),
        new Rule110TestCase(
            19,
            "111#0110100110010110100110010110#",
            7,
            28,
            "1110111110111010111110111011",
            "Configuración caótica de 28 celdas con 7 pasos."),
        new Rule110TestCase(
            20,
            "10#1010101010101010101010101010#",
            2,
            28,
            "0000000000000000000000000000",
            "Patrón periódico alternado de 28 celdas con 2 pasos.")
    };

    public static IReadOnlyList<Rule110InvalidCase> InvalidCases { get; } = new[]
    {
        new Rule110InvalidCase(21, "100#0101#", "N < 8: la consigna exige al menos 8 celdas."),
        new Rule110InvalidCase(22, "10#0000000A#", "Celda no binaria."),
        new Rule110InvalidCase(23, "1O#00000000#", "K no binario."),
        new Rule110InvalidCase(24, "100##", "Segmentos vacíos."),
        new Rule110InvalidCase(25, "100#00000000", "Falta el delimitador final."),
        new Rule110InvalidCase(
            26,
            "11111111111111111111111111111111#00000000#",
            "K fuera del rango de Int32.")
    };

    public static Rule110Report Run()
    {
        var lines = new List<string>(Cases.Count + InvalidCases.Count + 1);
        var failed = 0;

        var cross = Rule110CrossValidator.Validate();
        lines.Add(
            cross.Matches
                ? $"Caso 0 | Verificación cruzada Máquina de Turing vs. implementación de referencia | casos_aleatorios={cross.Cases} | semilla={Rule110CrossValidator.DefaultSeed} | resultado=OK"
                : $"Caso 0 | Verificación cruzada Máquina de Turing vs. implementación de referencia | resultado=ERROR: {cross.Detail} en {cross.FailingInput}");

        if (!cross.Matches)
        {
            failed++;
        }

        foreach (var testCase in Cases)
        {
            try
            {
                var parsed = TapeInput.Parse(testCase.Input, requireMinimumCells: false);
                var machine = Rule110TuringMachine.Simulate(parsed);
                var reference = Rule110Oracle.Simulate(parsed);
                Validate(testCase, machine, reference);

                lines.Add(
                    $"Caso {testCase.Number} | entrada={testCase.Input} | K={testCase.Iterations} | N={testCase.CellCount} | generaciones={machine.Generations.Count} | estado_final={machine.Generations[^1].Cells} | pasos_MT={machine.Steps} | resultado=OK | {testCase.Description}");
            }
            catch (Exception exception) when (
                exception is ArgumentException or FormatException or InvalidOperationException)
            {
                failed++;
                lines.Add(
                    $"Caso {testCase.Number} | entrada={testCase.Input} | resultado=ERROR: {exception.Message}");
            }
        }

        foreach (var invalidCase in InvalidCases)
        {
            try
            {
                _ = TapeInput.Parse(invalidCase.Input);
                failed++;
                lines.Add(
                    $"Caso {invalidCase.Number} | entrada={invalidCase.Input} | resultado=ERROR: se esperaba una entrada inválida y fue aceptada");
            }
            catch (Exception exception) when (
                exception is ArgumentException or FormatException or OverflowException)
            {
                lines.Add(
                    $"Caso {invalidCase.Number} | entrada={invalidCase.Input} | resultado=OK (entrada rechazada: {exception.Message}) | {invalidCase.Description}");
            }
        }

        return new Rule110Report(lines, Cases.Count + InvalidCases.Count + 1, failed);
    }

    private static void Validate(
        Rule110TestCase testCase,
        TuringSimulationResult machine,
        SimulationResult reference)
    {
        if (!machine.WithinLimit || machine.FinalState != Rule110TuringProgram.AcceptState)
        {
            throw new InvalidOperationException(
                $"La máquina no alcanzó '{Rule110TuringProgram.AcceptState}' (estado final: {machine.FinalState}).");
        }

        if (machine.Generations.Count != testCase.Iterations + 1)
        {
            throw new InvalidOperationException(
                $"Se esperaban {testCase.Iterations + 1} generaciones, se obtuvieron {machine.Generations.Count}.");
        }

        if (reference.Generations.Count != machine.Generations.Count)
        {
            throw new InvalidOperationException(
                "La cantidad de generaciones de la máquina difiere de la referencia.");
        }

        for (var index = 0; index < machine.Generations.Count; index++)
        {
            var generation = machine.Generations[index];
            var expected = reference.Generations[index];

            if (generation.Time != index)
            {
                throw new InvalidOperationException(
                    $"La generación en posición {index} tiene tiempo {generation.Time}.");
            }

            if (generation.Cells.Length != testCase.CellCount ||
                generation.Cells.Any(cell => cell is not ('0' or '1')))
            {
                throw new InvalidOperationException(
                    $"El estado de la generación {index} no tiene {testCase.CellCount} celdas binarias.");
            }

            if (generation.Cells != expected.Cells)
            {
                throw new InvalidOperationException(
                    $"La generación {index} difiere de la referencia: MT='{generation.Cells}' ref='{expected.Cells}'.");
            }
        }

        var finalState = machine.Generations[^1].Cells;
        if (finalState != testCase.ExpectedFinalState)
        {
            throw new InvalidOperationException(
                $"El estado final '{finalState}' difiere del esperado '{testCase.ExpectedFinalState}'.");
        }
    }
}
