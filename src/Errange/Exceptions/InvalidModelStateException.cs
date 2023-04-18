using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Errange.Exceptions;

internal class InvalidModelStateException : Exception
{
    public ModelStateDictionary ModelState { get; init; }

    public InvalidModelStateException(ModelStateDictionary modelState)
    {
        if (modelState.IsValid) throw new ArgumentException($"Can't create '{nameof(InvalidModelStateException)}' from valid model state.", nameof(modelState));
        ModelState = modelState;
    }
}