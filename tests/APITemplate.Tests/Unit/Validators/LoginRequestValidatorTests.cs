using APITemplate.Application.Features.Auth.Validation;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = new LoginRequest("admin", "password");

        var result = _sut.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, "password", "Username")]
    [InlineData("", "password", "Username")]
    [InlineData("   ", "password", "Username")]
    [InlineData("admin", null, "Password")]
    [InlineData("admin", "", "Password")]
    [InlineData("admin", "   ", "Password")]
    public void Validate_EmptyCredential_IsInvalid(string? username, string? password, string expectedErrorProperty)
    {
        var request = new LoginRequest(username!, password!);

        var result = _sut.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == expectedErrorProperty);
    }

    [Fact]
    public void Validate_BothEmpty_HasMultipleErrors()
    {
        var request = new LoginRequest("", "");

        var result = _sut.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
