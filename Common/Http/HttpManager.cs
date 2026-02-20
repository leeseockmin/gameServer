using Newtonsoft.Json;
using System.Text;

namespace Common.Http
{
    /// <summary>
    /// HTTP 요청을 처리하는 매니저 클래스.
    /// DI 컨테이너에 Singleton으로 등록하여 사용합니다.
    /// </summary>
    public class HttpManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<T> ExecuteHttp<T, K>(string url, K requestData, Dictionary<string, string> headers = null) where T : class
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
                Serilog.Log.Error(ex, "[HttpManager] ExecuteHttp failed for {Url}", url);
                return null;
            }
        }
    }
}
