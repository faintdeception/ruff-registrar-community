using ClosedXML.Excel;

namespace StudentRegistrar.Api.Services;

/// <summary>
/// Generates the downloadable .xlsx template for the family bulk-import flow: one row per
/// child, with parent columns repeated per row so a family with multiple children groups by
/// ParentEmail. Ships with example rows so the expected shape is obvious without a separate
/// instructions doc.
/// </summary>
public static class FamilyImportTemplateGenerator
{
    public static readonly string[] Headers =
    {
        "ParentFirstName", "ParentLastName", "ParentEmail",
        "ChildFirstName", "ChildLastName", "ChildDateOfBirth", "ChildGrade"
    };

    public static byte[] Generate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Family Import");

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var exampleRows = new (string ParentFirstName, string ParentLastName, string ParentEmail,
            string ChildFirstName, string ChildLastName, DateTime ChildDateOfBirth, string ChildGrade)[]
        {
            ("Jane", "Smith", "jane.smith@example.com", "Alex", "Smith", new DateTime(2015, 4, 12), "3rd"),
            ("Jane", "Smith", "jane.smith@example.com", "Sam", "Smith", new DateTime(2018, 9, 2), "K"),
        };

        var row = 2;
        foreach (var example in exampleRows)
        {
            sheet.Cell(row, 1).Value = example.ParentFirstName;
            sheet.Cell(row, 2).Value = example.ParentLastName;
            sheet.Cell(row, 3).Value = example.ParentEmail;
            sheet.Cell(row, 4).Value = example.ChildFirstName;
            sheet.Cell(row, 5).Value = example.ChildLastName;
            sheet.Cell(row, 6).Value = example.ChildDateOfBirth;
            sheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 7).Value = example.ChildGrade;

            sheet.Range(row, 1, row, Headers.Length).Style.Fill.BackgroundColor = XLColor.LightYellow;
            row++;
        }

        var noteCell = sheet.Cell(row + 1, 1);
        noteCell.Value = "^ EXAMPLE ROWS - delete before uploading. One row per child; repeat the " +
            "Parent* columns for each additional child in the same family.";
        noteCell.Style.Font.Italic = true;
        noteCell.Style.Font.FontColor = XLColor.DarkRed;

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
