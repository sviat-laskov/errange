using Microsoft.AspNetCore.Mvc;

namespace Errange.ViewModels;

public class ErrangeProblemDetails : ProblemDetails
{
    /// <example>CUS001</example>
    public string? Code { get; set; } = null!;

    public IDictionary<string, ProblemDataItemVM> Data { get; init; } = new Dictionary<string, ProblemDataItemVM>(StringComparer.Ordinal);
}