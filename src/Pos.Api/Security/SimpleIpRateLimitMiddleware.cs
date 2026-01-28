using System.Collections.Concurrent;

namespace Pos.Api.Security
{
    public class SimpleIpRateLimitMiddleware : IMiddleware
    {
        private sealed record Counter(int Count, DateTime WindowStartUtc);
        private static readonly ConcurrentDictionary<string, Counter> _counters = new();


        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var path = context.Request.Path.Value ?? "";

            if (!path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{ip}:{path}";

            const int limit = 10;
            var window = TimeSpan.FromMinutes(1);
            var now = DateTime.UtcNow;

            var current = _counters.GetOrAdd(key, _ => new Counter(0, now));

            if (now - current.WindowStartUtc > window)
            {
                current = new Counter(0, now);
                _counters[key] = current;
            }

            var newCount = current.Count + 1;
            _counters[key] = current with { Count = newCount };

            if (newCount > limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Try again later." });
                return;
            }

            await next(context);
        }
    }
}
