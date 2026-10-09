using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
namespace Retail.Api.Tests;

// Test-only assertion for the schema keywords used by this repository's OpenAPI contract.
internal static class ContractAssert
{
    internal static void Schema(JsonElement value, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "openapi.json")));
        Assert.Equal("1.2.0", document.RootElement.GetProperty("info").GetProperty("version").GetString());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Check(value, schemas.GetProperty(name), schemas);
    }

    private static void Check(JsonElement value, JsonElement schema, JsonElement schemas)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        { Check(value, schemas.GetProperty(reference.GetString()!.Split('/')[^1]), schemas); return; }
        var type = schema.GetProperty("type").GetString();
        Assert.True(type switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            _ => false
        }, $"Expected schema type {type}, got {value.ValueKind}");
        if (schema.TryGetProperty("enum", out var choices))
            Assert.Contains(choices.EnumerateArray(), x => x.GetRawText() == value.GetRawText());
        if (type == "object")
        {
            if (schema.TryGetProperty("required", out var required))
                foreach (var field in required.EnumerateArray()) Assert.True(value.TryGetProperty(field.GetString()!, out _), field.GetString());
            foreach (var field in value.EnumerateObject())
            {
                if (schema.TryGetProperty("properties", out var properties) && properties.TryGetProperty(field.Name, out var property))
                    Check(field.Value, property, schemas);
                else if (schema.TryGetProperty("additionalProperties", out var additional))
                {
                    Assert.NotEqual(JsonValueKind.False, additional.ValueKind);
                    if (additional.ValueKind == JsonValueKind.Object) Check(field.Value, additional, schemas);
                }
            }
        }
        if (type == "array")
        {
            if (schema.TryGetProperty("minItems", out var min)) Assert.True(value.GetArrayLength() >= min.GetInt32());
            if (schema.TryGetProperty("maxItems", out var max)) Assert.True(value.GetArrayLength() <= max.GetInt32());
            foreach (var item in value.EnumerateArray()) Check(item, schema.GetProperty("items"), schemas);
        }
        if (type == "string")
        {
            var text = value.GetString()!;
            if (schema.TryGetProperty("pattern", out var pattern)) Assert.Matches(pattern.GetString()!, text);
            if (schema.TryGetProperty("minLength", out var min)) Assert.True(text.Length >= min.GetInt32());
            if (schema.TryGetProperty("maxLength", out var max)) Assert.True(text.Length <= max.GetInt32());
        }
        if (type == "integer")
        {
            if (schema.TryGetProperty("minimum", out var min)) Assert.True(value.GetInt64() >= min.GetInt64());
            if (schema.TryGetProperty("maximum", out var max)) Assert.True(value.GetInt64() <= max.GetInt64());
        }
    }
    internal static async Task<JsonElement> Json(HttpResponseMessage response, int status, string schema)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(schema == "ProblemDetails" ? "application/problem+json" : "application/json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Schema(document.RootElement, schema);
        return document.RootElement.Clone();
    }
    internal static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        var json = await Json(response, status, "ProblemDetails");
        Assert.Equal(status, json.GetProperty("status").GetInt32());
        Assert.Equal(code, json.GetProperty("code").GetString());
        Assert.Matches("[\u0600-\u06ff]", json.GetProperty("detail").GetString()!);
        Assert.False(json.TryGetProperty("installments", out _));
        Assert.DoesNotContain("private", json.GetRawText());
        Assert.False(json.TryGetProperty("stackTrace", out _));
    }
}
