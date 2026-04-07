using API_Calinout_Project.Configurations;
using API_Calinout_Project.Data;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Infrastructure.Exceptions;
using API_Calinout_Project.Services;
using API_Calinout_Project.Services.Features;
using API_Calinout_Project.Services.Interfaces;
using API_Calinout_Project.Services.Interfaces.Features;
using API_Calinout_Project.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace API_Calinout_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;

            // =================================================================
            // 1. SERVICES CONFIGURATION (Dependency Injection)
            // =================================================================

            // 1.1 Database Context (SQL Server)
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
                {
                    sql.EnableRetryOnFailure(15, TimeSpan.FromSeconds(10), null);
                    sql.CommandTimeout(60);
                }
                ));

            // limit requests in the auth controller
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            // User Limit requests
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;



                options.AddPolicy("UserRequestsLimit", HttpContext =>
                {
                    var userId = HttpContext.User.FindFirstValue("sub") ?? "anonymous";
                    return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: userId,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0
                            });
                });
            });

        

        // 1.2 Identity Configuration (User Management)
        // We configure password complexity here.
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;       // Simplify for testing? Set true for Prod
                options.Password.RequireUppercase = true;   // Simplify for testing?
                options.Password.RequireNonAlphanumeric = true;
                options.SignIn.RequireConfirmedEmail = false; // Set true if you implement email confirmation later
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

    //  Bind JSON to C# Object
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            var jwt = config.GetSection("JwtSettings").Get<JwtSettings>()!;

    // 1.3 Authentication & JWT Configuration (The Security Guard)
    // This overrides the default "Cookie" behavior of Identity.
    builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Set true in Production
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero, // Removes default 5 min delay on expiration

                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
                };
                options.Events = new JwtBearerEvents
                {
                    // Good: structured, low-volume, security-relevant
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning(context.Exception,
                            "JWT Authentication failed :: {Reason} :: IP {IpAddress} :: Path {Path}",
                            context.Exception.Message,
                            context.HttpContext.Connection.RemoteIpAddress,
                            context.HttpContext.Request.Path);

                        return Task.CompletedTask;
                    }

                };
            });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    opt.AddPolicy("RequirePower", p => p.RequireClaim("RequirePower", "general"));

});

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFoodTypeService, FoodTypeService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<IDailyLogService, DailyLogService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Utilities
builder.Services.AddHttpContextAccessor(); // Needed for getting IP Address inside Services

// 1.4 Controllers
builder.Services.AddControllers();

// 1.5 Swagger / OpenAPI (With JWT Support)
// This enables the "Authorize" padlock in the Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calinout API", Version = "v1" });

    // Define Security Scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },

                        },
                        Array.Empty<string>()
                    }
    });
});



// 1. Add Problem Details support(global exceptions)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();



// mapper configuration
MappingConfig.Configure();

var app = builder.Build();

// =================================================================
//  DATABASE SEEDING (Run Migrations & Create Roles)
// =================================================================
// We use 'await' so the app doesn't start accepting requests until the DB is ready.
await DbInitializer.SeedAsync(app);

app.UseExceptionHandler();

// 2.1 Developer Tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2.2 Security Middleware
app.UseHttpsRedirection();

app.UseRateLimiter();

// 2.4 Authentication (Who are you?)
app.UseAuthentication();

// 2.5 Authorization (Are you allowed here?)
app.UseAuthorization();

// 2.6 Routing
app.MapControllers();

app.Run();
        }
    }
}

// for testing
public partial class Program { }