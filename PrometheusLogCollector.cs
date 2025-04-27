using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LogAggregatorService.Services
{
    public class PrometheusLogCollector
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PrometheusLogCollector> _logger;
        private readonly List<string> _prometheusEndpoints = new()
        {
            "https://portfolio-api-cptv.onrender.com/metrics",
            "https://stock-stream-api.onrender.com/metrics"
        };

        public PrometheusLogCollector(ILogger<PrometheusLogCollector> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<IEnumerable<string>> CollectLogs()
        {
            var collectedLogs = new List<string>();

            foreach (var endpoint in _prometheusEndpoints)
            {
                try
                {
                    var response = await _httpClient.GetAsync(endpoint);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to fetch metrics from {endpoint}. Status: {response.StatusCode}");
                        continue;
                    }

                    var metricsContent = await response.Content.ReadAsStringAsync();

                    // For now treat each metric line as a 'log line'
                    var lines = metricsContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        if (!line.StartsWith("#")) // Skip comments
                            collectedLogs.Add($"[Prometheus][{endpoint}] {line}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred while scraping Prometheus metrics from {endpoint}");
                }
            }

            return collectedLogs;
        }
    }
}