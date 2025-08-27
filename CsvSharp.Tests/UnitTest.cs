using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace CsvSharp.Tests;

public class UnitTest
{
    private static readonly CsvFileOptions _options = new()
    {
        HasHeader = true
    };

    private const string _strangeCsv = """""
            Name;Count;
            Apple;5;a
            """Banana""";10;b
            Orange;3;o
            "Apple;Pen";42;
            """Lime"";"";""";;
            ___;;
            ";";;
            """";;
            ___;;
            ;;
            """ ;"""";;"" "",;, """;;
            
            """"";

    [Fact]
    public void SaveSimpleCsv()
    {
        using var csv = new CsvFile(["Name", "Age"]);

        csv[0, "Name"] = "Alice";
        csv[0, "Age"] = "30";
        csv[1, "Name"] = "Bob";
        csv[1, "Age"] = "25";

        var expected = """
            Name;Age
            Alice;30
            Bob;25

            """;

        using var writer = new StringWriter();
        csv.Save(writer, _options);
        var actual = writer.ToString();

        Assert.Equal(2, csv.Rows);
        Assert.Equal(2, csv.Columns);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreateAndSaveStrangeCsv()
    {
        using var csv = new CsvFile(["Name", "Count"]);

        csv[0, 0] = "Apple";
        csv[0, 1] = "5";
        csv[0, 2] = "a";

        csv[1, 0] = "\"Banana\"";
        csv[1, 1] = "10";
        csv[1, 2] = "b";

        csv[2, 0] = "Orange";
        csv[2, 1] = "3";
        csv[2, 2] = "o";

        csv[3, 0] = "Apple;Pen";
        csv[3, 1] = "42";

        csv[4, 0] = "\"Lime\";\";\"";

        csv[5, 0] = "___";

        csv[6, 0] = ";";

        csv[7, 0] = "\"";

        csv[8, 0] = "___";

        csv[10, 0] = "\" ;\"\";;\" \",;, \"";

        using var writer = new StringWriter();
        csv.Save(writer, _options);
        var actual = writer.ToString();

        Assert.Equal(11, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal(_strangeCsv, actual);
    }

    [Fact]
    public void ParseStrangeCsv()
    {
        using var csv = CsvFile.Parse(_strangeCsv, _options);

        Assert.Equal(11, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal("Apple", csv[0, 0]);
        Assert.Equal("5", csv[0, 1]);
        Assert.Equal("a", csv[0, 2]);
        Assert.Equal("\"Banana\"", csv[1, 0]);
        Assert.Equal("10", csv[1, 1]);
        Assert.Equal("b", csv[1, 2]);
        Assert.Equal("Orange", csv[2, 0]);
        Assert.Equal("3", csv[2, 1]);
        Assert.Equal("o", csv[2, 2]);
        Assert.Equal("Apple;Pen", csv[3, 0]);
        Assert.Equal("42", csv[3, 1]);
        Assert.Equal("\"Lime\";\";\"", csv[4, 0]);
        Assert.Equal("___", csv[5, 0]);
        Assert.Equal(";", csv[6, 0]);
        Assert.Equal("\"", csv[7, 0]);
        Assert.Equal("___", csv[8, 0]);
        Assert.Null(csv[9, 0]);
        Assert.Equal("\" ;\"\";;\" \",;, \"", csv[10, 0]);
    }

    [Fact]
    public void ParseAndSaveStrangeCsv()
    {
        using var csv = CsvFile.Parse(_strangeCsv, _options);
        using var writer = new StringWriter();

        csv.Save(writer, _options);
        var actual = writer.ToString();

        Assert.Equal(11, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal(_strangeCsv, actual);
    }

    [Fact]
    public void LoadCsvFromStreamWithEncoding()
    {
        var text = """
            Имя;Возраст
            Иван;25
            Мария;30
            """;

        var bytes = Encoding.UTF32.GetBytes(text);

        using var stream = new MemoryStream(bytes);
        using var csv = CsvFile.Load(stream, encoding: Encoding.UTF32);

        Assert.Equal(3, csv.Rows);
        Assert.Equal(2, csv.Columns);

        Assert.Equal("Имя", csv[0, 0]);
        Assert.Equal("Возраст", csv[0, 1]);
        Assert.Equal("Иван", csv[1, 0]);
        Assert.Equal("25", csv[1, 1]);
        Assert.Equal("Мария", csv[2, 0]);
        Assert.Equal("30", csv[2, 1]);
    }

    [Fact]
    public void SaveCsvToStreamWithEncoding()
    {
        using var csv = new CsvFile(["Имя", "Возраст"]);
        csv[0, 0] = "Иван";
        csv[0, 1] = "25";

        using var stream = new MemoryStream();

        csv.Save(stream, encoding: Encoding.UTF32);

        var result = Encoding.UTF32.GetString(stream.ToArray());

        Assert.Equal(1, csv.Rows);
        Assert.Equal(2, csv.Columns);

        Assert.StartsWith("Иван", result);
    }

    [Fact]
    public void ParseEmptyCsv()
    {
        using var csv = CsvFile.Parse(string.Empty, _options);

        Assert.Equal(0, csv.Rows);
        Assert.Equal(0, csv.Columns);

        Assert.Empty(csv.AsString());

        Assert.Equal(0, csv.Rows);
        Assert.Equal(0, csv.Columns);
    }

    [Fact]
    public void ParseCsvWithoutHeader()
    {
        var text = """
            1;2;3
            4;5;6"
            """;

        var options = new CsvFileOptions
        {
            HasHeader = false
        };

        var csv = CsvFile.Parse(text, options);

        Assert.Equal(2, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal("1", csv[0, 0]);
        Assert.Equal("5", csv[1, 1]);
    }

    [Fact]
    public void SparseDataShouldBeAccessible()
    {
        using var csv = new CsvFile();

        var row = 100;
        var column = 50;

        csv[row, column] = "end";

        Assert.Equal(101, csv.Rows);
        Assert.Equal(51, csv.Columns);

        for (int r = 0; r < row; r++)
        {
            for (int c = 0; c < column; c++)
            {
                if (r == row && c == column)
                {
                    Assert.Equal("end", csv[r, c]);
                }
                else
                {
                    Assert.Null(csv[r, c]);
                }
            }
        }
    }

    [Fact]
    public void MissingValuesShouldBeEmpty()
    {
        var text = """
            A;B;C
            1;;3
            ;;
            """;

        using var csv = CsvFile.Parse(text, _options);

        Assert.Equal(2, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal("1", csv[0, 0]);
        Assert.Null(csv[0, 1]);
        Assert.Null(csv[1, 0]);
        Assert.Null(csv[1, 1]);
        Assert.Null(csv[1, 2]);
    }

    [Fact]
    public void AccessByHeaderName()
    {
        var text = """
            Name;Age
            Alice;30
            """;

        using var csv = CsvFile.Parse(text, _options);

        Assert.Equal(1, csv.Rows);
        Assert.Equal(2, csv.Columns);

        Assert.Equal("Alice", csv[0, "Name"]);
        Assert.Equal("30", csv[0, "Age"]);
    }

    [Fact]
    public void IrregularColumnsHandledCorrectly()
    {
        var text = """
            A;B;C
            1;2
            3;4;5;6
            """;

        using var csv = CsvFile.Parse(text, _options);

        Assert.Equal(2, csv.Rows);
        Assert.Equal(4, csv.Columns);

        Assert.Equal("1", csv[0, 0]);
        Assert.Null(csv[0, 2]);
        Assert.Equal("6", csv[1, 3]);
    }

    [Fact]
    public void SaveAndReloadShouldPreserveData()
    {
        using var original = new CsvFile(["X", "Y"]);
        original[0, 0] = "Hello";
        original[0, 1] = "World";

        using var writer = new StringWriter();
        original.Save(writer, _options);
        var text = writer.ToString();

        using var reloaded = CsvFile.Parse(text, _options);

        Assert.Equal(1, original.Rows);
        Assert.Equal(2, original.Columns);

        Assert.Equal(1, reloaded.Rows);
        Assert.Equal(2, reloaded.Columns);

        Assert.Equal("Hello", reloaded[0, "X"]);
        Assert.Equal("World", reloaded[0, "Y"]);
    }

    [Fact]
    public void SpecialCharactersPreservedCorrectly()
    {
        using var csv = new CsvFile(["Note"]);
        csv[0, 0] = "Text with; semicolon\r\nand newline\r\nand \"quotes\"";

        using var writer = new StringWriter();
        csv.Save(writer, _options);

        using var result = CsvFile.Parse(writer.ToString(), _options);

        Assert.Equal(1, csv.Rows);
        Assert.Equal(1, csv.Columns);

        Assert.Equal(1, result.Rows);
        Assert.Equal(1, result.Columns);

        Assert.Equal(csv[0, 0], result[0, 0]);
    }

    [Fact]
    public void ParseSpecialCharactersPreservedCorrectly()
    {
        var text = "\"hello\nworld\";\"\"\"\";\"\"\"\"\"\"\r\n\";\";\"\"\";\";\";\"\"\"\";\"\r\n";

        using var csv = CsvFile.Parse(text);

        Assert.Equal("hello\nworld", csv[0, 0]);
        Assert.Equal("\"", csv[0, 1]);
        Assert.Equal("\"\"", csv[0, 2]);
        Assert.Equal(";", csv[1, 0]);
        Assert.Equal("\";", csv[1, 1]);
        Assert.Equal(";\"\";", csv[1, 2]);

        Assert.Equal(2, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal(text, csv.AsString());
    }

    [Fact]
    public void ParseInvalidCsv()
    {
        var invalid = """
            1;2;3
            1
            1;2
            ;
            """;

        var valid = """
            1;2;3
            1;;
            1;2;
            ;;

            """;

        using var csv = CsvFile.Parse(invalid);

        Assert.Equal(4, csv.Rows);
        Assert.Equal(3, csv.Columns);

        Assert.Equal("1", csv[0, 0]);
        Assert.Equal("2", csv[0, 1]);
        Assert.Equal("3", csv[0, 2]);

        Assert.Equal("1", csv[1, 0]);
        Assert.Null(csv[1, 1]);
        Assert.Null(csv[1, 2]);

        Assert.Equal("1", csv[2, 0]);
        Assert.Equal("2", csv[2, 1]);
        Assert.Null(csv[2, 2]);

        Assert.Null(csv[3, 0]);
        Assert.Null(csv[3, 1]);
        Assert.Null(csv[3, 2]);

        Assert.Equal(valid, csv.AsString());
    }

    [Fact]
    public void ChangeStorageType()
    {
        using var csv = new CsvFile();
        Assert.True(csv.IsDense);

        for (int i = 0; i < 100; i++)
        {
            csv[i, 0] = i.ToString();
        }

        Assert.True(csv.IsDense);

        for (int i = 0; i < 100; i++)
        {
            csv[i, 20] = i.ToString();
        }

        Assert.False(csv.IsDense);

        for (int i = 0; i < 10_000; i++)
        {
            csv[i, 1] = i.ToString();
            csv[i, 2] = i.ToString();
            csv[i, 3] = i.ToString();
            csv[i, 4] = i.ToString();
            csv[i, 5] = i.ToString();
        }

        Assert.True(csv.IsDense);

        using var sha = SHA256.Create();
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        csv.Save(writer);
        ms.Seek(0, SeekOrigin.Begin);
        var hash = Convert.ToHexString(sha.ComputeHash(ms));

        Assert.Equal("5ACC1D8E3B2DB3E5C4A3DAF7E9F187D367045D86A10C08C21C29E9CD034D12F1", hash);
    }
}