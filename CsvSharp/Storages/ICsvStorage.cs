namespace CsvSharp.Storages;

internal interface ICsvStorage
{
    int Rows { get; }
    int Columns { get; }
    public float Density { get; }
    string? Get(int row, int column);
    void Set(int row, int column, string? value);
}