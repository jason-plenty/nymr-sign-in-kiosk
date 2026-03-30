namespace NymrSignIn.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' blob: data:; " +
            "style-src 'self' 'unsafe-inline'; " +
            "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; " +
            "connect-src 'self'";

        await _next(context);
    }
}
