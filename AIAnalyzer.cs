using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogAggregatorService.Services
{
    public class AIAnalyzerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIAnalyzerService> _logger;
        private const string CohereEndpoint = "https://api.cohere.ai/v1/chat"; // Cohere Chat Endpoint
        private const string CohereApiKey = "ffn4SNGDfbbPOzVhPhdhgc2eqmfNglkFQBGpZKcy"; // Put your actual API key here

        public AIAnalyzerService(HttpClient httpClient, ILogger<AIAnalyzerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> AnalyzeLogsAsync(string logs)
        {
            try
            {
                const string prompt = @"You are a log analysis engine.

Given a batch of application logs, your tasks are:
1. Analyze the logs to understand severity, patterns, and anomalies.
2. Segregate logs into: Info, Warning, Error, Critical, Anomaly.
3. Summarize:
    - Highlight critical errors and warning trends
    - Point out anomalies or abnormal patterns
    - Give a brief health status (Healthy / Warning / Degraded)

Respond strictly in this JSON format:

{
  ""info_logs_count"": number,
  ""warning_logs_count"": number,
  ""error_logs_count"": number,
  ""critical_logs_count"": number,
  ""anomalies_detected"": true/false,
  ""summary"": ""short health overview""
}";

                var requestBody = new
                {
                    message = $"{prompt}\nLogs:\n{logs}",
                    model = "command-r-plus", // Best model for summarization
                    temperature = 0.2
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var request = new HttpRequestMessage(HttpMethod.Post, CohereEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CohereApiKey);
                request.Content = jsonContent;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to analyze logs via Cohere. StatusCode: {response.StatusCode}, Response: {errorBody}");
                    return $"[AI Analysis Failed: {response.StatusCode}]";
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                var responseJson = JObject.Parse(responseBody);
                var resultContent = responseJson["text"]?.ToString();

                return resultContent ?? "[No content returned]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during AI log analysis with Cohere.");
                return "[AI Analysis Error]";
            }
        }
    }
}