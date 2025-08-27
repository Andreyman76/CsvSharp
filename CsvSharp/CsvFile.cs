using CsvSharp.Storages;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace CsvSharp;

public class CsvFile : IParsable<CsvFile>, IDisposable
{
    public float SwitchToSparseThreshold { get; set; } = 0.05f;
    public float SwitchToDenseThreshold { get; set; } = 0.30f;
    public int CheckDensityAfter { get; set; } = 100;

    public int Rows => _storage.Rows;
    public int Columns => _storage.Columns;
    public bool IsDense { get; private set; } = true;

    private ICsvStorage _storage = new DenseCsvStorage();
    private readonly List<string> _header;
    private int _setsCounter;

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
            for (int column = 0; column < _header.Count; column++)
            {
                if (column > 0)
                {
                    writer.Write(options.ColumnSeparator);
                }

                writer.Write(FormatCsvString(_header[column], options));
            }

            var count = _storage.Columns - _header.Count;

            for (int i = 0; i < count; i++)
            {
                writer.Write(options.ColumnSeparator);
            }

            writer.WriteLine();
        }

        for (int row = 0; row < _storage.Rows; row++)
        {
            for (int column = 0; column < _storage.Columns; column++)
            {
                if (column > 0)
                {
                    writer.Write(options.ColumnSeparator);
                }

                var cell = _storage.Get(row, column);
                writer.Write(FormatCsvString(cell, options));
            }

            writer.WriteLine();
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
        var parser = new CsvParser(reader.ReadToEnd(), options);

        while (parser.HasNext)
        {
            var cell = parser.Next();

            if (cell.Row == 0 && options.HasHeader)
            {
                result._header.Add(cell.Value.ToString());
                continue;
            }

            result._storage.Set(
                options.HasHeader
                    ? cell.Row - 1
                    : cell.Row
                , cell.Column, cell.Value.ToString());
        }

        return result;
    }

    #endregion

    #region IParsable

    public static CsvFile Parse(string str)
    {
        return Parse(str, CsvFileOptions.Default);
    }

    public static CsvFile Parse(
       string str,
       IFormatProvider? provider = default)
    {
        var options = provider?.GetFormat(typeof(CsvFileOptions)) as CsvFileOptions
            ?? CsvFileOptions.Default;

        using var reader = new StringReader(str);

        return LoadFromReader(reader, options);
    }

    public static bool TryParse(
       [NotNullWhen(true)] string? str,
       [MaybeNullWhen(false)] out CsvFile result)
    {
        return TryParse(str, CsvFileOptions.Default, out result);
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? str,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out CsvFile result)
    {
        Unsafe.SkipInit(out result);

        try
        {
            result = Parse(str ?? string.Empty, provider);
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

    public string? this[int row, string column]
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

    public string? this[int row, int column]
    {
        get => _storage.Get(row, column);
        set
        {
            _storage.Set(row, column, value);
            _setsCounter++;

            if (_setsCounter > CheckDensityAfter)
            {
                _setsCounter = 0;
                ChangeStorageIfNeed();
            }
        }
    }

    #endregion

    private void ChangeStorageIfNeed()
    {
        if (_storage.Density < SwitchToSparseThreshold && IsDense)
        {
            var old = _storage;

            _storage = new SparseCsvStorage(_storage);
            IsDense = false;

            if (old is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        else if (_storage.Density > SwitchToDenseThreshold && !IsDense)
        {
            _storage = new DenseCsvStorage(_storage);
            IsDense = true;
        }
    }

    private static string? FormatCsvString(string? str, CsvFileOptions options)
    {
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        var w = options.Wrapper;

        if (str.Contains(options.ColumnSeparator)
            || str.Contains(w)
            || str.Contains('\n')
            || str.Contains('\r'))
        {

            var doubledWrapper = $"{w}{w}";

            return $"{w}{str.Replace(w.ToString(), doubledWrapper)}{w}";
        }

        return str;
    }

    public void Dispose()
    {
        if (_storage is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}