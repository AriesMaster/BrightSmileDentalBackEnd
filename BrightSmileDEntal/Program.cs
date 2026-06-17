using BrightSmileDEntal.Data;
using BrightSmileDEntal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Create the application builder which holds configuration, logging and DI container
var builder = WebApplication.CreateBuilder(args);

// Register MVC controller support (API endpoints)
builder.Services.AddControllers();

// Register the EF Core DbContext with a PostgreSQL provider.
// Connection string is read from configuration (appsettings.json or environment).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services. JwtService is a scoped service used to create/validate tokens.
builder.Services.AddScoped<JwtService>();

// Configure CORS to allow requests from the Angular development server.
// The named policy can be applied globally or per-controller.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Read the symmetric signing key for JWTs from configuration.
var jwtKey = builder.Configuration["Jwt:Key"];

// Configure JWT bearer authentication. Tokens will be validated for issuer, audience,
// lifetime and signing key. Make sure the Jwt:Issuer, Jwt:Audience, and Jwt:Key values
// are set in configuration for both token creation and validation.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            )
        };
    });

// Add authorization services (policy-based or role-based can be added later)
builder.Services.AddAuthorization();

// Swagger/OpenAPI tools for API documentation and testing (enabled in Development)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build the application pipeline
var app = builder.Build();

// In Development environment enable Swagger UI for interactive API exploration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Apply CORS policy. Place before authentication if CORS preflight requests need to be handled
// without requiring credentials or authentication.
app.UseCors("AllowAngularApp");

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes to endpoints
app.MapControllers();

// Start the web application
app.Run();
