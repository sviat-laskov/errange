using Errange.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Errange;

public class ProblemDataItemBuilder<TException> where TException : Exception
{
    protected readonly Func<TException, HttpContext, IServiceProvider, string> KeyFactory;
    private Func<TException, HttpContext, IServiceProvider, bool> _predicate = (_, _, _) => true;
    private ProblemDataItemBuilder<TException>? _problemDataItemWithValueBuilder;
    protected HashSet<string> Messages = new();

    /// <summary>
    ///     Returns <see cref="ProblemPolicy<TException>"/> to add more data items.
    /// </summary>
    public ProblemPolicy<TException> ProblemPolicy { get; }

    internal ProblemDataItemBuilder(Func<TException, HttpContext, IServiceProvider, string> keyFactory, ProblemPolicy<TException> problemPolicy)
    {
        KeyFactory = keyFactory;
        ProblemPolicy = problemPolicy;
    }

    internal virtual bool ShouldBeBuilt(TException exception, HttpContext httpContext, IServiceProvider serviceProvider) => _problemDataItemWithValueBuilder?.ShouldBeBuilt(exception, httpContext, serviceProvider) ?? _predicate(exception, httpContext, serviceProvider);

    internal virtual ProblemDataItem Build(TException exception, HttpContext httpContext, IServiceProvider serviceProvider) => _problemDataItemWithValueBuilder?.Build(exception, httpContext, serviceProvider) ??
        new ProblemDataItem
        {
            Key = KeyFactory(exception, httpContext, serviceProvider),
            Messages = Messages
        };

    /// <summary>
    ///     Adds value to data item.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> WithValue<TValue>(Func<TException, HttpContext, IServiceProvider, TValue?> valueFactory)
    {
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
        _problemDataItemWithValueBuilder = new ProblemDataItemBuilder<TException, TValue>(KeyFactory, valueFactory, Messages, ProblemPolicy);
        return (ProblemDataItemBuilder<TException, TValue>)_problemDataItemWithValueBuilder;
    }

    /// <summary>
    ///     Adds value to data item.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> WithValue<TValue>(Func<TException, TValue?> valueFactory)
    {
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
        return WithValue((exception, _, _) => valueFactory(exception));
    }

    /// <summary>
    ///     Adds value to data item.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> WithValue<TValue>(Func<HttpContext, TValue?> valueFactory)
    {
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
        return WithValue((_, httpContext, _) => valueFactory(httpContext));
    }

    /// <summary>
    ///     Adds value to data item.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> WithValue<TService, TValue>(Func<TService, TValue?> valueFactory) where TService : notnull
    {
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
        return WithValue((_, _, serviceProvider) => valueFactory(serviceProvider.GetRequiredService<TService>()));
    }

    /// <summary>
    ///     Adds value to data item.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> WithValue<TValue>(TValue value) => WithValue((_, _, _) => value);

    /// <summary>
    ///     Sets predicate, that controls when data item should be included to response.
    /// </summary>
    public ProblemDataItemBuilder<TException> When(Func<TException, HttpContext, IServiceProvider, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>
    ///     Sets predicate, that controls when data item should be included to response.
    /// </summary>
    public ProblemDataItemBuilder<TException> When(Func<TException, bool> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return When((exception, _, _) => predicate(exception));
    }

    /// <summary>
    ///     Sets predicate, that controls when data item should be included to response.
    /// </summary>
    public ProblemDataItemBuilder<TException> When(Func<HttpContext, bool> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return When((_, httpContext, _) => predicate(httpContext));
    }

    /// <summary>
    ///     Sets predicate, that controls when data item should be included to response.
    /// </summary>
    public ProblemDataItemBuilder<TException> When<TService>(Func<TService, bool> predicate) where TService : notnull
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return When((_, _, serviceProvider) => predicate(serviceProvider.GetRequiredService<TService>()));
    }

    /// <summary>
    ///     Adds additional information about data item.
    /// </summary>
    public ProblemDataItemBuilder<TException> WithMessages(params string[] messages)
    {
        foreach (string message in messages.Where(message => !string.IsNullOrWhiteSpace(message))) Messages.Add(message);
        return this;
    }
}

public class ProblemDataItemBuilder<TException, TValue> : ProblemDataItemBuilder<TException> where TException : Exception
{
    private readonly Func<TException, HttpContext, IServiceProvider, TValue?> _valueFactory;
    private bool _isValueGenerated;
    private Func<TValue?, TException, HttpContext, IServiceProvider, bool> _predicate = (_, _, _, _) => true;
    private TValue? _value;

    internal ProblemDataItemBuilder(
        Func<TException, HttpContext, IServiceProvider, string> keyFactory,
        Func<TException, HttpContext, IServiceProvider, TValue?> valueFactory,
        HashSet<string> messages,
        ProblemPolicy<TException> problemPolicy) : base(keyFactory, problemPolicy)
    {
        _valueFactory = valueFactory;
        Messages = messages;
    }

    /// <summary>
    ///     Sets predicate, that controls when data item should be included to response.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> When(Func<TValue?, TException, HttpContext, IServiceProvider, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>
    ///     Sets predicate, that controls when data item should be included to response.
    /// </summary>
    public ProblemDataItemBuilder<TException, TValue> When(Func<TValue?, bool> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return When((value, _, _, _) => predicate(value));
    }

    private TValue? GetOrGenerateValue(TException exception, HttpContext httpContext, IServiceProvider serviceProvider)
    {
        if (_isValueGenerated) return _value;

        _isValueGenerated = true;
        return _value = _valueFactory(exception, httpContext, serviceProvider);
    }

    internal override bool ShouldBeBuilt(TException exception, HttpContext httpContext, IServiceProvider serviceProvider) => _predicate(
        GetOrGenerateValue(exception, httpContext, serviceProvider),
        exception,
        httpContext,
        serviceProvider);

    internal override ProblemDataItem<TValue> Build(TException exception, HttpContext httpContext, IServiceProvider serviceProvider) => new()
    {
        Key = KeyFactory(exception, httpContext, serviceProvider),
        Value = GetOrGenerateValue(exception, httpContext, serviceProvider),
        Messages = Messages
    };
}