using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WamisDataCollector.Services
{
    public class WamisApiClient
    {
        private readonly HttpClient _httpClient;
        //private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly Action<string> _logAction;

        public WamisApiClient(string apiKey, string baseUrl, Action<string> logAction = null)
        {
            _httpClient = new HttpClient();
           // _apiKey = apiKey;
            _baseUrl = baseUrl;
            _logAction = logAction ?? Console.WriteLine;
        }

        public async Task<T> GetDataAsync<T>(string endpoint, Dictionary<string, string> parameters)
        {
          //  parameters["serviceKey"] = _apiKey;
            parameters["output"] = "json";

            var queryString = await new FormUrlEncodedContent(parameters).ReadAsStringAsync();
            var requestUrl = $"{_baseUrl}/{endpoint}?{queryString}";
            /////////////////////////////////////////////////////////////////////////////////    _logAction($"[API 요청] {requestUrl}");
            {
                int kkk = 0;
            }
            try
            {
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "[]" || jsonString.Contains("\"list\":[]"))
                {
                    return default(T);
                }
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch (Exception e)
            {
                _logAction($"[API 오류] {e.Message}");
                return default(T);
            }
        }
    }
}