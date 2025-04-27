using System;
using System.Net.Http;
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
        private const string HuggingFaceEndpoint = "https://api-inference.huggingface.co/models/google/flan-t5-small"; // Update your model here
        private const string HuggingFaceApiKey = "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"; // Set this in environment variables ideally

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
                    inputs = $"{prompt}\nLogs:\n{logs}"
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var request = new HttpRequestMessage(HttpMethod.Post, HuggingFaceEndpoint);
                request.Headers.Add("Authorization", $"Bearer {HuggingFaceApiKey}");
                request.Content = jsonContent;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to analyze logs via HuggingFace. StatusCode: {response.StatusCode}, Response: {errorBody}");
                    return $"[AI Analysis Failed: {response.StatusCode}]";
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                // Fix: Parse as JArray
                var responseArray = JArray.Parse(responseBody);
                var resultContent = responseArray[0]?["generated_text"]?.ToString();

                return resultContent ?? "[No content returned]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during AI log analysis with HuggingFace.");
                return "[AI Analysis Error]";
            }
        }
    }
}