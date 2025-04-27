using LogAggregatorService.Services;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogAggregatorService
{
    public class AggregatorWorker : BackgroundService
    {
        private readonly NewRelicLogCollector _newRelic;
        private readonly PythonLogCollector _python;
        private readonly PrometheusLogCollector _prometheus;
        private readonly AIAnalyzerService _ai;
        private readonly HttpClient _httpClient;

        public AggregatorWorker(NewRelicLogCollector newrelic, PythonLogCollector python, PrometheusLogCollector prometheus, AIAnalyzerService ai, HttpClient httpClient)
        {
            _newRelic = newrelic;
            _python = python;
            _prometheus = prometheus;
            _ai = ai;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var newRelicLogs = await _newRelic.CollectLogsAsync();
                var pythonLogs = await _python.CollectLogs();
                var promLogs = await _prometheus.CollectLogs();

                //var allLogs = splunkLogs.Concat(pythonLogs).Concat(promLogs);
                var allLogs = newRelicLogs.Concat(pythonLogs.Concat(promLogs)).ToList();

                var aiResult = await _ai.AnalyzeLogsAsync(string.Join("\n", allLogs.Take(100)));
                // Log the analysis result or handle it as needed
                if (string.IsNullOrWhiteSpace(aiResult))
                {

                    return;
                }

                try
                {
                    var analysisResult = JObject.Parse(aiResult);
                    Console.WriteLine(analysisResult.ToString());
                    // proceed using analysisResult
                    
                var payload = new
                {
                    labels = new Dictionary<string, string>
            {
                { "__name__", "ai_analyzed_logs" },
                { "health_status", analysisResult.ToString() },
            },
                    samples = new List<object>
            {
                new { value = (int)analysisResult["info_logs_count"], labels = new Dictionary<string, string>{{"type", "info"}} },
                new { value = (int)analysisResult["warning_logs_count"], labels = new Dictionary<string, string>{{"type", "warning"}} },
                new { value = (int)analysisResult["error_logs_count"], labels = new Dictionary<string, string>{{"type", "error"}} },
                new { value = (int)analysisResult["critical_logs_count"], labels = new Dictionary<string, string>{{"type", "critical"}} },
            }
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var username = "2404761";
                var password = "glc_eyJvIjoiMTQxNDE3NiIsIm4iOiJzdGFjay0xMjQwMjYxLWhtLXdyaXRlLXBvY19sb2dfYWdncmVnYXRvcl8xIiwiayI6IkE0NnduQ2M0ODc3b1lCMnY3aDh4RnZmMCIsIm0iOnsiciI6InByb2QtYXAtc291dGgtMSJ9fQ==";
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));



                var request = new HttpRequestMessage(HttpMethod.Post, "https://prometheus-prod-43-prod-ap-south-1.grafana.net/api/prom/push")
                {
                    Content = content
                };
                request.Headers.Add("Authorization", $"Basic {credentials}");

                await _httpClient.SendAsync(request);
                }
                catch (JsonReaderException ex)
                {
                    return;
                }


                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}