using Rule110;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/turing/simulate", (SimulateRequest request) =>
{
    if (string.IsNullOrEmpty(request.TapeInput))
    {
        return Results.Ok(new SimulateResponse(
            false,
            "tapeInput es obligatorio.",
            []));
    }

    try
    {
        var result = Rule110Machine.Simulate(request.TapeInput);
        var steps = result.Generations
            .Select(generation => new SimulationStep(generation.Time, generation.Cells))
            .ToList();

        return Results.Ok(new SimulateResponse(true, string.Empty, steps));
    }
    catch (FormatException exception)
    {
        return Results.Ok(new SimulateResponse(false, exception.Message, []));
    }
    catch (ArgumentException exception)
    {
        return Results.Ok(new SimulateResponse(false, exception.Message, []));
    }
    catch (OverflowException)
    {
        return Results.Ok(new SimulateResponse(
            false,
            "K excede el máximo entero soportado (Int32).",
            []));
    }
});

app.Run();

public sealed record SimulateRequest(string? TapeInput);

public sealed record SimulateResponse(
    bool IsValid,
    string ErrorMessage,
    List<SimulationStep> Steps);

public sealed record SimulationStep(int Generation, string TapeState);
