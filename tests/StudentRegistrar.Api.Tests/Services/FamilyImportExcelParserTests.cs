using ClosedXML.Excel;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class FamilyImportExcelParserTests
{
    private static readonly string[] Headers = FamilyImportTemplateGenerator.Headers;

    [Fact]
    public void Parse_ValidFamilyWithMultipleChildren_GroupsByParentEmail()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", new DateTime(2015, 4, 12), "3rd" },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", new DateTime(2018, 9, 2), "K" },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        Assert.Empty(result.RowErrors);
        var family = Assert.Single(result.Families);
        Assert.Equal("jane@example.com", family.ParentEmail);
        Assert.Equal(2, family.Children.Count);
        Assert.Contains(family.Children, c => c.ChildFirstName == "Alex" && c.ChildGrade == "3rd");
        Assert.Contains(family.Children, c => c.ChildFirstName == "Sam" && c.ChildDateOfBirth == new DateTime(2018, 9, 2));
    }

    [Fact]
    public void Parse_MultipleFamilies_ProducesSeparateGroups()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Bob", "Lee", "bob@example.com", "Cara", "Lee", null, null },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        Assert.Empty(result.RowErrors);
        Assert.Equal(2, result.Families.Count);
    }

    [Fact]
    public void Parse_ParentEmailCaseInsensitive_GroupsAsOneFamily()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "Smith", "Jane@Example.com", "Alex", "Smith", null, null },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", null, null },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        var family = Assert.Single(result.Families);
        Assert.Equal(2, family.Children.Count);
    }

    [Fact]
    public void Parse_MissingHeaderColumn_Throws()
    {
        var badHeaders = Headers.Where(h => h != "ChildGrade").ToArray();
        var stream = BuildWorkbook(badHeaders, new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null },
        });

        Assert.Throws<InvalidOperationException>(() => FamilyImportExcelParser.Parse(stream));
    }

    [Fact]
    public void Parse_EmptyWorkbook_Throws()
    {
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Family Import");
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        Assert.Throws<InvalidOperationException>(() => FamilyImportExcelParser.Parse(stream));
    }

    [Fact]
    public void Parse_BlankRequiredCell_ReportsRowErrorAndExcludesRow()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "", "jane@example.com", "Alex", "Smith", null, null },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        Assert.Empty(result.Families);
        var rowError = Assert.Single(result.RowErrors);
        Assert.Equal(2, rowError.RowNumber);
        Assert.Contains("ParentLastName", rowError.Message);
    }

    [Fact]
    public void Parse_InvalidParentEmail_ReportsRowError()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "Smith", "not-an-email", "Alex", "Smith", null, null },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        Assert.Empty(result.Families);
        var rowError = Assert.Single(result.RowErrors);
        Assert.Contains("not a valid ParentEmail", rowError.Message);
    }

    [Fact]
    public void Parse_TooManyRows_Throws()
    {
        var rows = Enumerable.Range(1, FamilyImportExcelParser.MaxChildRowsPerFile + 1)
            .Select(i => new object?[] { "Jane", "Smith", $"jane{i}@example.com", "Alex", "Smith", null, null })
            .ToArray();
        var stream = BuildWorkbook(Headers, rows);

        Assert.Throws<InvalidOperationException>(() => FamilyImportExcelParser.Parse(stream));
    }

    [Fact]
    public void Parse_FamilyExceedsMaxChildren_ReportsRowErrorsForWholeFamilyAndExcludesIt()
    {
        var rows = Enumerable.Range(1, FamilyImportExcelParser.MaxChildrenPerFamily + 1)
            .Select(i => new object?[] { "Jane", "Smith", "jane@example.com", $"Child{i}", "Smith", null, null })
            .ToArray();
        var stream = BuildWorkbook(Headers, rows);

        var result = FamilyImportExcelParser.Parse(stream);

        Assert.Empty(result.Families);
        Assert.Equal(FamilyImportExcelParser.MaxChildrenPerFamily + 1, result.RowErrors.Count);
        Assert.All(result.RowErrors, e => Assert.Contains("exceeds the maximum", e.Message));
    }

    [Fact]
    public void Parse_BlankDateOfBirthCell_IsOptional()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        var family = Assert.Single(result.Families);
        var child = Assert.Single(family.Children);
        Assert.Null(child.ChildDateOfBirth);
        Assert.Null(child.ChildGrade);
    }

    [Fact]
    public void Parse_InvalidDateOfBirthText_ReportsRowError()
    {
        var stream = BuildWorkbook(Headers, new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", "not-a-date", null },
        });

        var result = FamilyImportExcelParser.Parse(stream);

        Assert.Empty(result.Families);
        var rowError = Assert.Single(result.RowErrors);
        Assert.Contains("not a valid ChildDateOfBirth", rowError.Message);
    }

    private static MemoryStream BuildWorkbook(string[] headers, object?[][] dataRows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Family Import");

        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        for (var rowIndex = 0; rowIndex < dataRows.Length; rowIndex++)
        {
            var row = dataRows[rowIndex];
            for (var col = 0; col < row.Length; col++)
            {
                var cell = sheet.Cell(rowIndex + 2, col + 1);
                switch (row[col])
                {
                    case null:
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        break;
                    default:
                        cell.Value = row[col]!.ToString();
                        break;
                }
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
