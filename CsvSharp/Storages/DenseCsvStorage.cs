using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace CsvSharp.Storages;

internal class DenseCsvStorage : ICsvStorage, IDisposable
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

            return (float)_notEmptyCells / total;
        }
    }

    private string?[] _data;
    private int _arrayRows;
    private int _arrayColumns;
    private int _notEmptyCells;

    public DenseCsvStorage(int rows = 100, int columns = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rows, nameof(rows));
        ArgumentOutOfRangeException.ThrowIfNegative(columns, nameof(columns));

        RentNewArray(rows, columns);
    }

    public DenseCsvStorage(ICsvStorage storage) : this(storage.Rows, storage.Columns)
    {
        for (int row = 0; row < storage.Rows; row++)
        {
            for (int column = 0; column < storage.Columns; column++)
            {
                Set(row, column, storage.Get(row, column));
            }
        }
    }

    public string? Get(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row, nameof(row));
        ArgumentOutOfRangeException.ThrowIfNegative(column, nameof(column));

        if (row >= Rows
            || column >= Columns)
        {
            return null;
        }

        return _data[row * _arrayColumns + column];
    }

    public void Set(int row, int column, string? value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row, nameof(row));
        ArgumentOutOfRangeException.ThrowIfNegative(column, nameof(column));

        if (row >= _arrayRows || column >= _arrayColumns)
        {
            var newRows = Math.Max(_arrayRows * 2, row + 1);
            var newCols = Math.Max(_arrayColumns * 2, column + 1);
            RentNewArray(newRows, newCols);
        }

        var index = row * _arrayColumns + column;
        var wasEmpty = _data[index] is null;
        var isEmpty = string.IsNullOrWhiteSpace(value);

        if (wasEmpty && !isEmpty)
        {
            _notEmptyCells++;
        }
        else if (!wasEmpty && isEmpty)
        {
            _notEmptyCells--;
        }

        _data[index] = isEmpty ? null : value;

        if (row >= Rows)
        {
            Rows = row + 1;
        }

        if (column >= Columns)
        {
            Columns = column + 1;
        }
    }

    [MemberNotNull(nameof(_data))]
    private void RentNewArray(int rows, int columns)
    {
        var oldData = _data;
        var oldRowCapacity = _arrayRows;
        var oldColumnCapacity = _arrayColumns;

        _arrayRows = rows;
        _arrayColumns = columns;
        _data = ArrayPool<string?>.Shared.Rent(rows * columns);

        Array.Clear(_data, 0, _data.Length);

        if (oldData is not null)
        {
            if (columns == oldColumnCapacity)
            {
                var copyCount = Math.Min(Rows, oldRowCapacity) * columns;
                Array.Copy(oldData, 0, _data, 0, copyCount);
            }
            else
            {
                var matterRows = Math.Min(Rows, oldRowCapacity);
                var matterColumns = Math.Min(Columns, oldColumnCapacity);

                for (int row = 0; row < matterRows; row++)
                {
                    for (int col = 0; col < matterColumns; col++)
                    {
                        var oldIndex = row * oldColumnCapacity + col;
                        var newIndex = row * _arrayColumns + col;
                        _data[newIndex] = oldData[oldIndex];
                    }
                }
            }

            ArrayPool<string?>.Shared.Return(oldData);
        }
    }

    public void Dispose()
    {
        ArrayPool<string?>.Shared.Return(_data);
    }
}