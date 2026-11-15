namespace Rule110;

/// <summary>
/// Entrada de la cinta: K en binario y la configuración inicial de N celdas.
/// </summary>
public sealed record TapeInput(int Iterations, string Cells, string BinaryCounter)
{
    public const int MinimumCells = 8;

    /// <summary>
    /// Palabra de entrada tal como se escribe en la cinta: K_binario + # + celdas + #.
    /// </summary>
    public string Word => $"{BinaryCounter}#{Cells}#";

    public static TapeInput Parse(string input, bool requireMinimumCells = true)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parts = input.Split('#');
        if (parts.Length != 3 || parts[2].Length != 0)
        {
            throw new FormatException(
                "El formato debe ser K_binario#celdas_binarias#.");
        }

        var iterations = ParseBinaryInteger(parts[0]);
        var cells = parts[1];

        if (cells.Length == 0)
        {
            throw new FormatException("La cinta debe contener al menos una celda.");
        }

        if (requireMinimumCells && cells.Length < MinimumCells)
        {
            throw new ArgumentException(
                $"La cinta debe contener al menos {MinimumCells} celdas.");
        }

        if (cells.Any(cell => cell is not ('0' or '1')))
        {
            throw new FormatException("Las celdas deben contener solo 0 y 1.");
        }

        return new TapeInput(iterations, cells, parts[0]);
    }

    public static int ParseBinaryInteger(string value)
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
}
