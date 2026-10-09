namespace Retail.Api.Models;
public sealed record ValidityResponse(string BaseDate, string ServerTime, string ExpiresAt, bool IsValid);
