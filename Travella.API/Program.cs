using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Travella.API.Middleware;
using Travella.Application.Interfaces;
using Travella.Application.Services;
using Travella.Infrastructure.Persistence;
using Travella.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers().AddJsonOptions(options =>
{
    // Match Angular expectations (`startDate`, `overnightLocation`, `attractions`, etc.)
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtSection = configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer = jwtSection["Issuer"] ?? "Travella.API";
var jwtAudience = jwtSection["Audience"] ?? "Travella.Client";

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

services.AddAuthorization();

// Infrastructure
services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
services.AddScoped<UnitOfWork>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

// Repositories
services.AddScoped<IItineraryRepository, ItineraryRepository>();
services.AddScoped<IBookingRepository, BookingRepository>();
services.AddScoped<IStaffRepository, StaffRepository>();
services.AddScoped<IAuthRepository, AuthRepository>();
services.AddScoped<IApplicationRepository, ApplicationRepository>();
services.AddScoped<ICompanyRepository, CompanyRepository>();
services.AddScoped<IReviewRepository, ReviewRepository>();
services.AddScoped<IPricingRepository, PricingRepository>();

// Services
services.AddScoped<IItineraryService, ItineraryService>();
services.AddScoped<IBookingService, BookingService>();
services.AddScoped<IStaffService, StaffService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IAdminService, AdminService>();
services.AddScoped<IApplicationService, ApplicationService>();
services.AddScoped<ICompanyService, CompanyService>();
services.AddScoped<IReviewService, ReviewService>();
services.AddScoped<IPricingService, PricingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("FrontendPolicy");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();