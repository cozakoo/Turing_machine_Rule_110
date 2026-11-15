using Rule110.Turing;

namespace Rule110.Verification;

/// <summary>
/// Utilidades de diagnóstico para inspeccionar la ejecución de la máquina.
/// </summary>
public static class Rule110Diagnostics
{
    public static string Describe(string input, int steps = 80)
    {
        var parsed = TapeInput.Parse(input, requireMinimumCells: false);
        var result = Rule110TuringMachine.Simulate(parsed, traceLimit: steps);

        var lines = new List<string>
        {
            $"Entrada: {input}",
            $"Estado final: {result.FinalState} | pasos: {result.Steps} | generaciones: {result.Generations.Count}",
            $"Cinta: {string.Join(" ", result.FinalTape.Select(cell => $"{cell.Position}:'{cell.Symbol}'"))}"
        };

        foreach (var step in result.Trace)
        {
            lines.Add(
                $"{step.Index,4} | {step.State,-34} | cabeza={step.HeadPosition,4} | lee='{step.ReadSymbol}' | escribe='{step.WrittenSymbol}' | {step.Move} | -> {step.NextState}");
        }

        foreach (var generation in result.Generations)
        {
            lines.Add($"Gen {generation.Time}: {generation.Cells}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
