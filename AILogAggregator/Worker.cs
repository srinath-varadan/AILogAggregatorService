using LogAggregatorService.Services;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace LogAggregatorService
{
    public class AggregatorWorker : BackgroundService
    {
        private readonly SplunkLogCollector _splunk;
        private readonly PythonLogCollector _python;
        private readonly PrometheusLogCollector _prometheus;
        private readonly AIAnalyzerService _ai;

        public AggregatorWorker(SplunkLogCollector splunk, PythonLogCollector python, PrometheusLogCollector prometheus, AIAnalyzerService ai)
        {
            _splunk = splunk;
            _python = python;
            _prometheus = prometheus;
            _ai = ai;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var splunkLogs = await _splunk.CollectLogs();
                var pythonLogs = await _python.CollectLogs();
                var promLogs = await _prometheus.CollectLogs();

                var allLogs = splunkLogs.Concat(pythonLogs).Concat(promLogs);

                await _ai.AnalyzeLogs(allLogs);

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}