namespace StudentRegistrar.Api.DTOs;

public sealed class FamilyImportChildResult
{
    public string ChildFirstName { get; set; } = string.Empty;
    public string ChildLastName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class FamilyImportFamilyResult
{
    public string ParentEmail { get; set; } = string.Empty;
    public bool ParentAccountCreated { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FamilyImportChildResult> Children { get; set; } = new();
}

public sealed class FamilyImportParseError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class FamilyImportResponse
{
    public int TotalFamilies { get; set; }
    public int TotalChildren { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<FamilyImportParseError> ParseErrors { get; set; } = new();
    public List<FamilyImportFamilyResult> Families { get; set; } = new();
}
