using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LogAggregatorService.Services
{
    public class PythonLogCollector
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonLogCollector> _logger;

        public PythonLogCollector(ILogger<PythonLogCollector> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<IEnumerable<string>> CollectLogs()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://stock-stream-api.onrender.com/logs");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch logs from Python App. Status: {response.StatusCode}");
                    return new List<string>();
                }

                var content = await response.Content.ReadAsStringAsync();

                // Split log file content into individual log lines
                var logs = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching logs from Python App");
                return new List<string>();
            }
        }
    }
}