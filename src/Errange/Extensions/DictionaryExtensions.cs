using Errange.ViewModels;

namespace Errange.Extensions;

public static class DictionaryExtensions
{
    public static IDictionary<string, ProblemDataItemVM> AddIfDoesNotExist(this IDictionary<string, ProblemDataItemVM> dictionary, ProblemDataItemVM problemDataItemVM)
    {
        dictionary.TryAdd(problemDataItemVM.Key, problemDataItemVM);
        return dictionary;
    }
}