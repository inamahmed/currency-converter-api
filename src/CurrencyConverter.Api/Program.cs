using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using CurrencyConverter.Api.Infrastructure.Middlewares;
using CurrencyConverter.Api.Infrastructure.Factories;
using CurrencyConverter.Api.Application.Configuration;
using CurrencyConverter.Api.Application.Interfaces;
using CurrencyConverter.Api.Services;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.Configure<PollyOptions>(builder.Configuration.GetSection("PollySettings"));
var pollySettings = builder.Configuration.GetSection("PollySettings").Get<PollyOptions>();

// Configure Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)  // Read from appsettings
          .Enrich.FromLogContext()  // Enrich logs with additional context (like request IDs, etc.)
.WriteTo.Seq(builder.Configuration["Serilog:SeqServerUrl"])); // Add Seq server URL in appsettings.json

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);        // Default version: v1.0
    options.AssumeDefaultVersionWhenUnspecified = true;       // Use default version if not specified
    options.ReportApiVersions = true;                         // Show supported versions in response headers
    options.ApiVersionReader = new HeaderApiVersionReader("x-api-version"); // Accept version from header
});


builder.Services.Configure<FrankfurterApiOptions>(builder.Configuration.GetSection("FrankfurterApi"));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection"); // Set the Redis server connection string
    options.InstanceName = "CurrencyConverter_API"; // Optional: Add instance name if you want
});

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CurrencyConverterAPI"))
             .AddJaegerExporter(options =>
             {
                 options.AgentHost = builder.Configuration["Jaeger:AgentHost"];
                 options.AgentPort = int.Parse(builder.Configuration["Jaeger:AgentPort"]);
             });
    });

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();


builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddTransient<FrankfurterExchangeRateService>();

builder.Services.AddHttpClient("DefaultHttpClient")
    .AddPolicyHandler(ResilienceMiddleware.GetRetryPolicy(pollySettings))
    .AddPolicyHandler(ResilienceMiddleware.GetCircuitBreakerPolicy(pollySettings));


builder.Services.AddTransient<IExchangeRateServiceFactory, ExchangeRateServiceFactory>();
// Add Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CurrencyConverter API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Enter 'Bearer' followed by a space and your token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Middleware
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CurrencyConverter API v1");
    });
}
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TracingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
