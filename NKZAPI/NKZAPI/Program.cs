using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NKZAPI.Controllers;
using NKZAPI.Repositories;
using NKZAPI.Services.TeamServices;
using NKZAPI.Services.UserServices;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.Extensions.DependencyInjection;
using NKZAPI.Services.DiscordServices;
using NKZAPI.Services.EmailServices;
using NKZAPI.Services.RiotService;
using NKZAPI.Services.SubscriptionServices;
using NKZAPI.Services.WalletServices;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

static void LoadDotEnv(string path)
{
    if (!File.Exists(path)) return;

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#")) continue;

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0) continue;

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"');
        if (!string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

LoadDotEnv(Path.Combine(builder.Environment.ContentRootPath, ".env"));
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

static string GetClientPartitionKey(HttpContext context)
{
    var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? context.User?.FindFirst("Id")?.Value;

    if (!string.IsNullOrWhiteSpace(userId)) return $"user:{userId}";

    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    var ip = forwardedFor?.Split(',').FirstOrDefault()?.Trim()
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    return $"ip:{ip}";
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 240,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("AuthPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientPartitionKey(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("VerificationPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientPartitionKey(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("RiotPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientPartitionKey(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 4,
            Window = TimeSpan.FromMinutes(2),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("UploadPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientPartitionKey(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 12,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("PaymentPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientPartitionKey(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("GeneralWritePolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        GetClientPartitionKey(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var configuredOriginsArray = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

var configuredOriginsValue = builder.Configuration["Cors:AllowedOrigins"];

var configuredOrigins = configuredOriginsArray
    .Concat((configuredOriginsValue ?? "")
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

var allowedOrigins = configuredOrigins
    .Concat(new[]
    {
        "http://localhost:5173",
        "https://localhost:5173"
    })
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

string? connectionString = builder.Configuration.GetConnectionString("ConnectionString");

if (connectionString is null)
{
    throw new InvalidOperationException("Connection string 'ConnectionString' not found.");
}

builder.Services.AddDbContext<NKZAPI.Data.NKZAPIContext>(options =>
    options.UseNpgsql(connectionString),
    contextLifetime: ServiceLifetime.Scoped,
    optionsLifetime: ServiceLifetime.Scoped);

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey

    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
        ValidateAudience = false,
        ValidateIssuer = false
    };
});

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserServices>();
builder.Services.AddScoped<NKZAPI.Services.PlayerServices.IPlayerInterface, NKZAPI.Services.PlayerServices.PlayerServices>();
builder.Services.AddScoped<TeamRepository>();
builder.Services.AddScoped<TeamServices>();
builder.Services.AddScoped<LeagueRepository>();
builder.Services.AddScoped<TournamentRepository>();
builder.Services.AddScoped<PlayerRepository>();

builder.Services.AddScoped<NKZAPI.Services.AuthServices.IAuthInterface, NKZAPI.Services.AuthServices.AuthService>();
builder.Services.AddScoped<NKZAPI.Services.PassService.IPasswordInterface, NKZAPI.Services.PassService.PasswordService>();
builder.Services.AddScoped<NKZAPI.Services.UserServices.IUserInterface, NKZAPI.Services.UserServices.UserServices>();
builder.Services.AddScoped<NKZAPI.Services.TeamServices.ITeamInterface, NKZAPI.Services.TeamServices.TeamServices>();
builder.Services.AddScoped<NKZAPI.Services.LeagueServices.ILeagueInterface, NKZAPI.Services.LeagueServices.LeagueServices>();
builder.Services.AddScoped<NKZAPI.Services.TournamentServices.ITournamentInterface, NKZAPI.Services.TournamentServices.TournamentServices>();
builder.Services.AddScoped<NKZAPI.Services.PlayerServices.IPlayerInterface, NKZAPI.Services.PlayerServices.PlayerServices>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IDiscordVerificationService, DiscordBotClient>();
builder.Services.AddHttpClient<IDiscordTeamRoleService, DiscordTeamRoleService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IRiotService, RiotService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Required for services that need access to the current HttpContext (e.g. authorization checks inside services)
builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.DocumentTitle = "API NKZ - V1";
        c.SwaggerEndpoint("/swagger/v1/swagger.json","API NKZ - V1");
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("FrontendCors");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NKZAPI.Data.NKZAPIContext>();
    await dbContext.Database.MigrateAsync();
    await NKZAPI.Data.LeagueSchemaRepair.EnsureLeagueStandingRatingColumnsAsync(dbContext);
}

await NKZAPI.Data.AdminSeed.SeedAsync(app.Services, app.Configuration);
app.Run();
