using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace FileStorage.Api.Extensions;

/// <summary>
/// Presentation-layer helper extensions for <see cref="ControllerBase"/> providing
/// convenient access to API versioning metadata.
/// </summary>
public static class ControllerExtensions
{
    public static string GetApiVersion(this ControllerBase controller) =>
        controller.HttpContext.GetRequestedApiVersion()!.ToString();
}
