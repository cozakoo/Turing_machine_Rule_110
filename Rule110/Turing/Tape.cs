namespace Rule110.Turing;

public sealed class Tape
{
    public const char Blank = '·';

    private readonly Dictionary<int, char> _cells = new();

    public char Read(int position) =>
        _cells.TryGetValue(position, out var symbol) ? symbol : Blank;

    public void Write(int position, char symbol)
    {
        if (symbol == Blank)
        {
            _cells.Remove(position);
            return;
        }

        _cells[position] = symbol;
    }

    public IReadOnlyDictionary<int, char> Cells => _cells;

    public string ReadRange(int start, int length)
    {
        var buffer = new char[length];
        for (var offset = 0; offset < length; offset++)
        {
            buffer[offset] = Read(start + offset);
        }

        return new string(buffer);
    }

    public IEnumerable<(int Position, char Symbol)> InOrder()
    {
        foreach (var position in _cells.Keys.OrderBy(static value => value))
        {
            yield return (position, _cells[position]);
        }
    }

    public string Render(int from, int to)
    {
        var parts = new List<string>();
        for (var position = from; position <= to; position++)
        {
            var symbol = Read(position);
            parts.Add(symbol == Blank ? "_" : symbol.ToString());
        }

        return string.Concat(parts);
    }
}
