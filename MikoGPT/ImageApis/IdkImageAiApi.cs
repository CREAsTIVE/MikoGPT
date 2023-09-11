using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model.Attachments;
using static MikoGPT.ProxyManager;

namespace MikoGPT.ImageApis
{
    class IdkImageAiApi
    {
        static IdkImageAiApi? instance;
        public static IdkImageAiApi Instance { get => instance ?? throw new ArgumentNullException(); set => instance = value; }
        ProxyManager proxyManager;
        public IdkImageAiApi()
        {
            proxyManager = new ProxyManager(
                Config.DefaultProxyFetchers,
                (proxy) =>
                {
                    try
                    {
                        proxy.ToClient(20).GetAsync("https://httpbin.org/anything").Wait();
                        return true;
                    } catch (Exception) { return false; }
                }
                );
        }
        public byte[][] GenerateImageByProxy(string prompt, HttpClient httpClient)
        {
            HttpRequestMessage httpRequestMessage2 = new HttpRequestMessage(HttpMethod.Post, "https://api.craiyon.com/v3");
            JObject requestObject = new JObject()
            {
                {"prompt", prompt },
                {"version", "c4ue22fb7kb6wlac" },
                {"token", null },
                {"model", "art" },
                {"negative_prompt", "" }
            };
            httpRequestMessage2.Content = new StringContent(requestObject.ToString(), Encoding.UTF8, "application/json");
            httpRequestMessage2.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");

            var reqResult = httpClient.Send(httpRequestMessage2).Content.ReadAsStringAsync().Result;
            var result = JObject.Parse(reqResult)["images"]?.Select((t) => (string?)t)?.ToArray() ?? throw new();
            byte[][] photos = new byte[9][];
            for (var i = 0; i < 9; i++)
            {
                photos[i] = httpClient.GetAsync($"https://img.craiyon.com/{result[i]}").Result.Content.ReadAsByteArrayAsync().Result;
            }
            return photos;
        }
        public byte[][] GenerateImage(string prompt)
        {
            for (var i = 0; i < 20; i++)
            {
                var proxy = proxyManager.GetNextProxy();
                
                try
                {
                    var result = GenerateImageByProxy(prompt, proxy.ToClient(120));
                    proxy.Close(true);
                    return result;
                } catch (Exception e)
                {
                    Logger.Instance?.Log("idkimage runtime", $"Bad generation {e}", Logger.WarningLevel.Warning);
                    proxy.Close(false);
                }
            }
            return new byte[0][];
        }
    }
}
