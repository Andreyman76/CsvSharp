namespace CsvSharp;

public class CsvFileOptions : IFormatProvider
{
    public bool HasHeader { get; init; } = false;
    public char ColumnSeparator { get; init; } = ';';
    public char Wrapper { get; init; } = '"';

    public static CsvFileOptions Default { get; } = new CsvFileOptions();

    public object? GetFormat(Type? formatType)
    {
        if (formatType == typeof(CsvFileOptions))
        {
            return this;
        }

        return null;
    }
}
