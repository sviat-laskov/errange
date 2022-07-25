using System.Text.Json.Serialization;

namespace Errange.ViewModels;

public class ProblemDataItemVM
{
    [JsonIgnore]
    public string Key { get; init; } = null!;

    public object? Value { get; init; }

    public ISet<string> Messages { get; init; } = new HashSet<string>();

    // ToDo: from, to, equal, other requirements
}

public class ProblemDataItemVM<TValue> : ProblemDataItemVM
{
    public new TValue? Value
    {
        get => (TValue?)base.Value;
        init => base.Value = value;
    }
}