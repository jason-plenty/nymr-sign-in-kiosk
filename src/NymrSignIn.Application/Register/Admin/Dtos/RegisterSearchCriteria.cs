namespace NymrSignIn.Application.Register.Admin.Dtos;

public sealed record RegisterSearchCriteria
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public RegisterEntryStatus Status { get; init; } = RegisterEntryStatus.All;
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public RegisterSortField SortBy { get; init; } = RegisterSortField.TimeIn;
    public bool SortDescending { get; init; } = true;
}
