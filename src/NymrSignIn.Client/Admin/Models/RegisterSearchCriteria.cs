namespace NymrSignIn.Client.Admin.Models;

public sealed class RegisterSearchCriteria
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public RegisterEntryStatus Status { get; set; } = RegisterEntryStatus.All;
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public RegisterSortField SortBy { get; set; } = RegisterSortField.TimeIn;
    public bool SortDescending { get; set; } = true;
}
