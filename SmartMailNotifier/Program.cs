using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartMailNotifier.Data;
using SmartMailNotifier.Helpers;
using SmartMailNotifier.Repository;
using SmartMailNotifier.Repository.Interfaces;
using SmartMailNotifier.Services;
using SmartMailNotifier.Services.Background;
using SmartMailNotifier.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Swagger with JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


// =========================
// ✅ DATABASE CONFIG (FINAL FIX)
// =========================

string connectionString = null;

// 🔥 Priority 1: Railway MYSQL_PUBLIC_URL
var dbUrl = Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL");

if (!string.IsNullOrEmpty(dbUrl))
{
    var uri = new Uri(dbUrl);
    var userInfo = uri.UserInfo.Split(':');

    connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};User={userInfo[0]};Password={userInfo[1]};SslMode=Required;";
}

// 🔥 Priority 2: Render DB_CONNECTION
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
}

// 🔥 Fallback: appsettings.json
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Database connection string is missing!");
}

// ✅ NO AutoDetect (prevents crash)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 32)))
);


// =========================
// ✅ JWT CONFIG
// =========================

var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSection["Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = jwtSection["Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = jwtSection["Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE");

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing!");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),

        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();


// =========================
// ✅ CORS
// =========================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://smart-mail-assistant.netlify.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});


// =========================
// ✅ DEPENDENCY INJECTION
// =========================

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<GmailService>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<SendEmailService>();


var app = builder.Build();


// =========================
// ✅ APPLY MIGRATIONS SAFELY
// =========================

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
catch (Exception ex)
{
    Console.WriteLine("Migration failed: " + ex.Message);
}


// =========================
// ✅ MIDDLEWARE
// =========================

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowReact");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// =========================
// ✅ RENDER PORT FIX
// =========================

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();