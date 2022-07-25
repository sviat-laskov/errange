using System.Net;
using Errange.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Errange;

public abstract class ErrorPolicy
{
    public abstract Type ExceptionType { get; init; }

    public abstract ErrangeProblemDetails GenerateError(Exception exception, HttpContext context, IServiceProvider serviceProvider);
}

public class ErrorPolicy<TException> : ErrorPolicy where TException : Exception
{
    private readonly List<IErrorDataItemsBuilder<TException>> _dataItemBuilders = new();

    private string? Code { get; set; }

    private int HttpStatusCode { get; set; } = StatusCodes.Status500InternalServerError; // ToDo Require code setup.

    private string? Title { get; set; }

    private string? Detail { get; set; }

    public override Type ExceptionType { get; init; } = typeof(TException);

    public override ErrangeProblemDetails GenerateError(Exception exception, HttpContext context, IServiceProvider serviceProvider) => GenerateError((TException)exception, context, serviceProvider);

    public ErrangeProblemDetails GenerateError(TException exception, HttpContext context, IServiceProvider serviceProvider) => new()
    {
        Code = Code,
        Status = HttpStatusCode,
        Detail = Detail,
        Data = _dataItemBuilders
            .SelectMany(dataItemBuilder => dataItemBuilder.BuildIfShouldBeIncludedIntoError(exception, context, serviceProvider))
            .ToDictionary(dataItem => dataItem.Key, dataItem => dataItem)
    };

    public ErrorPolicy<TException> WithCode(string code)
    {
        //todo empty or whitespace

        Code = code;
        return this;
    }

    /// <param name="httpStatusCode">Use <see cref="StatusCodes" />.</param>
    public ErrorPolicy<TException> WithHttpStatusCode(int httpStatusCode)
    {
        HttpStatusCode = httpStatusCode;
        return this;
    }

    public ErrorPolicy<TException> WithHttpStatusCode(HttpStatusCode httpStatusCode)
    {
        HttpStatusCode = (int)httpStatusCode;
        return this;
    }

    public ErrorPolicy<TException> WithTitle(string title)
    {
        //todo empty or whitespace
        Title = title;
        return this;
    }

    public ErrorPolicy<TException> WithDetail(string? detail)
    {
        //todo empty or whitespace
        Detail = detail;
        return this;
    }

    public ErrorPolicy<TException> WithDataItemForEach<TSourceItem>(
        Func<TException, IEnumerable<TSourceItem>> sourceItemsSelector,
        Func<TSourceItem, TException, HttpContext, IServiceProvider, string> keyFactory,
        Func<TSourceItem, TException, HttpContext, IServiceProvider, object?> valueFactory,
        Func<TSourceItem, TException, HttpContext, IServiceProvider, IEnumerable<string>> messagesFactory)
    {
        var dataItemBuilder = new ErrorDataItemsBuilder<TException, TSourceItem>
        {
            SourceItemsSelector = sourceItemsSelector,
            KeyFactory = keyFactory,
            ValueFactory = valueFactory,
            MessagesFactory = messagesFactory,
            ErrorPolicy = this
        };
        _dataItemBuilders.Add(dataItemBuilder);

        return this;
    }

    public ErrorDataItemBuilder<TDataItem, TException> WithDataItem<TDataItem>(
        Func<TException, HttpContext, IServiceProvider, string> keyFactory,
        Func<TException, HttpContext, IServiceProvider, TDataItem>? valueFactory,
        params string[] messages)
    {
        if (keyFactory == null) throw new ArgumentNullException(nameof(keyFactory));
        if (messages.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Error data item message cannot be null or whitespace.", nameof(messages));

        var dataItemBuilder = new ErrorDataItemBuilder<TDataItem, TException>
        {
            KeyFactory = keyFactory,
            ValueFactory = valueFactory,
            Messages = messages,
            ErrorPolicy = this
        };
        _dataItemBuilders.Add(dataItemBuilder);
        return dataItemBuilder;
    }
}