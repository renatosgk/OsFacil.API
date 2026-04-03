using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OsFacil.Data;
using OsFacil.HealthChecks;
using OsFacil.Messaging;
using OsFacil.Profiles;
using Serilog;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Serilog (Configuração de Logs) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/osfacil_log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. Controllers + JSON Config ---

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

builder.Services.AddHostedService<RabbitMqConsumer>();

builder.Services.AddSingleton<RabbitMqProducer>();

// --- 3. Entity Framework + Oracle ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

// --- 4. Health Checks ---
builder.Services.AddHealthChecks()
    .AddCheck<ApiHealthCheck>(
        "osfacil_api",
        tags: new[] { "api" });


// --- 5. OpenTelemetry (Observabilidade) ---
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("OsFacil"))
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation() 
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter();
    });

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            project = "OsFacil - Sistema de Oficina",
            timestamp = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

try
{
    Log.Information("Iniciando a API OsFacil...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A API OsFacil falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
