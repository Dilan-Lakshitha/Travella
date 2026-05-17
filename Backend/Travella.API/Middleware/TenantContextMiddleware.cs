namespace Travella.API.Middleware
{
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var userId = context.User?.FindFirst("userId")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Items["UserId"] = userId;
            }

            var role = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(role))
            {
                context.Items["Role"] = role;
            }

            var companyId = context.User?.FindFirst("companyId")?.Value;
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                context.Items["CompanyId"] = companyId;
            }

            await _next(context);
        }
    }
}
