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

// ==================================================
// 1. LOGGING (Serilog)
// ==================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pastport-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ==================================================
// 2. SERVICES (DI Container)
// ==================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("PastPort.Infrastructure")));

// --- Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- JWT & Auth Configuration
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var secretKey = jwtSettingsSection["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettingsSection["Issuer"],
        ValidAudience = jwtSettingsSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? "DefaultFallbackKeyForSafety"))
    };
});

// --- Dependency Injection (Repositories & Services)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ISceneRepository, SceneRepository>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISceneService, SceneService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentService, PayPalService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAIConversationService, MockAIConversationService>();
builder.Services.AddSingleton<INpcSessionStore, NpcSessionStore>();

// --- External Configurations
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<PayPalSettings>(builder.Configuration.GetSection("PayPal"));
builder.Services.Configure<NpcAISettings>(builder.Configuration.GetSection("NpcAI"));
builder.Services.AddHttpClient<INpcAIService, NpcAIService>();

// --- Swagger Configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PastPort API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ==================================================
// 3. BUILD APP & DIRECTORIES
// ==================================================
var app = builder.Build();

// إنشاء المجلدات بأمان (بإستخدام Try-Catch لضمان عدم توقف التطبيق في الاستضافة)
var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
string[] paths = { Path.Combine(webRoot, "uploads"), Path.Combine(webRoot, "assets") };

foreach (var path in paths)
{
    try
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Log.Information("📁 Directory created: {Path}", path);
        }
    }
    catch (Exception ex)
    {
        Log.Warning("⚠️ Could not create directory {Path}: {Message}", path, ex.Message);
    }
}

// ==================================================
// 4. MIDDLEWARE PIPELINE
// ==================================================

// 1. الأخطاء أولاً
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 2. Swagger (قبل الـ Auth والـ Redirection لضمان فتحه دائماً)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PastPort API v1");
    c.RoutePrefix = "swagger"; // الرابط سيكون: yourdomain.com/swagger
});

// 3. الأساسيات
app.UseHttpsRedirection();
app.UseStaticFiles(); // لملفات wwwroot الافتراضية
app.UseRouting();
app.UseCors("AllowAll");

// 4. الحماية
app.UseAuthentication();
app.UseAuthorization();

// 5. الـ Custom Middlewares الخاصة بك
app.UseRequestLogging();
app.UseCustomExceptionHandler();

// 6. الروابط
app.MapControllers();

// ==================================================
// 5. SEEDING & RUN
// ==================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "School", "Museum", "Enterprise", "Individual" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    catch (Exception ex) { Log.Error(ex, "❌ Seeding failed"); }
}

Log.Information("🚀 PastPort API Starting...");
app.Run();