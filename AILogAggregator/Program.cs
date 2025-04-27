using LogAggregatorService;
using LogAggregatorService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<AggregatorWorker>();
        services.AddSingleton<SplunkLogCollector>();
        services.AddSingleton<PythonLogCollector>();
        services.AddSingleton<PrometheusLogCollector>();
        services.AddSingleton<AIAnalyzerService>();
    });

var app = builder.Build();

app.Run();