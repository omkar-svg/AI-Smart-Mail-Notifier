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

// =========================
// ✅ SERVICES
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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
// ✅ DATABASE (FINAL FIX)
// =========================

var dbUrl = Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL");

if (string.IsNullOrEmpty(dbUrl))
{
    throw new Exception("MYSQL_PUBLIC_URL is missing in environment variables!");
}

var uri = new Uri(dbUrl);
var userInfo = uri.UserInfo.Split(':');

var connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};User={userInfo[0]};Password={userInfo[1]};SslMode=Required;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 32)))
);

// =========================
// ✅ JWT CONFIG
// =========================

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT_KEY is missing!");
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
// ✅ DEPENDENCIES
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
// ✅ SAFE MIGRATION
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