namespace Errange.ViewModels;

public class ExceptionInfoVM
{
    public string Message { get; init; } = null!;

    public string? StackTrace { get; init; }
}