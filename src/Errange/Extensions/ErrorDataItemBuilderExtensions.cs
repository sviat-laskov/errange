using Microsoft.AspNetCore.Http;

namespace Errange.Extensions;

public static class ErrorDataItemBuilderExtensions
{
    public static ErrorDataItemBuilder<TDataItem, TException> WithDataItem<TDataItem, TException>(
        this ErrorDataItemBuilder<TException> errorDataItemBuilder,
        Func<TException, HttpContext, IServiceProvider, string> keyFactory,
        Func<TException, HttpContext, IServiceProvider, TDataItem>? valueFactory,
        params string[] messages) where TException : Exception => errorDataItemBuilder.ErrorPolicy.WithDataItem(keyFactory, valueFactory, messages);
}