using APITemplate.Application.Common.Middleware;
using ErrorOr;
using FluentValidation;
using Shouldly;
using Wolverine;
using Xunit;

namespace APITemplate.Tests.Unit.Middleware;

public sealed record MiddlewareTestCommand(string Name, decimal Price);

public class ErrorOrValidationMiddlewareTests
{
    private sealed class AlwaysValidValidator : AbstractValidator<MiddlewareTestCommand>
    {
        public AlwaysValidValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class NameRequiredValidator : AbstractValidator<MiddlewareTestCommand>
    {
        public NameRequiredValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    private sealed class MultiFieldValidator : AbstractValidator<MiddlewareTestCommand>
    {
        public MultiFieldValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        }
    }

    [Fact]
    public async Task BeforeAsync_WhenNoValidator_ReturnsContinue()
    {
        var (continuation, result) = await ErrorOrValidationMiddleware.BeforeAsync<
            MiddlewareTestCommand,
            string
        >(new MiddlewareTestCommand("Widget", 10m), validator: null);

        continuation.ShouldBe(HandlerContinuation.Continue);
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public async Task BeforeAsync_WhenValidationPasses_ReturnsContinue()
    {
        var validator = new AlwaysValidValidator();

        var (continuation, result) = await ErrorOrValidationMiddleware.BeforeAsync<
            MiddlewareTestCommand,
            string
        >(new MiddlewareTestCommand("Widget", 10m), validator);

        continuation.ShouldBe(HandlerContinuation.Continue);
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public async Task BeforeAsync_WhenValidationFails_ReturnsStopWithErrors()
    {
        var validator = new NameRequiredValidator();

        var (continuation, result) = await ErrorOrValidationMiddleware.BeforeAsync<
            MiddlewareTestCommand,
            string
        >(new MiddlewareTestCommand("", 10m), validator);

        continuation.ShouldBe(HandlerContinuation.Stop);
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Description.ShouldBe("Name is required.");
    }

    [Fact]
    public async Task BeforeAsync_WhenMultipleValidationFailures_ReturnsAllErrors()
    {
        var validator = new MultiFieldValidator();

        var (continuation, result) = await ErrorOrValidationMiddleware.BeforeAsync<
            MiddlewareTestCommand,
            string
        >(new MiddlewareTestCommand("", 0m), validator);

        continuation.ShouldBe(HandlerContinuation.Stop);
        result.IsError.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.All(e => e.Type == ErrorType.Validation).ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Description == "Name is required.");
        result.Errors.ShouldContain(e => e.Description == "Price must be greater than zero.");
    }
}
