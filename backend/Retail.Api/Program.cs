using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Retail.Api;
using Retail.Api.Models;

var builder = WebApplication.CreateBuilder(args);

try
{
    var culture = CultureInfo.GetCultureInfo("fa-IR");
    if (culture.DateTimeFormat.Calendar is not PersianCalendar)
        throw new InvalidOperationException("PersianCalendar requires non-invariant globalization and ICU.");

    _ = new PersianCalendar().ToDateTime(1405, 1, 1, 0, 0, 0, 0);
    builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));
}
catch (Exception exception) when (exception is CultureNotFoundException or TimeZoneNotFoundException
    or InvalidTimeZoneException or InvalidOperationException or ArgumentOutOfRangeException)
{
    throw new InvalidOperationException(
        "Startup requires ICU, PersianCalendar and Asia/Tehran tzdata; calendar/timezone fallback is disabled.",
        exception);
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProductSource, SampleProductSource>();
builder.Services.AddSingleton<PaymentPlanCalculator>();
builder.Services.AddSingleton<TehranDateProvider>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (!context.ProblemDetails.Extensions.ContainsKey("code"))
    {
        var defaults = ApiProblems.Create(context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode);
        context.ProblemDetails.Type = defaults.Type;
        context.ProblemDetails.Title = defaults.Title;
        context.ProblemDetails.Detail = defaults.Detail;
        context.ProblemDetails.Extensions["code"] = defaults.Extensions["code"];
    }
});

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Problem(ApiProblems.Create(500)).ExecuteAsync(context);
}));
app.UseStatusCodePages(context =>
    Results.Problem(ApiProblems.Create(context.HttpContext.Response.StatusCode))
        .ExecuteAsync(context.HttpContext));
app.MapGet("/api/product", (HttpContext context, IProductSource source) => ReadProduct(context, source, null, null));
app.MapGet("/api/product/payment-plan", (HttpContext context, IProductSource source,
    PaymentPlanCalculator calculator, TehranDateProvider dates) => ReadProduct(context, source, calculator, dates));
app.MapPost("/api/product/payment-plan/validity", async (HttpContext context, TehranDateProvider dates) =>
{
    if (!context.Request.HasJsonContentType()) return Results.Problem(ApiProblems.Create(400));
    ValidityRequest request;
    try
    {
        using var body = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
        var root = body.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
            !root.TryGetProperty("baseDate", out var baseDate) || baseDate.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("expiresAt", out var expiresAt) || expiresAt.ValueKind != JsonValueKind.String)
            return Results.Problem(ApiProblems.Create(400));
        request = new ValidityRequest(baseDate.GetString()!, expiresAt.GetString()!);
        _ = TehranDateProvider.ParsePersianDate(request.BaseDate);
        _ = TehranDateProvider.ParseUtc(request.ExpiresAt);
    }
    catch (Exception exception) when (exception is JsonException or ArgumentException)
    { return Results.Problem(ApiProblems.Create(400)); }
    // Request validation is separate: failures reading/converting the server clock are 500.
    return Results.Ok(dates.CheckValidity(request));
});
app.Run();

static IResult ReadProduct(HttpContext context, IProductSource source, PaymentPlanCalculator? calculator, TehranDateProvider? dates)
{
    if (context.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)
        return Results.Problem(ApiProblems.Create(400));
    var raw = source.GetProduct();
    if (raw is null) return Results.Problem(ApiProblems.Create(404, "product_not_found"));
    Product product;
    try { product = SampleProductSource.Validate(raw); }
    catch (ArgumentException exception)
    {
        context.RequestServices.GetRequiredService<ILogger<Program>>().LogError(exception, "Invalid product source data.");
        return Results.Problem(ApiProblems.Create(500, "invalid_product"));
    }
    if (calculator is null) return Results.Ok(product);
    try
    {
        var snapshot = dates!.GetSnapshot();
        var plan = calculator.Calculate(product, snapshot.BaseDate) with
            { ServerTime = snapshot.ServerTime, ExpiresAt = snapshot.ExpiresAt };
        return Results.Ok(plan);
    }
    catch (Exception exception) when (exception is OverflowException or InvalidOperationException or ArgumentException)
    {
        context.RequestServices.GetRequiredService<ILogger<Program>>().LogError(exception, "Payment calculation failed.");
        return Results.Problem(ApiProblems.Create(500, "calculation_failed"));
    }
}

public partial class Program;

internal static class ApiProblems
{
    internal static ProblemDetails Create(int status, string? specificCode = null)
    {
        var (code, title, detail) = status switch
        {
            400 => ("invalid_request", "درخواست معتبر نیست", "ساختار درخواست معتبر نیست."),
            404 => ("not_found", "مسیر پیدا نشد", "مسیر درخواست‌شده وجود ندارد."),
            405 => ("method_not_allowed", "روش درخواست مجاز نیست", "این روش برای مسیر درخواست‌شده مجاز نیست."),
            _ => ("internal_error", "خطای سرور", "انجام درخواست در حال حاضر ممکن نیست.")
        };
        if (specificCode is not null)
        {
            code = specificCode;
            (title, detail) = specificCode switch
            {
                "product_not_found" => ("محصول پیدا نشد", "محصول نمونه در دسترس نیست."),
                "invalid_product" => ("اطلاعات محصول معتبر نیست", "قیمت باید تومان صحیح بین ۵ و ۱۰۰ میلیارد و دارای چهار قسط مثبت باشد."),
                "calculation_failed" => ("محاسبهٔ برنامه ممکن نیست", "تهیهٔ برنامهٔ پرداخت در حال حاضر ممکن نیست."),
                _ => (title, detail)
            };
        }
        return new ProblemDetails
        {
            Type = "about:blank",
            Status = status,
            Title = title,
            Detail = detail,
            Extensions = { ["code"] = code }
        };
    }
}
