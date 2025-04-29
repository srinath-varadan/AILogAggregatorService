using LogAggregatorService;
using LogAggregatorService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

// Expose minimal endpoint to allow HEAD checks
app.Map("/", async context =>
{
    if (context.Request.Method == HttpMethods.Head)
    {
        context.Response.StatusCode = 200;
        return;
    }
    await context.Response.WriteAsync("LogAggregatorService is running");
});
app.Run();