namespace StudentRegistrar.Api.Services;

public sealed class MemberImportRow
{
    public int RowNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Minimal CSV parser for the "firstName,lastName,email" bulk member import format.
/// Handles a header row (column order/case-insensitive) and basic quoted fields.
/// Not a general-purpose CSV library \u2014 kept small and scoped to this one import shape.
/// </summary>
public static class MemberImportCsvParser
{
    private static readonly string[] RequiredHeaders = { "firstname", "lastname", "email" };

    public static IReadOnlyList<MemberImportRow> Parse(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        var rows = new List<MemberImportRow>();

        string? headerLine = ReadNonBlankLine(reader);
        if (headerLine == null)
        {
            throw new InvalidOperationException("CSV file is empty.");
        }

        var headerFields = ParseLine(headerLine);
        var columnIndexes = RequiredHeaders.ToDictionary(
            h => h,
            h => Array.FindIndex(headerFields, f => string.Equals(f.Trim(), h, StringComparison.OrdinalIgnoreCase)));

        if (columnIndexes.Values.Any(idx => idx < 0))
        {
            throw new InvalidOperationException(
                "CSV must have a header row with 'firstName', 'lastName', and 'email' columns.");
        }

        int rowNumber = 1; // header is row 1
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseLine(line);
            rows.Add(new MemberImportRow
            {
                RowNumber = rowNumber,
                FirstName = GetField(fields, columnIndexes["firstname"]),
                LastName = GetField(fields, columnIndexes["lastname"]),
                Email = GetField(fields, columnIndexes["email"])
            });
        }

        return rows;
    }

    private static string? ReadNonBlankLine(StreamReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
        }
        return null;
    }

    private static string GetField(string[] fields, int index) =>
        index >= 0 && index < fields.Length ? fields[index].Trim() : string.Empty;

    private static string[] ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
