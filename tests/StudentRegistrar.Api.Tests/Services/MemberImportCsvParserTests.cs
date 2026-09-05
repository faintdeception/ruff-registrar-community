using System.Text;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class MemberImportCsvParserTests
{
    [Fact]
    public void Parse_ValidCsv_ReturnsRows()
    {
        var csv = "firstName,lastName,email\nJane,Doe,jane@example.com\nJohn,Smith,john@example.com\n";

        var rows = MemberImportCsvParser.Parse(ToStream(csv));

        Assert.Equal(2, rows.Count);
        Assert.Equal("Jane", rows[0].FirstName);
        Assert.Equal("Doe", rows[0].LastName);
        Assert.Equal("jane@example.com", rows[0].Email);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal(3, rows[1].RowNumber);
    }

    [Fact]
    public void Parse_HeaderCaseInsensitiveAndReordered_ReturnsRows()
    {
        var csv = "Email,FirstName,LastName\njane@example.com,Jane,Doe\n";

        var rows = MemberImportCsvParser.Parse(ToStream(csv));

        Assert.Single(rows);
        Assert.Equal("Jane", rows[0].FirstName);
        Assert.Equal("jane@example.com", rows[0].Email);
    }

    [Fact]
    public void Parse_QuotedFieldWithComma_ParsesCorrectly()
    {
        var csv = "firstName,lastName,email\n\"Jane, Q.\",Doe,jane@example.com\n";

        var rows = MemberImportCsvParser.Parse(ToStream(csv));

        Assert.Equal("Jane, Q.", rows[0].FirstName);
    }

    [Fact]
    public void Parse_BlankLinesAreSkipped()
    {
        var csv = "firstName,lastName,email\n\nJane,Doe,jane@example.com\n\n";

        var rows = MemberImportCsvParser.Parse(ToStream(csv));

        Assert.Single(rows);
    }

    [Fact]
    public void Parse_MissingRequiredColumn_Throws()
    {
        var csv = "firstName,email\nJane,jane@example.com\n";

        Assert.Throws<InvalidOperationException>(() => MemberImportCsvParser.Parse(ToStream(csv)));
    }

    [Fact]
    public void Parse_EmptyFile_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => MemberImportCsvParser.Parse(ToStream("")));
    }

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
