using ClosedXML.Excel;

namespace StudentRegistrar.Api.Services;

public sealed class FamilyImportChildRow
{
    public int RowNumber { get; init; }
    public string ParentFirstName { get; init; } = string.Empty;
    public string ParentLastName { get; init; } = string.Empty;
    public string ParentEmail { get; init; } = string.Empty;
    public string ChildFirstName { get; init; } = string.Empty;
    public string ChildLastName { get; init; } = string.Empty;
    public DateTime? ChildDateOfBirth { get; init; }
    public string? ChildGrade { get; init; }
}

public sealed class FamilyImportRowError
{
    public int RowNumber { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class FamilyImportFamily
{
    public string ParentEmail { get; init; } = string.Empty;
    public string ParentFirstName { get; init; } = string.Empty;
    public string ParentLastName { get; init; } = string.Empty;
    public List<FamilyImportChildRow> Children { get; } = new();
}

public sealed class FamilyImportParseResult
{
    public List<FamilyImportFamily> Families { get; } = new();
    public List<FamilyImportRowError> RowErrors { get; } = new();
}

/// <summary>
/// Reads the family bulk-import .xlsx template: one row per child, parent columns repeated per
/// row, grouped into families by ParentEmail. Validation-only — no Keycloak/database access.
/// Not a general-purpose Excel library wrapper, kept scoped to this one import shape.
/// </summary>
public static class FamilyImportExcelParser
{
    public const int MaxChildRowsPerFile = 300;
    public const int MaxChildrenPerFamily = 25;

    private static readonly string[] RequiredHeaders =
    {
        "parentfirstname", "parentlastname", "parentemail",
        "childfirstname", "childlastname", "childdateofbirth", "childgrade"
    };

    public static FamilyImportParseResult Parse(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The workbook has no worksheets.");

        var headerRow = sheet.Row(1);
        if (headerRow.IsEmpty())
        {
            throw new InvalidOperationException(
                "The workbook must have a header row with columns: " + string.Join(", ", FamilyImportTemplateGenerator.Headers));
        }

        var columnIndexes = MapHeaderColumns(headerRow);

        var lastRowNumber = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var dataRows = new List<(int RowNumber, IXLRow Row)>();
        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (!row.IsEmpty())
            {
                dataRows.Add((rowNumber, row));
            }
        }

        if (dataRows.Count > MaxChildRowsPerFile)
        {
            throw new InvalidOperationException(
                $"The file has {dataRows.Count} child rows, which exceeds the maximum of {MaxChildRowsPerFile} per upload. Split it into multiple files.");
        }

        var result = new FamilyImportParseResult();
        var familiesByEmail = new Dictionary<string, FamilyImportFamily>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rowNumber, row) in dataRows)
        {
            ParseRow(row, rowNumber, columnIndexes, result, familiesByEmail);
        }

        foreach (var family in familiesByEmail.Values)
        {
            if (family.Children.Count > MaxChildrenPerFamily)
            {
                foreach (var child in family.Children)
                {
                    result.RowErrors.Add(new FamilyImportRowError
                    {
                        RowNumber = child.RowNumber,
                        Message = $"Family '{family.ParentEmail}' has {family.Children.Count} children, " +
                            $"which exceeds the maximum of {MaxChildrenPerFamily} per family."
                    });
                }
                continue;
            }

            result.Families.Add(family);
        }

        return result;
    }

    private static void ParseRow(
        IXLRow row,
        int rowNumber,
        Dictionary<string, int> columnIndexes,
        FamilyImportParseResult result,
        Dictionary<string, FamilyImportFamily> familiesByEmail)
    {
        string GetText(string header) => row.Cell(columnIndexes[header]).GetString().Trim();

        var parentFirstName = GetText("parentfirstname");
        var parentLastName = GetText("parentlastname");
        var parentEmail = GetText("parentemail");
        var childFirstName = GetText("childfirstname");
        var childLastName = GetText("childlastname");
        var gradeText = GetText("childgrade");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(parentFirstName)) missing.Add("ParentFirstName");
        if (string.IsNullOrWhiteSpace(parentLastName)) missing.Add("ParentLastName");
        if (string.IsNullOrWhiteSpace(parentEmail)) missing.Add("ParentEmail");
        if (string.IsNullOrWhiteSpace(childFirstName)) missing.Add("ChildFirstName");
        if (string.IsNullOrWhiteSpace(childLastName)) missing.Add("ChildLastName");

        if (missing.Count > 0)
        {
            result.RowErrors.Add(new FamilyImportRowError
            {
                RowNumber = rowNumber,
                Message = $"Missing required value(s): {string.Join(", ", missing)}."
            });
            return;
        }

        if (!IsValidEmail(parentEmail))
        {
            result.RowErrors.Add(new FamilyImportRowError
            {
                RowNumber = rowNumber,
                Message = $"'{parentEmail}' is not a valid ParentEmail address."
            });
            return;
        }

        var dobCell = row.Cell(columnIndexes["childdateofbirth"]);
        DateTime? dateOfBirth = null;
        if (!dobCell.IsEmpty())
        {
            if (dobCell.DataType == XLDataType.DateTime)
            {
                dateOfBirth = dobCell.GetDateTime();
            }
            else if (DateTime.TryParse(dobCell.GetString(), out var parsedDate))
            {
                dateOfBirth = parsedDate;
            }
            else
            {
                result.RowErrors.Add(new FamilyImportRowError
                {
                    RowNumber = rowNumber,
                    Message = $"'{dobCell.GetString()}' is not a valid ChildDateOfBirth."
                });
                return;
            }
        }

        var childRow = new FamilyImportChildRow
        {
            RowNumber = rowNumber,
            ParentFirstName = parentFirstName,
            ParentLastName = parentLastName,
            ParentEmail = parentEmail,
            ChildFirstName = childFirstName,
            ChildLastName = childLastName,
            ChildDateOfBirth = dateOfBirth,
            ChildGrade = string.IsNullOrWhiteSpace(gradeText) ? null : gradeText
        };

        if (!familiesByEmail.TryGetValue(parentEmail, out var family))
        {
            family = new FamilyImportFamily
            {
                ParentEmail = parentEmail,
                ParentFirstName = parentFirstName,
                ParentLastName = parentLastName
            };
            familiesByEmail[parentEmail] = family;
        }

        family.Children.Add(childRow);
    }

    private static Dictionary<string, int> MapHeaderColumns(IXLRow headerRow)
    {
        var columnIndexes = RequiredHeaders.ToDictionary(h => h, _ => -1);

        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var col = 1; col <= lastColumn; col++)
        {
            var headerText = headerRow.Cell(col).GetString().Trim().ToLowerInvariant();
            if (columnIndexes.ContainsKey(headerText))
            {
                columnIndexes[headerText] = col;
            }
        }

        var missingHeaders = columnIndexes.Where(kv => kv.Value < 0).Select(kv => kv.Key).ToList();
        if (missingHeaders.Count > 0)
        {
            throw new InvalidOperationException(
                "The workbook must have a header row with columns: " + string.Join(", ", FamilyImportTemplateGenerator.Headers));
        }

        return columnIndexes;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
