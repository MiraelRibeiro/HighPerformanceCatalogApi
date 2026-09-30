using Catalog.Api;
using Catalog.Application;
using Catalog.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

var topProductsCacheSeconds = builder.Configuration.GetValue("OutputCache:TopProductsSeconds", 30);
builder.Services.AddOutputCache(options =>
    options.AddPolicy("TopProducts", policy =>
        policy.Expire(TimeSpan.FromSeconds(topProductsCacheSeconds))
            .SetVaryByQuery("take")
            .Tag("top-products")));

var serviceName = builder.Configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "catalog-api";
var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(ProductQueryService.ActivitySourceName)
            .AddAspNetCoreInstrumentation(options =>
                options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health") &&
                    !context.Request.Path.StartsWithSegments("/metrics"))
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(ProductQueryService.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
        }
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseOutputCache();

app.MapProductEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

await CatalogDatabaseInitializer.InitializeAsync(app.Services);
await app.RunAsync();

public partial class Program;
