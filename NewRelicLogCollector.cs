using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogAggregatorService.Services
{
    public class NewRelicLogCollector
    {
        private readonly HttpClient _httpClient;
        private readonly string _accountId;
        private readonly string _userApiKey;

        public NewRelicLogCollector(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _accountId = "6698438"; // Replace with your actual account ID
            _userApiKey = "NRAK-BIGU8NQ6SVK6658W6O50A04M906";
        }

        public async Task<List<string>> CollectLogsAsync()
        {
            var graphqlQuery = new
            {
                query = $@"
                {{
                    actor {{
                        account(id: {_accountId}) {{
                            nrql(query: ""SELECT * FROM Log SINCE 30 minutes ago"") {{
                                results
                            }}
                        }}
                    }}
                }}"
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(graphqlQuery);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.newrelic.com/graphql");
            request.Headers.Add("API-Key", _userApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to fetch logs: {response.StatusCode} | {errorContent}");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var parsed = JObject.Parse(jsonString);

            var structuredLogs = new List<string>();

            foreach (var result in parsed["results"] ?? Enumerable.Empty<JToken>())
            {
                foreach (var eventItem in result["events"] ?? Enumerable.Empty<JToken>())
                {
                    var log = new StructuredLog
                    {
                        Message = eventItem["message"]?.ToString(),
                        Level = eventItem["level"]?.ToString(),
                        Timestamp = eventItem["timestamp"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        AppName = eventItem["attributes"]?["appName"]?.ToString(),
                        Controller = eventItem["attributes"]?["controller"]?.ToString(),
                        Method = eventItem["attributes"]?["method"]?.ToString(),
                        HttpVerb = eventItem["attributes"]?["httpVerb"]?.ToString(),
                        Payload = eventItem["attributes"]?["payload"]?.ToString(),
                        Environment = eventItem["attributes"]?["env"]?.ToString()
                    };

                    structuredLogs.Add(JsonConvert.SerializeObject(log));
                }
            }

            return structuredLogs;
        }
    }
      public class StructuredLog
    {
        public string Message { get; set; }
        public string Level { get; set; }
        public long Timestamp { get; set; }
        public string AppName { get; set; }
        public string Controller { get; set; }
        public string Method { get; set; }
        public string HttpVerb { get; set; }
        public string Payload { get; set; }
        public string Environment { get; set; }
    }
}