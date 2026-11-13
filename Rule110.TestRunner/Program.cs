using Rule110;

var cases = new[]
{
    new TestCase(1, "11#00010000#", 3, 8),
    new TestCase(2, "100#000000010000000#", 4, 15),
    new TestCase(3, "1110#01101001#", 14, 8),
    new TestCase(4, "1#10000001#", 1, 8),
    new TestCase(5, "101#0000000000#", 5, 10)
};

var reportLines = new List<string>(cases.Length);
var failedCases = 0;

foreach (var testCase in cases)
{
    try
    {
        var result = Rule110Machine.Simulate(testCase.Input);
        Validate(testCase, result);

        var finalState = result.Generations[^1].Cells;
        reportLines.Add(
            $"Caso {testCase.Number} | entrada={testCase.Input} | K={testCase.Iterations} | N={testCase.CellCount} | generaciones={result.Generations.Count} | estado_final={finalState} | resultado=OK");
    }
    catch (Exception exception) when (
        exception is ArgumentException or FormatException or InvalidOperationException)
    {
        failedCases++;
        reportLines.Add(
            $"Caso {testCase.Number} | entrada={testCase.Input} | K={testCase.Iterations} | N={testCase.CellCount} | resultado=ERROR: {exception.Message}");
    }
}

var outputPath = Path.GetFullPath("resultados-regla110.txt");
File.WriteAllLines(outputPath, reportLines);

Console.WriteLine($"Reporte exportado: {outputPath}");
Console.WriteLine($"Casos correctos: {cases.Length - failedCases}/{cases.Length}");

return failedCases == 0 ? 0 : 1;

static void Validate(TestCase testCase, SimulationResult result)
{
    if (result.Iterations != testCase.Iterations)
    {
        throw new InvalidOperationException(
            $"Se esperaban {testCase.Iterations} iteraciones, se obtuvieron {result.Iterations}.");
    }

    if (result.Generations.Count != testCase.Iterations + 1)
    {
        throw new InvalidOperationException(
            $"Se esperaban {testCase.Iterations + 1} generaciones, se obtuvieron {result.Generations.Count}.");
    }

    var initialCells = testCase.Input.Split('#')[1];
    for (var index = 0; index < result.Generations.Count; index++)
    {
        var generation = result.Generations[index];
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
    }

    if (result.Generations[0].Cells != initialCells)
    {
        throw new InvalidOperationException("La generación inicial no coincide con la cinta recibida.");
    }
}

internal sealed record TestCase(
    int Number,
    string Input,
    int Iterations,
    int CellCount);
