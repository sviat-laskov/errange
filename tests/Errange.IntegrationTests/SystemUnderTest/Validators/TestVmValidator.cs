using Errange.IntegrationTests.SystemUnderTest.ViewModels;
using FluentValidation;

namespace Errange.IntegrationTests.SystemUnderTest.Validators;

public class TestVmValidator : AbstractValidator<TestVM>
{
    public TestVmValidator()
    {
        RuleFor(testVM => testVM.RequiredProperty)
            .NotNull();
        RuleFor(testVM => testVM.RequiredPropertyWithRangeLimit)
            .InclusiveBetween(from: 1, to: 2)
            .Must(value => value % 2 == 1).WithMessage("Must be odd.");
    }
}