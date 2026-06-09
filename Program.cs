using System.Text;
using DotNetEnv;
using HotstarApi.Data;
using HotstarApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Load environment variables from .env file before builder is created
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ── 0. Kestrel — raise max body size to 2 GB for video uploads ───────────────
builder.Services.Configure<KestrelServerOptions>(options =>
    options.Limits.MaxRequestBodySize = 2_147_483_648);   // 2 GB

// ── 1. Database ──────────────────────────────────────────────────────────────
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Hardcode the server version to avoid AutoDetect trying to connect at startup
var serverVersion = new MySqlServerVersion(new Version(8, 0, 33)); // Adjust if you're using a different MySQL version
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mySqlOptions => 
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ── 2. JWT Authentication ─────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew                = TimeSpan.Zero   // No grace period for token expiry
    };
});

builder.Services.AddAuthorization();

// ── 3. Application Services ───────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStreamGuardService, StreamGuardService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ── 4. Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── 5. Static files (for wwwroot/uploads) ────────────────────────────────────
builder.Services.AddHttpContextAccessor();   // useful for building absolute URLs in controllers

// ── 6. Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Hotstar API",
        Version = "v1",
        Description = "REST API for the Hotstar OTT clone"
    });

    // Add JWT bearer auth to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── 7. CORS (permissive for local dev — tighten for production) ───────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── CORS must come before static files so uploaded assets (images/videos) ─────
// ── receive Access-Control-Allow-Origin headers when served cross-origin ───────
app.UseCors("AllowAll");

// ── Serve files from wwwroot (poster/banner images, uploaded videos) ──────────
app.UseStaticFiles();

// ── Swagger UI ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotstar API v1");
        c.RoutePrefix = string.Empty;   // Serve Swagger at root "/"
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Auto-apply EF Core migrations on startup (convenient for dev / free-tier) ─
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var maxRetries = 10;
    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            if (db.Database.IsRelational())
            {
                db.Database.Migrate();
            }
            break; // Success!
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB Migration] Attempt {retry + 1} of {maxRetries} failed. Retrying in 3 seconds...");
            Console.WriteLine($"Error: {ex.Message}");
            if (retry == maxRetries - 1) throw;
            System.Threading.Thread.Sleep(3000);
        }
    }
    
    // Seed our data (Users, TMDB Posters, YouTube Trailers)
    DatabaseSeeder.Seed(db);
}

app.Run();

public partial class Program { }