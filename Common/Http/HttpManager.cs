using Newtonsoft.Json;
using System.Text;

namespace Common.Http
{
    public class HttpManager
    {
        private static HttpManager Instance;
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<T> ExcuteHttp<T, K>(string url, K requestData, Dictionary<string, string> headers = null) where T : class
                                             where K : class
        {
            var json = JsonConvert.SerializeObject(requestData);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (headers != null && headers.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                var result = await _httpClient.SendAsync(request);
                if (result.IsSuccessStatusCode == true)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<T>(response);
                    return data;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[HttpManager] ExcuteHttp failed for {Url}", url);
                return null;
            }
        }

        public static HttpManager GetInstance()
        {
            if(Instance == null)
            {
                Instance = new HttpManager();
            }
            return Instance;
        }
    }
}
