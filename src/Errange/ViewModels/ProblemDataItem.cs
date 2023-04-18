using System.Text.Json.Serialization;

namespace Errange.ViewModels;

public class ProblemDataItem
{
    [JsonIgnore]
    public string Key { get; set; } = null!;

    public object? Value { get; set; }

    public ISet<string> Messages { get; init; } = new HashSet<string>();
}

public class ProblemDataItem<TValue> : ProblemDataItem
{
    public new TValue? Value
    {
        get => (TValue?)base.Value;
        set => base.Value = value;
    }
}