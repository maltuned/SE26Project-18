using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Hubs;
using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Entities;
using Minio;
using SE26Project_18.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = Encoding.UTF8.GetBytes(jwtSection["Secret"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/v1/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

// Services
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var minioConfig = sp.GetRequiredService<IConfiguration>().GetSection("Minio");
    return new MinioClient()
        .WithEndpoint(minioConfig["Endpoint"]!)
        .WithCredentials(minioConfig["AccessKey"]!, minioConfig["SecretKey"]!)
        .WithSSL(bool.Parse(minioConfig["UseSsl"] ?? "false"))
        .Build();
});
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<MapperService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// CORS - allow frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new EnumMemberJsonConverter());
    });
builder.Services.AddOpenApi();

var app = builder.Build();

app.Urls.Add("http://0.0.0.0:5111");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/api/v1/chatHub");

// Seed default admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Admins.Any())
    {
        db.Admins.Add(new Admin("admin", BCrypt.Net.BCrypt.HashPassword("123456")));
        db.SaveChanges();
    }
}

app.Run();