using System.Text.Json;

namespace TestCommon;

public static class TestJsonOptions
{
    public static JsonSerializerOptions CaseInsensitive { get; } =
        new() { PropertyNameCaseInsensitive = true };
}
