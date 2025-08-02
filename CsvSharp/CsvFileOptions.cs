namespace CsvSharp;

public class CsvFileOptions : IFormatProvider
{
    public bool HasHeader { get; init; } = false;
    public string ColumnSeparator { get; init; } = ";";
    public string Wrapper { get; init; } = "\"";

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
