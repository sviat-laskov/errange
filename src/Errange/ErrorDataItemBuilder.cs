using Errange.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Errange;

public class ErrorDataItemBuilder<TException> : IErrorDataItemsBuilder<TException> where TException : Exception
{
    public ErrorPolicy<TException> ErrorPolicy = null!;
    public Func<TException, HttpContext, IServiceProvider, string> KeyFactory = null!;
    public string[] Messages = null!;
    public Func<TException, HttpContext, IServiceProvider, bool> Predicate = (_, _, _) => true;

    public virtual IEnumerable<ProblemDataItemVM> BuildIfShouldBeIncludedIntoError(TException exception, HttpContext context, IServiceProvider serviceProvider)
    {
        if (ShouldBeIncludedIntoError(exception, context, serviceProvider)) yield return Build(exception, context, serviceProvider);
    }

    public ErrorPolicy<TException> When(Func<TException, HttpContext, IServiceProvider, bool> predicate)
    {
        Predicate = predicate;
        return ErrorPolicy;
    }

    public bool ShouldBeIncludedIntoError(TException exception, HttpContext context, IServiceProvider serviceProvider) => Predicate(exception, context, serviceProvider);

    public ProblemDataItemVM Build(TException exception, HttpContext context, IServiceProvider serviceProvider) => new()
    {
        Key = KeyFactory(exception, context, serviceProvider),
        Messages = Messages.ToHashSet()
    };
}

public class ErrorDataItemBuilder<TDataItem, TException> : ErrorDataItemBuilder<TException> where TException : Exception
{
    public Func<TDataItem, TException, HttpContext, IServiceProvider, bool> PredicateWithDataItem = (_, _, _, _) => true;
    public Func<TException, HttpContext, IServiceProvider, TDataItem>? ValueFactory;

    public bool ShouldBeIncludedIntoError(TDataItem value, TException exception, HttpContext context, IServiceProvider serviceProvider) => PredicateWithDataItem(value, exception, context, serviceProvider);

    public new ProblemDataItemVM<TDataItem> Build(TException exception, HttpContext context, IServiceProvider serviceProvider)
    {
        var value = default(TDataItem); //todo check
        if (ValueFactory is not null) value = ValueFactory.Invoke(exception, context, serviceProvider);

        ProblemDataItemVM errorDataItem = base.Build(exception, context, serviceProvider);
        return new ProblemDataItemVM<TDataItem>
        {
            Key = errorDataItem.Key,
            Value = value,
            Messages = errorDataItem.Messages
        };
    }

    public ErrorPolicy<TException> When(Func<TDataItem, TException, HttpContext, IServiceProvider, bool> predicateWithDataItem)
    {
        PredicateWithDataItem = predicateWithDataItem;
        return ErrorPolicy;
    }

    public override IEnumerable<ProblemDataItemVM> BuildIfShouldBeIncludedIntoError(TException exception, HttpContext context, IServiceProvider serviceProvider)
    {
        ProblemDataItemVM<TDataItem> dataItem = Build(exception, context, serviceProvider);
        if (ShouldBeIncludedIntoError(dataItem.Value!, exception, context, serviceProvider)) yield return dataItem;
    }
}

public class ErrorDataItemsBuilder<TException, TSourceItem> : IErrorDataItemsBuilder<TException> where TException : Exception
{
    public ErrorPolicy<TException> ErrorPolicy = null!;
    public Func<TSourceItem, TException, HttpContext, IServiceProvider, string> KeyFactory;
    public Func<TSourceItem, TException, HttpContext, IServiceProvider, IEnumerable<string>> MessagesFactory;
    public Func<TException, IEnumerable<TSourceItem>> SourceItemsSelector;
    public Func<TSourceItem, TException, HttpContext, IServiceProvider, object?> ValueFactory;

    public IEnumerable<ProblemDataItemVM> BuildIfShouldBeIncludedIntoError(TException exception, HttpContext context, IServiceProvider serviceProvider) => SourceItemsSelector(exception)
        .Select(sourceItem => new ProblemDataItemVM
        {
            Key = KeyFactory(sourceItem, exception, context, serviceProvider),
            Value = ValueFactory(sourceItem, exception, context, serviceProvider),
            Messages = MessagesFactory(sourceItem, exception, context, serviceProvider).ToHashSet()
        });
}

public interface IErrorDataItemsBuilder<in TException> where TException : Exception
{
    public IEnumerable<ProblemDataItemVM> BuildIfShouldBeIncludedIntoError(TException exception, HttpContext context, IServiceProvider serviceProvider);
}