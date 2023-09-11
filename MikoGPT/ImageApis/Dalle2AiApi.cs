using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static MikoGPT.ProxyManager;

namespace MikoGPT.ImageApis
{
    class Dalle2AiApi
    {
        static Dalle2AiApi? instance;
        public static Dalle2AiApi Instance { get => instance ?? throw new ArgumentNullException(); set => instance = value; }
        ProxyManager proxyManager;
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        public Dalle2AiApi()
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
        public class GenerationException : Exception
        {
            public string Reason = "";
        }
        public class ProxyBlockedException : Exception { }
        public byte[] GenerateImageByProxy(string prompt, HttpClient httpClient)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.veed.io/api/v1/ai-images");
            var obj = new JObject()
                    {
                        {"prompt",  prompt},
                        {"resolution", "Res256"}
                    };
            httpRequestMessage.Content = new StringContent(obj.ToString(), Encoding.UTF8, "application/json");
            httpRequestMessage.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            httpRequestMessage.Headers.Referrer = new Uri("https://www.veed.io/tools/ai-image-generator");

            var result = JObject.Parse(httpClient.Send(httpRequestMessage).Content.ReadAsStringAsync().Result);
            if (result.ContainsKey("errors"))
            {
                var reason = ((string?)result["errors"]?[0]?["message"]) ?? "";
                if (reason == "Rate limit exceeded")
                    throw new ProxyBlockedException();
                throw new GenerationException() { Reason =  reason};
            }

            string id = ((string?)result["data"]?["assetId"]) ?? throw new Exception();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, @$"https://cdn-user.veed.io/ai-generated/images/{id}.png?width=640&quality=75");
            requestMessage.Headers.Referrer = new Uri(@"https://www.veed.io/tools/ai-image-generator");
            requestMessage.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");

            var arr = httpClient.Send(requestMessage).Content.ReadAsByteArrayAsync().Result;
            return arr;
        }
        public byte[] GenerateImage(string prompt)
        {
            for (var i = 0; i < 20; i++)
            {
                var proxy = proxyManager.GetNextProxy();
                try
                {
                    var result = GenerateImageByProxy(prompt, proxy.ToClient(20));
                    proxy.Close(true);
                    return result;
                } catch (HttpRequestException e)
                {
                    Logger.Instance?.Log("dalle2 gen", $"{e}", Logger.WarningLevel.Warning);
                    proxy.Close(false);
                } catch (TaskCanceledException)
                {
                    Logger.Instance?.Log("dalle2 gen", $"timeout...", Logger.WarningLevel.Warning);
                    proxy.Close(false);
                }
                catch (ProxyBlockedException)
                {
                    proxy.Close(false); proxy.Close(false); proxy.Close(false); proxy.Close(false); proxy.Close(false); proxy.Close(false);
                }
            }
            throw new GenerationException() { Reason="to many attempts" };
        }
    }
}
