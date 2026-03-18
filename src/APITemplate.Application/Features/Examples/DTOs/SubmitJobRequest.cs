using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record SubmitJobRequest(
    [NotEmpty(ErrorMessage = "Job type is required.")] [MaxLength(100)] string JobType,
    string? Parameters = null
);
