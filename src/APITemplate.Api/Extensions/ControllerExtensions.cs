using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Extensions;

public static class ControllerExtensions
{
    public static string GetApiVersion(this ControllerBase controller) =>
        controller.HttpContext.GetRequestedApiVersion()!.ToString();
}
