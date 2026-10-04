using Microsoft.AspNetCore.Diagnostics;

namespace TradeHub.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            ArgumentException ex    => (StatusCodes.Status400BadRequest, ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            _                       => (StatusCodes.Status500InternalServerError,
                                        "Something went wrong"),
        };

        // Like System.out.println in your Spring handler, but only for unexpected errors
        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true; // handled, stop here
    }
}