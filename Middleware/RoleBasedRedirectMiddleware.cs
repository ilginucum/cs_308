using System.Security.Claims;
using e_commerce.Models;
using Microsoft.AspNetCore.Http;

namespace e_commerce.Middleware
{
    public class RoleBasedRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleBasedRedirectMiddleware> _logger;

        public RoleBasedRedirectMiddleware(RequestDelegate next, ILogger<RoleBasedRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Only check on the home page
                if (context.User.Identity?.IsAuthenticated == true &&
                    context.Request.Path.Value?.Equals("/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var userRole = context.User.FindFirstValue(ClaimTypes.Role);
                    
                    if (!string.IsNullOrEmpty(userRole))
                    {
                        string redirectPath = userRole switch
                        {
                            nameof(UserType.SalesManager) => "/Sales",
                            nameof(UserType.ProductManager) => "/Product",
                            _ => null
                        };

                        if (redirectPath != null)
                        {
                            _logger.LogInformation($"Redirecting {userRole} to {redirectPath}");
                            context.Response.Redirect(redirectPath);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RoleBasedRedirectMiddleware");
            }

            await _next(context);
        }
    }

    // Extension method for easier middleware registration
    public static class RoleBasedRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseRoleBasedRedirect(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RoleBasedRedirectMiddleware>();
        }
    }
}