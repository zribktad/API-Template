using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using SharedKernel.Application.Errors;
using SharedKernel.Application.Middleware;
using Shouldly;
using Wolverine;
using Xunit;

namespace SharedKernel.Tests.Middleware;

public sealed class ErrorOrValidationMiddlewareTests
{
    [Fact]
    public async Task BeforeAsync_WhenNoValidator_ReturnsContinue()
    {
        TestCommand message = new("valid");

        (HandlerContinuation continuation, ErrorOr<string> response) =
            await ErrorOrValidationMiddleware.BeforeAsync<TestCommand, string>(message);

        continuation.ShouldBe(HandlerContinuation.Continue);
    }

    [Fact]
    public async Task BeforeAsync_WhenValidationPasses_ReturnsContinue()
    {
        TestCommand message = new("valid");
        PassingValidator validator = new();

        (HandlerContinuation continuation, ErrorOr<string> response) =
            await ErrorOrValidationMiddleware.BeforeAsync<TestCommand, string>(message, validator);

        continuation.ShouldBe(HandlerContinuation.Continue);
    }

    [Fact]
    public async Task BeforeAsync_WhenValidationFails_ReturnsStopWithErrors()
    {
        TestCommand message = new("");
        FailingValidator validator = new();

        (HandlerContinuation continuation, ErrorOr<string> response) =
            await ErrorOrValidationMiddleware.BeforeAsync<TestCommand, string>(message, validator);

        continuation.ShouldBe(HandlerContinuation.Stop);
        response.IsError.ShouldBeTrue();
        response.Errors.Count.ShouldBe(1);
        response.FirstError.Code.ShouldBe(ErrorCatalog.General.ValidationFailed);
    }

    [Fact]
    public async Task BeforeAsync_WhenValidationFails_ErrorContainsPropertyNameMetadata()
    {
        TestCommand message = new("");
        FailingValidator validator = new();

        (HandlerContinuation _, ErrorOr<string> response) =
            await ErrorOrValidationMiddleware.BeforeAsync<TestCommand, string>(message, validator);

        response.FirstError.Metadata.ShouldContainKey("propertyName");
        response.FirstError.Metadata["propertyName"].ShouldBe("Value");
    }

    [Fact]
    public async Task BeforeAsync_WhenValidationFails_ErrorContainsAttemptedValueMetadata()
    {
        TestCommand message = new("");
        FailingValidator validator = new();

        (HandlerContinuation _, ErrorOr<string> response) =
            await ErrorOrValidationMiddleware.BeforeAsync<TestCommand, string>(message, validator);

        response.FirstError.Metadata.ShouldContainKey("attemptedValue");
        response.FirstError.Metadata["attemptedValue"].ShouldBe("");
    }

    public sealed record TestCommand(string Value);

    private sealed class PassingValidator : AbstractValidator<TestCommand>
    {
        public PassingValidator()
        {
            // No rules - always passes
        }
    }

    private sealed class FailingValidator : AbstractValidator<TestCommand>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
        }
    }
}
