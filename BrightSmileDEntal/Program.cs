
using BrightSmileDEntal.Data;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//  SERVICES

// Register MVC controllers.
builder.Services.AddControllers();

// Register PostgreSQL database context.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is missing."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register application services.
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();

// CORS CONFIGURATION


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://brightsmiledentalfrontend.netlify.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// --------------------------------------------------
// 3. JWT AUTHENTICATION
// --------------------------------------------------

// Retrieve and validate JWT configuration.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing from configuration."
    );
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes."
    );
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "JWT issuer is missing from configuration."
    );
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT audience is missing from configuration."
    );
}

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
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
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };
});

// --------------------------------------------------
// 4. AUTHORIZATION
// --------------------------------------------------

builder.Services.AddAuthorization();

// --------------------------------------------------
// 5. SWAGGER / OPENAPI
// --------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------------------------------------
// 6. BUILD APPLICATION
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// 7. DATABASE MIGRATIONS AND DEMO SEEDING
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation(
        "Starting database migration and demo user seeding."
    );

    try
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        await DbSeeder.SeedDemoUsersAsync(
            context,
            app.Logger
        );
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(
            ex,
            "Database migration or demo user seeding failed."
        );

        throw;
    }
}

//HTTP REQUEST PIPELINE


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();

app.UseAuthorization();

// Map controller endpoints.
app.MapControllers();

//  START APPLICATION


app.Run();
