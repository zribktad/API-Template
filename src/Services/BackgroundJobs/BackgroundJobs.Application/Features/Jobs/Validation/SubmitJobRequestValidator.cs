using BackgroundJobs.Application.Features.Jobs.DTOs;
using SharedKernel.Application.Validation;

namespace BackgroundJobs.Application.Features.Jobs.Validation;

/// <summary>
/// FluentValidation validator for <see cref="SubmitJobRequest"/> that enforces data-annotation constraints,
/// including required job type and optional URL format for the callback.
/// </summary>
public sealed class SubmitJobRequestValidator : DataAnnotationsValidator<SubmitJobRequest>;
