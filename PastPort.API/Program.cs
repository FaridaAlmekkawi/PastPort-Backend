using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PastPort.API.Extensions;
using PastPort.Application.Common;
using PastPort.Application.Identity;
using PastPort.Application.Interfaces;
using PastPort.Application.Services;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;
using PastPort.Infrastructure.Data;
using PastPort.Infrastructure.Data.Repositories;
using PastPort.Infrastructure.ExternalServices.AI;
using PastPort.Infrastructure.ExternalServices.Payment;
using PastPort.Infrastructure.ExternalServices.Storage;
using PastPort.Infrastructure.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Serilog
// ---------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/pastport-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------------------
// Add Services (DI) - Application / Infrastructure / API
// ---------------------------

builder.Services.AddControllers();

// --- Database (Infrastructure) ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Identity (Infrastructure) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Password hashing (tune if necessary)
builder.Services.Configure<PasswordHasherOptions>(opts => opts.IterationCount = 210000);

// --- Jwt Settings (Application) ---
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);

var secretKey = jwtSettingsSection["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured");
}

// --- Authentication & External Providers (API) ---
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

// JWT Bearer
authBuilder.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettingsSection["Issuer"],
        ValidAudience = jwtSettingsSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Log.Warning("Authentication failed: {Error}", ctx.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Log.Information("Token validated for user: {User}", ctx.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

// Google
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Events.OnCreatingTicket = ctx =>
        {
            Log.Information("Google authentication successful for user: {Email}",
                ctx.Principal?.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value);
            return Task.CompletedTask;
        };
    });
    Log.Information("Google Authentication enabled");
}
else
{
    Log.Warning("Google Authentication is not configured - skipping");
}

// Facebook
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.SaveTokens = true;
        options.Fields.Add("name");
        options.Fields.Add("email");
        options.Fields.Add("picture");
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
        options.Events.OnCreatingTicket = ctx =>
        {
            Log.Information("Facebook authentication successful for user: {Email}",
                ctx.Principal?.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value);
            return Task.CompletedTask;
        };
    });
    Log.Information("Facebook Authentication enabled");
}
else
{
    Log.Warning("Facebook Authentication is not configured - skipping");
}

// --- Application services & Repositories (DI container)
// Note: keep registrations narrow and consistent with Clean Architecture:
//  - Domain.Interfaces live in Domain
//  - Repositories in Infrastructure implement those interfaces
//  - Services in Application implement app-level use cases
// ---------------------------

// Generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Infrastructure repositories
builder.Services.AddScoped<ISceneRepository, SceneRepository>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Application services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISceneService, SceneService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentService, PayPalService>();

// External / infra services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAIConversationService, MockAIConversationService>();

// PayPal settings (Infrastructure)
builder.Services.Configure<PayPalSettings>(builder.Configuration.GetSection("PayPal"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Swagger (with JWT)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PastPort API",
        Version = "v1",
        Description = "API for PastPort - Virtual Reality Historical Experience Platform"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// ---------------------------
// Build the app
// ---------------------------
var app = builder.Build();

// ---------------------------
// Ensure directories that rely on app.Environment are created AFTER build
// ---------------------------
var uploadPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

// Ensure assets folder for static assets
var assetsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "assets");
if (!Directory.Exists(assetsPath))
{
    Directory.CreateDirectory(assetsPath);
}

// ---------------------------
// Middleware pipeline
// ---------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PastPort API v1"));
}
else
{
    // In Production, enable a user-friendly exception handler + HSTS if needed
    app.UseExceptionHandler("/error"); // customize your error endpoint
    app.UseHsts();
}

app.UseHttpsRedirection();

// Static files: default wwwroot + uploads + assets
app.UseStaticFiles(); // serves wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(assetsPath),
    RequestPath = "/assets"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads")),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // cache for 1 year
        const int durationInSeconds = 60 * 60 * 24 * 365;
        ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={durationInSeconds}");
    }
});

// Custom Middlewares (logging, error handling)
app.UseRequestLogging();
app.UseCustomExceptionHandler();

// CORS
app.UseCors("AllowAll");

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// ---------------------------
// Seed Roles (run once at startup)
// ---------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "School", "Museum", "Enterprise", "Individual" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Log.Information("Role '{Role}' created successfully", role);
            }
        }

        Log.Information("All roles initialized");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding roles");
    }
}

Log.Information("PastPort API Starting...");
try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
