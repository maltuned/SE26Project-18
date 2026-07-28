using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.Messaging;
using SE26Project_18.Api.Infrastructure.VectorStore;
using SE26Project_18.Api.Repositories;
using SE26Project_18.Api.Services;
using SE26Project_18.Api.Services.Recommendations;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "The default database connection string is not configured."
    );
var serverVersion = ServerVersion.AutoDetect(connectionString);
MariaDbCompatibility.Validate(serverVersion);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion);
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = Encoding.UTF8.GetBytes(jwtSection["Secret"]!);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await context.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Authentication required",
                        Detail = "A valid access token is required.",
                        Instance = context.Request.Path,
                    },
                    context.HttpContext.RequestAborted
                );
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Forbidden",
                        Detail = "You are not allowed to perform this operation.",
                        Instance = context.Request.Path,
                    },
                    context.HttpContext.RequestAborted
                );
            },
        };
    });

builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));

builder.Services.AddSingleton<ITokenService, TokenService>();
builder
    .Services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder
    .Services.AddOptions<EmbeddingOptions>()
    .BindConfiguration(EmbeddingOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder
    .Services.AddOptions<EmbeddingSyncOptions>()
    .BindConfiguration(EmbeddingSyncOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
builder
    .Services.AddOptions<MilvusOptions>()
    .BindConfiguration(MilvusOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IVectorStore, MilvusVectorStore>();
builder.Services.AddSingleton<RecommendationVectorRepository>();
builder.Services.AddHostedService<RecommendationVectorStoreInitializer>();
builder.Services.AddRabbitMqBatchConsumer<EmbeddingSyncRequested, EmbeddingSyncBatchConsumer>(
    EmbeddingSyncRequested.EventName,
    EmbeddingSyncRequested.QueueName
);
builder.Services.AddHostedService<EmbeddingSyncOutboxDispatcher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<ITagCatalogService, TagCatalogService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<
    IRecruitmentRecommendationAlgorithm,
    EmbeddingRecruitmentRecommendationAlgorithm
>();
builder.Services.AddScoped<IUserPreferenceProfileBuilder, UserPreferenceProfileBuilder>();
builder.Services.AddScoped<IEmbeddingSyncScheduler, EmbeddingSyncScheduler>();
builder.Services.AddScoped<TagEmbeddingBuilder>();
builder.Services.AddScoped<EmbeddingProfileBatchBuilder>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder
    .Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Instance = context.HttpContext.Request.Path,
            };

            return new BadRequestObjectResult(details)
            {
                ContentTypes = { "application/problem+json" },
            };
        };
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.MapControllers();
app.Run();
