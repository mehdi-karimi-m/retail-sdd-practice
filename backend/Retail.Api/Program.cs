using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

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
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    Results.Problem(ApiProblems.Create(StatusCodes.Status500InternalServerError)).ExecuteAsync(context)));
app.UseStatusCodePages(context =>
    Results.Problem(ApiProblems.Create(context.HttpContext.Response.StatusCode))
        .ExecuteAsync(context.HttpContext));
app.Run();

public partial class Program;

internal static class ApiProblems
{
    internal static ProblemDetails Create(int status)
    {
        var (code, title, detail) = status switch
        {
            400 => ("invalid_request", "درخواست معتبر نیست", "ساختار درخواست معتبر نیست."),
            404 => ("not_found", "مسیر پیدا نشد", "مسیر درخواست‌شده وجود ندارد."),
            405 => ("method_not_allowed", "روش درخواست مجاز نیست", "این روش برای مسیر درخواست‌شده مجاز نیست."),
            _ => ("internal_error", "خطای سرور", "انجام درخواست در حال حاضر ممکن نیست.")
        };
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
