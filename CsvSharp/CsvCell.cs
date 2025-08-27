namespace CsvSharp;

internal readonly ref struct CsvCell(int row, int column, ReadOnlySpan<char> value)
{
    public int Row { get; } = row;
    public int Column { get; } = column;
    public ReadOnlySpan<char> Value { get; } = value;
}