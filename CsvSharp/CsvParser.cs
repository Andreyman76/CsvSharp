namespace CsvSharp;

internal class CsvParser(string? csv, CsvFileOptions? options = default)
{
    private int _position;
    private readonly CsvFileOptions _options = options ?? CsvFileOptions.Default;
    private readonly string _csv = csv ?? string.Empty;

    private int _currentRow;
    private int _currentColumn;

    public bool HasNext => _position < _csv.Length;

    public CsvCell Next()
    {
        if (!HasNext)
        {
            throw new InvalidOperationException("No more cells");
        }

        var span = _csv.AsSpan();
        var valueStart = _position;
        var inQuotes = false;
        var valueWrapped = false;

        var wrapper = _options.Wrapper;
        var separator = _options.ColumnSeparator;
        var valueEnd = _position;
        var valueEndSet = false;

        while (_position < _csv.Length)
        {
            char c = span[_position];

            if (inQuotes)
            {
                if (c == wrapper)
                {
                    if (_position + 1 < span.Length && span[_position + 1] == wrapper)
                    {
                        _position += 2;
                        continue;
                    }
                    else
                    {
                        inQuotes = false;
                        _position++;
                        continue;
                    }
                }

                _position++;
            }
            else
            {
                if (c == wrapper && _position == valueStart)
                {
                    inQuotes = true;
                    valueWrapped = true;
                    _position++;
                    continue;
                }

                if (span[_position..].StartsWith(separator))
                {
                    valueEnd = _position;
                    valueEndSet = true;
                    break;
                }

                if (c == '\r')
                {
                    valueEnd = _position;
                    valueEndSet = true;
                    break;
                }

                if (c == '\n')
                {
                    valueEnd = _position;
                    valueEndSet = true;
                    break;
                }

                _position++;
            }
        }

        if (!valueEndSet)
        {
            valueEnd = _position;
        }

        var raw = span[valueStart..valueEnd];

        if (!valueWrapped && raw.Length > 0)
        {
            if (raw[^1] == '\n')
            {
                raw = raw[..^1];

                if (raw.Length > 0 && raw[^1] == '\r')
                {
                    raw = raw[..^1];
                }
            }
            else if (raw[^1] == '\r')
            {
                raw = raw[..^1];
            }
        }

        ReadOnlySpan<char> value;

        if (valueWrapped && raw.Length >= 2 && raw[0] == wrapper && raw[^1] == wrapper)
        {
            value = Unescape(raw[1..^1], wrapper);
        }
        else
        {
            value = raw;
        }

        var row = _currentRow;
        var column = _currentColumn;

        if (_position < _csv.Length)
        {
            if (span[_position..].StartsWith(separator))
            {
                _position++;
                _currentColumn++;
            }
            else if (span[_position] == '\r')
            {
                _position++;
                if (_position < _csv.Length && span[_position] == '\n')
                {
                    _position++;
                }

                _currentRow++;
                _currentColumn = 0;
            }
            else if (span[_position] == '\n')
            {
                _position++;
                _currentRow++;
                _currentColumn = 0;
            }
            else
            {
                _position++;
                _currentColumn++;
            }
        }
        else
        {
            _currentColumn++;
        }

        return new CsvCell(row, column, value);
    }

    private static string Unescape(ReadOnlySpan<char> span, char wrapper)
    {
        Span<char> buffer = stackalloc char[span.Length];
        var write = 0;

        for (int read = 0; read < span.Length; read++)
        {
            var c = span[read];

            if (c == wrapper && read + 1 < span.Length && span[read + 1] == wrapper)
            {
                buffer[write++] = wrapper;
                read++;
            }
            else
            {
                buffer[write++] = c;
            }
        }

        return buffer[..write].ToString();
    }
}