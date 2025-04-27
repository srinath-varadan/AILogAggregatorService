using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using OpenAI;
using OpenAI.Chat;
using LogAggregatorService.LogMetrics;

namespace LogAggregatorService.Services
{
    public class AIAnalyzerService
    {
        private readonly OpenAIClient _client;

        public AIAnalyzerService()
        {
            _client = new OpenAIClient(new OpenAIAuthentication(Environment.GetEnvironmentVariable("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")));
        }

        public async Task AnalyzeLogs(IEnumerable<string> logs)
        {
            var input = string.Join("\n", logs.Take(100)); // Limit input to prevent token overflow

            var chatRequest = new ChatRequest(
                messages: new List<Message>
                {
                            new Message(Role.System, 
@"You are a log analysis engine.

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
}
Analyze carefully. Output strict JSON only."),
                    new Message(Role.User, input)
                },
                model: "gpt-4"
            );

            var response = await _client.ChatEndpoint.GetCompletionAsync(chatRequest);

            var output = response.FirstChoice.Message.Content;
            LogAggregatorService.LogMetrics.LogMetrics.ProcessAIInsights(output); // Assuming AILogAnalysis is a Counter

        }
    }
}