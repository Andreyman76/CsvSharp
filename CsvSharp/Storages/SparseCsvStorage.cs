namespace CsvSharp.Storages;

internal class SparseCsvStorage : ICsvStorage
{
    public int Rows { get; private set; }
    public int Columns { get; private set; }

    public float Density
    {
        get
        {
            var total = Rows * Columns;

            if (total == 0)
            {
                return 0f;
            }

            return (float)_data.Count / total;
        }
    }

    private readonly Dictionary<CellPosition, string?> _data;

    public SparseCsvStorage()
    {
        _data = [];
    }

    public SparseCsvStorage(ICsvStorage storage) : this()
    {
        for (int row = 0; row < storage.Rows; row++)
        {
            for (int column = 0; column < storage.Columns; column++)
            {
                var value = storage.Get(row, column);

                if (value is not null)
                {
                    Set(row, column, value);
                }
            }
        }
    }

    public string? Get(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row, nameof(row));
        ArgumentOutOfRangeException.ThrowIfNegative(column, nameof(column));

        var position = new CellPosition(row, column);

        return _data.GetValueOrDefault(position, null);
    }

    public void Set(int row, int column, string? value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row, nameof(row));
        ArgumentOutOfRangeException.ThrowIfNegative(column, nameof(column));

        var position = new CellPosition(row, column);
        var isEmpty = string.IsNullOrWhiteSpace(value);

        if (isEmpty)
        {
            _data.Remove(position);
        }
        else
        {
            _data[position] = value;
        }

        if (row >= Rows)
        {
            Rows = row + 1;
        }

        if (column >= Columns)
        {
            Columns = column + 1;
        }
    }

    private readonly record struct CellPosition
    {
        public int Row { get; }
        public int Column { get; }

        public CellPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }
    }
}