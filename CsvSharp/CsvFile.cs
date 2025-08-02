using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

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

public class CsvFile : IParsable<CsvFile>
{
    private readonly List<List<string>> _data = [];
    private List<string> _header;
    private int _maxColumnIndex;

    public int Rows => _data.Count;
    public int Columns => _maxColumnIndex > 0 ? _maxColumnIndex + 1 : 0;

    public CsvFile(List<string> header)
    {
        _header = header;
    }

    public CsvFile()
    {
        _header = [];
    }

    #region Save

    public void Save(string fileName, CsvFileOptions? options = default, Encoding? encoding = default)
    {
        SaveToFile(
            fileName,
            options ?? CsvFileOptions.Default,
            encoding ?? Encoding.UTF8);
    }

    private void SaveToFile(string fileName, CsvFileOptions options, Encoding encoding)
    {
        using var stream = File.OpenWrite(fileName);

        SaveToStream(stream, options, encoding);
    }

    public void Save(Stream stream, CsvFileOptions? options = default, Encoding? encoding = default)
    {
        SaveToStream(
            stream,
            options ?? CsvFileOptions.Default,
            encoding ?? Encoding.UTF8);
    }

    private void SaveToStream(Stream stream, CsvFileOptions options, Encoding encoding)
    {
        using var writer = new StreamWriter(stream, encoding);

        SaveToWriter(writer, options);
    }

    public void Save(TextWriter writer, CsvFileOptions? options = default)
    {
        SaveToWriter(
            writer,
            options ?? CsvFileOptions.Default);
    }

    private void SaveToWriter(TextWriter writer, CsvFileOptions options)
    {
        if (options.HasHeader)
        {
            WriteRow(writer, _header, options);
        }

        foreach (var row in _data)
        {
            WriteRow(writer, row, options);
        }

        writer.Flush();
    }

    public string AsString(CsvFileOptions? options = default)
    {
        using var writer = new StringWriter();

        SaveToWriter(
            writer,
            options ?? CsvFileOptions.Default);

        return writer.ToString();
    }

    #endregion

    #region Load

    public static CsvFile Load(string fileName, CsvFileOptions? options = default, Encoding? encoding = default)
    {
        return LoadFromFile(
            fileName,
            options ?? CsvFileOptions.Default,
            encoding ?? Encoding.UTF8);
    }

    private static CsvFile LoadFromFile(string fileName, CsvFileOptions options, Encoding encoding)
    {
        using var stream = File.OpenRead(fileName);

        return LoadFromStream(stream, options, encoding);
    }

    public static CsvFile Load(Stream stream, CsvFileOptions? options = default, Encoding? encoding = default)
    {
        return LoadFromStream(
            stream,
            options ?? CsvFileOptions.Default,
            encoding ?? Encoding.UTF8);
    }

    private static CsvFile LoadFromStream(Stream stream, CsvFileOptions options, Encoding encoding)
    {
        using var reader = new StreamReader(stream, encoding);

        return LoadFromReader(reader, options);
    }

    public static CsvFile Load(TextReader reader, CsvFileOptions? options = default)
    {
        return LoadFromReader(
            reader,
            options ?? CsvFileOptions.Default);
    }

    private static CsvFile LoadFromReader(TextReader reader, CsvFileOptions options)
    {
        var result = new CsvFile();
        string? line;
        var isFirstLine = true;

        while ((line = ReadCsvLine(reader, options)) is not null)
        {
            var row = ParseCsvRow(line, options);

            result._maxColumnIndex = Math.Max(result._maxColumnIndex, row.Count - 1);

            if (isFirstLine && options.HasHeader)
            {
                result._header = row;
                isFirstLine = false;
                continue;
            }

            result._data.Add(row);

            isFirstLine = false;
        }

        return result;
    }

    #endregion

    #region IParsable

    public static CsvFile Parse(
       string str,
       IFormatProvider? provider = default)
    {
        var options = provider?.GetFormat(typeof(CsvFileOptions)) as CsvFileOptions
            ?? CsvFileOptions.Default;

        using var reader = new StringReader(str);

        return Load(reader, options);
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? str,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out CsvFile result)
    {
        Unsafe.SkipInit(out result);

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(str, nameof(str));

            result = Parse(str, provider);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static CsvFile Parse(
        string str,
        CsvFileOptions? options = default)
    {
        return Parse(str, options as IFormatProvider);
    }

    public static bool TryParse(
       [NotNullWhen(true)] string? str,
       CsvFileOptions? options,
       [MaybeNullWhen(false)] out CsvFile result)
    {
        return TryParse(str, options as IFormatProvider, out result);
    }

    #endregion

    #region Get/set cells

    public string this[int row, string column]
    {
        get
        {
            var c = _header.IndexOf(column);

            if (c < 0)
            {
                throw new KeyNotFoundException($"Column '{column}' does not exists");
            }

            return this[row, c];
        }
        set
        {
            var c = _header.IndexOf(column);

            if (c < 0)
            {
                _header.Add(column);
                c = _header.Count - 1;
            }

            this[row, c] = value;
        }
    }

    public string this[int row, int column]
    {
        get
        {
            if(row >= Rows || column >= _data[row].Count)
            {
                return string.Empty;
            }

            return _data[row][column];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row, nameof(row));
            ArgumentOutOfRangeException.ThrowIfNegative(column, nameof(column));

            while (_data.Count - 1 < row)
            {
                _data.Add([]);
            }

            while (_data[row].Count - 1 < column)
            {
                _data[row].Add(string.Empty);
            }

            _data[row][column] = value;
            _maxColumnIndex = Math.Max(_maxColumnIndex, column);
        }
    }

    #endregion

    private void WriteRow(TextWriter writer, List<string> row, CsvFileOptions options)
    {
        for (int columnIndex = 0; columnIndex <= _maxColumnIndex; columnIndex++)
        {
            if (row.Count > columnIndex)
            {
                writer.Write(FormatCsvString(row[columnIndex], options));
            }

            if (columnIndex < _maxColumnIndex)
            {
                writer.Write(options.ColumnSeparator);
            }
        }

        writer.WriteLine();
    }

    private static string FormatCsvString(string str, CsvFileOptions options)
    {
        var w = options.Wrapper;

        if (str.Contains(options.ColumnSeparator)
            || str.Contains(w)
            || str.Contains('\n')
            || str.Contains('\r'))
        {
            return $"{w}{str.Replace(w, w + w)}{w}";
        }

        return str;
    }

    private static string? ReadCsvLine(TextReader reader, CsvFileOptions options)
    {
        var line = reader.ReadLine();

        if (line is null)
        {
            return null;
        }

        var sb = new StringBuilder(line);
        var wrappersCount = GetWrappersCount(line, options.Wrapper);

        while (wrappersCount % 2 != 0)
        {
            var nextLine = reader.ReadLine();

            if (nextLine is null)
            {
                break;
            }

            sb.AppendLine();
            sb.Append(nextLine);

            wrappersCount += GetWrappersCount(nextLine, options.Wrapper);
        }

        return sb.ToString();
    }

    private static int GetWrappersCount(string str, string wrapper)
    {
        if (wrapper.Length == 1)
        {
            return str.Count(c => c == wrapper[0]);
        }

        return str.Split(wrapper).Length - 1;
    }

    private static List<string> ParseCsvRow(ReadOnlySpan<char> line, CsvFileOptions options)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        int i = 0;
        var wrapper = options.Wrapper;
        var separator = options.ColumnSeparator;

        while (i < line.Length)
        {
            char c = line[i];

            if (c == wrapper[0])
            {
                if (inQuotes)
                {
                    // Возможен экранированный символ кавычки (двойной)
                    if (i + 1 < line.Length && line[i + 1] == wrapper[0])
                    {
                        sb.Append(wrapper[0]);
                        i += 2;
                    }
                    else
                    {
                        // Закрытие кавычек
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    // Начало кавычек
                    inQuotes = true;
                    i++;
                }
            }
            else if (!inQuotes && IsSeparatorAt(line, i, separator))
            {
                result.Add(sb.ToString());
                sb.Clear();
                i += separator.Length;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }

        result.Add(sb.ToString());
        return result;
    }

    private static bool IsSeparatorAt(ReadOnlySpan<char> line, int index, string separator)
    {
        if (separator.Length + index > line.Length)
        {
            return false;
        }

        for (int j = 0; j < separator.Length; j++)
        {
            if (line[index + j] != separator[j])
            {
                return false;
            }
        }

        return true;
    }
}