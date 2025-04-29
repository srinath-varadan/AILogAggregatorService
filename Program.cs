using LogAggregatorService;
using LogAggregatorService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<NewRelicLogCollector>();
builder.Services.AddSingleton<PythonLogCollector>();
builder.Services.AddSingleton<PrometheusLogCollector>();
builder.Services.AddSingleton<AIAnalyzerService>();
builder.Services.AddHostedService<AggregatorWorker>();

var app = builder.Build();

// Expose a basic HTTP endpoint for health check (Alloy scrapes here)
app.MapGet("/", () => "AILogAggregator is running.");

app.Run();