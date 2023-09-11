using MikoGPT.apis;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT.ImageApis
{
    public class DeepAIImageApi
    {
        static DeepAIImageApi? instance;
        static public DeepAIImageApi Instance { get => instance ?? throw new ArgumentNullException(nameof(instance)); set => instance = value; }

        ProxyManager proxyManager;
        HttpClient httpClient = new();

        public DeepAIImageApi()
        {
            proxyManager = new(Config.DefaultProxyFetchers,
                (p) =>
                {
                    try
                    {
                        generateImageUri("lol", p.ToClient(20));
                        return true;
                    } catch (Exception e)
                    {
                        return false;
                    }
                });
        }

        public byte[] GenerateImage(string text)
        {
            for (var i = 0; i < 20; i++) {
                var proxy = proxyManager.GetNextProxy();

                try
                {
                    var result = generateImageUri(text, proxy.ToClient(20));
                    proxy.Close(true);

                    var image = httpClient.GetAsync(result).Result.Content.ReadAsByteArrayAsync().Result;

                    return image;
                }
                catch (Exception e)
                {
                    Logger.Instance?.Log("deepaiimage runtime", $"Bad generation {e}", Logger.WarningLevel.Warning);
                    proxy.Close(false);
                }
            }
            return new byte[0];
        }

        public Uri generateImageUri(string text, HttpClient client)
        {
            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepai.org/api/text2img");
            request.Headers.Add("api-key", DeepAiApi.GetApiKey(userAgent));
            request.Headers.UserAgent.ParseAdd(userAgent);

            request.Content = new MultipartFormDataContent()
            {
                { new StringContent(text), "text"}
            };

            string reqRes = client.Send(request).Content.ReadAsStringAsync().Result;
            JObject reqResObject = JObject.Parse(reqRes);
            return new Uri((string?)reqResObject["output_url"] ?? throw new());
        }
    }
}
