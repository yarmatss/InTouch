using InTouch.MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace InTouch.MVC.Middleware;

public class LastActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public LastActivityMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = userManager.GetUserId(context.User);
            if (!string.IsNullOrEmpty(userId))
            {
                var cacheKey = $"LastActive_{userId}";

                if (!_cache.TryGetValue(cacheKey, out _))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        user.LastActive = DateTime.UtcNow;
                        await userManager.UpdateAsync(user);

                        // Cache for 1 minute to prevent frequent updates
                        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));
                    }
                }
            }
        }

        await _next(context);
    }
}