
using BrightSmileDEntal.Data;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure PostgreSQL persistence through Entity Framework Core.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is missing.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();

// Allow requests from the local Angular application and deployed frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://brightsmiledentalfrontend.netlify.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Validate required JWT settings before initializing authentication.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing from configuration.");
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "JWT issuer is missing from configuration.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT audience is missing from configuration.");
}

// Enforce token signature, issuer, audience and expiration validation.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Generate the OpenAPI specification used by Swagger UI and Scalar.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BrightSmile Dental API",
        Version = "v1",
        Description =
            "REST API for dental practice management, " +
            "including authentication, appointments, " +
            "dental services, dentist management and reporting."
    });

    // Define JWT bearer authentication for documented API operations.
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Enter the JWT token obtained from /api/Auth/login."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document),
                new List<string>()
            }
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        // Apply schema changes and seed development accounts.
        await context.Database.MigrateAsync();
        await DbSeeder.SeedDemoUsersAsync(context, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(
            ex,
            "Database migration or demo user seeding failed.");

        throw;
    }
}

// API documentation is enabled in development unless explicitly configured.
var documentationEnabled =
    builder.Configuration.GetValue<bool?>("ApiDocumentation:Enabled")
    ?? app.Environment.IsDevelopment();

if (documentationEnabled)
{
    // Serve the OpenAPI document consumed by both documentation interfaces.
    app.UseSwagger();

    if (app.Environment.IsDevelopment())
    {
        // Keep Swagger UI restricted to the development environment.
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "BrightSmile Dental API v1");

            options.DocumentTitle =
                "BrightSmile Dental - Swagger UI";
        });
    }

    // Expose Scalar as the API reference when documentation is enabled.
    app.MapScalarApiReference("/api-docs", options =>
    {
        options
            .WithTitle("BrightSmile Dental API")
            .WithOpenApiRoutePattern(
                "/swagger/{documentName}/swagger.json")
            .ForceDarkMode();
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

// Authenticate requests before evaluating endpoint authorization policies.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
