using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Features.Product.Validation;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Validators;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _sut = new();

    // --- Data Annotation tests ([NotEmpty], [MaxLength], [Range]) ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Annotation_InvalidName_IsInvalid(string? name)
    {
        var request = new CreateProductRequest(name!, null, 9.99m);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Annotation_NameExceeds200Characters_IsInvalid()
    {
        var request = new CreateProductRequest(new string('A', 201), null, 9.99m);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Name"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Annotation_PriceZeroOrNegative_IsInvalid(decimal price)
    {
        var request = new CreateProductRequest("Valid Name", null, price);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains("Price"));
    }

    // --- FluentValidation tests (cross-field rules) ---

    [Theory]
    [InlineData(1001, null, false, "Description")]
    [InlineData(1001, "Detailed description", true, null)]
    [InlineData(999, null, true, null)]
    public void FluentValidation_DescriptionRule_BasedOnPrice(
        decimal price,
        string? description,
        bool expectedIsValid,
        string? expectedErrorProperty)
    {
        var result = _sut.Validate(new CreateProductRequest("Any name", description, price));

        result.IsValid.ShouldBe(expectedIsValid);
        if (expectedErrorProperty is null)
        {
            result.Errors.ShouldNotContain(e => e.PropertyName == "Description");
        }
        else
        {
            result.Errors.ShouldContain(e => e.PropertyName == expectedErrorProperty);
        }
    }
}
