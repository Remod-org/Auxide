using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Auxide
{
    public class WebClient
    {
        public HttpClient _httpClient;
        public string _username;
        public string _password;

        public WebClient()
        {
            _httpClient = new HttpClient();
            _username = Auxide.config.Options.subscription.username;
            _password = Auxide.config.Options.subscription.password;
        }

        public WebClient(string baseUrl, string username, string password)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _username = username;
            _password = password;
        }

        public async Task<T> PostAsync<T>(string url, object payload)
        {
            // Serialize the payload object to JSON
            string json = JsonConvert.SerializeObject(payload);
            // Set the request content type to JSON
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the Basic authentication header
            byte[] byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            // POST the request and get the response
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            // Deserialize the response to a JSON object
            string responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseJson);
        }

        public async Task<T> GetAsync<T>(string url)
        {
            // Add the Basic authentication header
            byte[] byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            // GET the response
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Deserialize the response to a JSON object
            string responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseJson);
        }
    }
}
