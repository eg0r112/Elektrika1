namespace Elektrika.Api.Middleware;

public sealed class StaticFileGuardMiddleware
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".css", ".js", ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg", ".ico", ".woff", ".woff2", ".map",
    };

    private static readonly string[] BlockedSegments =
    [
        "/server/", "/bin/", "/obj/", "/.git/", "/.env",
    ];

    private readonly RequestDelegate _next;

    public StaticFileGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var lower = path.ToLowerInvariant();

        if (BlockedSegments.Any(lower.Contains) ||
            lower.Contains("appsettings", StringComparison.Ordinal) ||
            lower.EndsWith(".cs", StringComparison.Ordinal) ||
            lower.EndsWith(".json", StringComparison.Ordinal) && !lower.EndsWith("manifest.json", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (path != "/" && !path.EndsWith('/'))
        {
            var ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext) && !AllowedExtensions.Contains(ext))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }

        await _next(context);
    }
}
