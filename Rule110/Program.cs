using Rule110;
using Rule110.Turing;
using Rule110.Verification;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/turing/simulate", (SimulateRequest request, HttpContext context) =>
{
    if (string.IsNullOrEmpty(request.TapeInput))
    {
        return Results.Ok(new SimulateResponse(
            false,
            "tapeInput es obligatorio.",
            [],
            null));
    }

    var includeTrace = context.Request.Query["trace"].ToString() is "1" or "true" or "True";

    try
    {
        var parsed = TapeInput.Parse(request.TapeInput);
        var result = Rule110TuringMachine.Simulate(parsed, traceLimit: includeTrace ? 4000 : 0);
        var steps = result.Generations
            .Select(generation => new SimulationStep(generation.Time, generation.Cells))
            .ToList();

        if (!result.WithinLimit)
        {
            return Results.Ok(new SimulateResponse(
                false,
                "La máquina no alcanzó el estado de aceptación dentro del límite de pasos.",
                [],
                null));
        }

        var trace = includeTrace
            ? result.Trace
                .Select(step => new TraceStep(
                    step.Index,
                    step.State,
                    step.HeadPosition,
                    step.ReadSymbol,
                    step.NextState,
                    step.WrittenSymbol,
                    step.Move == HeadMove.Left ? "L" : "R"))
                .ToList()
            : null;

        return Results.Ok(new SimulateResponse(
            true,
            string.Empty,
            steps,
            new MachineSummary(
                result.FinalState,
                result.Steps,
                result.Generations.Count,
                trace,
                result.FinalTape
                    .Select(entry => new TapeCell(entry.Position, entry.Symbol))
                    .ToList())));
    }
    catch (FormatException exception)
    {
        return Results.Ok(new SimulateResponse(false, exception.Message, [], null));
    }
    catch (ArgumentException exception)
    {
        return Results.Ok(new SimulateResponse(false, exception.Message, [], null));
    }
    catch (OverflowException)
    {
        return Results.Ok(new SimulateResponse(
            false,
            "K excede el máximo entero soportado (Int32).",
            [],
            null));
    }
});

app.MapGet("/api/turing/table", () =>
{
    var table = Rule110TuringMachine.Table;

    return Results.Ok(new TransitionTableResponse(
        "M = (Q, Σ, Γ, δ, q₀, q_acc, q_rej)",
        new AlphabetDescription(
            "Σ = {0, 1, #}",
            "Γ = {0, 1, #, U, ⊢, _, x, y, ·}",
            "q₀ = q_init",
            "q_acc = q_acc (parada por agotamiento del contador unario)",
            "q_rej = q_rej (entrada malformada)"),
        table.Rows
            .Select(row => new TransitionRowResponse(
                row.State,
                row.Read,
                row.NextState,
                row.Write,
                row.Move == HeadMove.Left ? "L" : "R",
                row.Module))
            .ToList()));
});

app.MapGet("/api/turing/tests", () =>
{
    var report = Rule110TestCatalog.Run();

    return Results.Ok(new TestReportResponse(
        report.Total - report.Failed,
        report.Total,
        report.Failed,
        report.Lines));
});

app.Run();

public sealed record SimulateRequest(string? TapeInput);

public sealed record SimulateResponse(
    bool IsValid,
    string ErrorMessage,
    List<SimulationStep> Steps,
    MachineSummary? Machine);

public sealed record SimulationStep(int Generation, string TapeState);

public sealed record MachineSummary(
    string FinalState,
    int Steps,
    int Generations,
    List<TraceStep>? Trace,
    List<TapeCell> Tape);

public sealed record TraceStep(
    int Step,
    string State,
    int HeadPosition,
    char ReadSymbol,
    string NextState,
    char WrittenSymbol,
    string Move);

public sealed record TapeCell(int Position, char Symbol);

public sealed record TransitionTableResponse(
    string Definition,
    AlphabetDescription Alphabet,
    List<TransitionRowResponse> Rows);

public sealed record AlphabetDescription(
    string InputAlphabet,
    string TapeAlphabet,
    string InitialState,
    string AcceptState,
    string RejectState);

public sealed record TransitionRowResponse(
    string State,
    char Read,
    string NextState,
    char Write,
    string Move,
    string Module);

public sealed record TestReportResponse(
    int Passed,
    int Total,
    int Failed,
    IReadOnlyList<string> Lines);
