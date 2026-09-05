namespace StudentRegistrar.Api.DTOs;

public sealed class BulkImportRowResult
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class BulkImportResponse
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkImportRowResult> Results { get; set; } = new();
}
