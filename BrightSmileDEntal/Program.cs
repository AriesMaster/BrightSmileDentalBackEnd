
using BrightSmileDEntal.Data;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register application services and configure PostgreSQL persistence.
builder.Services.AddControllers();

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

// Restrict cross-origin requests to approved frontend applications.
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

// Validate JWT configuration before registering authentication services.
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

// Require valid issuer, audience, expiration, and signing credentials.
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

// Configure Swagger to support authenticated API requests using JWT.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
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

// Apply pending migrations and initialize demo accounts in development only.
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        await DbSeeder.SeedDemoUsersAsync(
            context,
            app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(
            ex,
            "Database migration or demo user seeding failed.");

        throw;
    }

    // Expose interactive API documentation in development environments.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

// Authentication must precede authorization in the request pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
