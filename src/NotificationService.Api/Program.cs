using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using MassTransit;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using MongoDB.Driver;

using NotificationService.Infrastructure;
using NotificationService.Domain;

using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration.GetConnectionString("Mongo")
    ?? throw new InvalidOperationException("ConnectionStrings:Mongo is required");

var jwtKey = builder.Configuration["Jwt:Key"] ?? "super-secret-dev-key-change-me-in-prod-32chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "hotelier-identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "hotelier";
var rabbitHost = builder.Configuration["Rabbit:Host"] ?? "rabbitmq";
var rabbitUser = builder.Configuration["Rabbit:Username"] ?? "guest";
var rabbitPass = builder.Configuration["Rabbit:Password"] ?? "guest";

// -------------------------------------------------------
// MongoDB
// -------------------------------------------------------
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("hotelier_notification");
builder.Services.AddSingleton(mongoClient);
builder.Services.AddSingleton(mongoDatabase);
builder.Services.AddSingleton<ISseConnectionManager, SseConnectionManager>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// -------------------------------------------------------
// Authentication (JWT Bearer)
// -------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        // Allow SSE clients to pass the token via ?access_token= query param
        // (EventSource API does not support custom headers)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------
// MassTransit + RabbitMQ
// -------------------------------------------------------
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(ReservationCreatedConsumer).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// -------------------------------------------------------
// API / Swagger
// -------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------------------------------------------------------
// OpenTelemetry
// -------------------------------------------------------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddMeter("System.Net.Http")
        .AddMeter("System.Net.NameResolution")
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.MapGet("/health", () => "OK");
app.MapGet("/test", () => new { message = "Notification service running" });

await app.RunAsync();
