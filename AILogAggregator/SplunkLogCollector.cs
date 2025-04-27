using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogAggregatorService.Services
{
    public class SplunkLogCollector
    {
        private readonly HttpClient _httpClient;
        private readonly string _splunkHECEndpoint;
        private readonly string _splunkToken;

        public SplunkLogCollector()
        {
            _httpClient = new HttpClient();
            _splunkHECEndpoint = Environment.GetEnvironmentVariable("SPLUNK_HEC_ENDPOINT") 
                                 ?? "https://prd-p-phf6t.splunkcloud.com:8088/services/collector/event";
            _splunkToken = Environment.GetEnvironmentVariable("SPLUNK_HEC_TOKEN") 
                           ?? "e45fb617-7cec-4362-9ad2-df707e1a3761";
        }

        public async Task<IEnumerable<string>> CollectLogs()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _splunkHECEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Splunk", _splunkToken);

         
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var logs = JArray.Parse(content);

            return logs.Select(x => x.ToString());
        }
    }
}