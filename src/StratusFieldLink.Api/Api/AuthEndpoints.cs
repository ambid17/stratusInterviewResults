namespace StratusFieldLink.Api.Api;

/// <summary>
/// Stand-in for the real device-code flow. It hands out an opaque token so the
/// Unity client has something to attach; nothing verifies it. Do not copy this
/// into anything that matters.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/token", (TokenRequestDto request) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceCode) || string.IsNullOrWhiteSpace(request.CrewId))
            {
                return Results.BadRequest(new { error = "deviceCode and crewId are required." });
            }

            var token = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{request.CrewId}:{request.DeviceCode}:{Guid.NewGuid():N}"));

            return Results.Ok(new TokenResponseDto(token, request.CrewId, DateTime.UtcNow.AddHours(12)));
        });

        return app;
    }
}
