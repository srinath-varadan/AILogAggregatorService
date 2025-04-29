using LogAggregatorService.Services;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace LogAggregatorService
{
    public class AggregatorWorker : BackgroundService
    {
        private readonly NewRelicLogCollector _newRelic;
        private readonly PythonLogCollector _python;
        private readonly PrometheusLogCollector _prometheus;
        private readonly AIAnalyzerService _ai;
        private readonly HttpClient _httpClient;

        private DateTime _lastAiAnalysisTime = DateTime.MinValue;

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

                var allLogs = newRelicLogs.Concat(pythonLogs).Concat(promLogs).ToList();

                await PushRawLogsToLoki(allLogs);

                // Run AI analysis every 30 minutes
                if (DateTime.UtcNow - _lastAiAnalysisTime > TimeSpan.FromMinutes(30))
                {
                    var aiResult = await _ai.AnalyzeLogsAsync(string.Join("\n", allLogs));
                    if (!string.IsNullOrWhiteSpace(aiResult))
                    {
                        await PushAiAnalysisToLoki(aiResult);
                        _lastAiAnalysisTime = DateTime.UtcNow;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task PushRawLogsToLoki(List<string> logs)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var streams = new
            {
                streams = new[]
                {
                    new
                    {
                        stream = new { job = "logaggregator-raw-logs" },
                        values = logs.Select(log => new[] {
                            timestamp.ToString(),
                            log
                        }).ToArray()
                    }
                }
            };

            await PushToLoki(streams);
        }

        private async Task PushAiAnalysisToLoki(string aiSummaryJson)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var streams = new
            {
                streams = new[]
                {
                    new
                    {
                        stream = new { job = "logaggregator-ai-analysis" },
                        values = new[]
                        {
                            new[] {
                                timestamp.ToString(),
                                aiSummaryJson
                            }
                        }
                    }
                }
            };

            await PushToLoki(streams);
        }

        private async Task PushToLoki(object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var username = "1198057";  // Replace
            var password = "glc_eyJvIjoiMTQxNDE3NiIsIm4iOiJ3cml0ZWxvZ3Mtd3JpdGVsb2dzdG9rZW4iLCJrIjoiQ3hUMWRvYzFNOTRhTDA5NjhEajVIako4IiwibSI6eyJyIjoidXMifX0=";    // Replace
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://logs-prod-028.grafana.net/loki/api/v1/push")
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Basic {credentials}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Loki push failed: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
            else
            {
                Console.WriteLine($"Loki push succeeded: {response.StatusCode}");  
            } 
        }
    }
}