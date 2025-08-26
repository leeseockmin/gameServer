using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Http
{
    public class HttpManager
    {
        private static HttpManager Instance;
        public async Task<T> ExcuteHttp<T, K>(string url, K requestData, Dictionary<string, string> headers = null) where T : class
                                             where K : class
        {
            var json = JsonConvert.SerializeObject(requestData);
            using (HttpClient client = new HttpClient())
            {
                try
                {

                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    if(headers != null && headers.Count > 0)
                    {
                        foreach(var header in headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }

                    var result = await client.PostAsync(url, content);
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
                    Debug.WriteLine(ex);
                    return null;
                }
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
