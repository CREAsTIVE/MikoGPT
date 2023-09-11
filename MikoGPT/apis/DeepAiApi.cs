using ChatGPTVK;
using Fare;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VkNet.Utils;
using static MikoGPT.ProxyManager;

namespace MikoGPT.apis
{
    internal class DeepAiApi : IChatCompletion
    {
        ProxyManager proxyManager;
        public DeepAiApi(ProxyManager proxyManager)
        {
            this.proxyManager = proxyManager;
        }

        public static Xeger RandomQuestionGeneratorRegex = new Xeger(@"\d{1,2}[\+\-\/]\d{1,2}=?");
        public string DirectChatCompletion(IEnumerable<(string, string)> messages)
        {
            for (var i = 0; i < 20; i++)
            {
                var proxy = proxyManager.GetNextProxy();
                try
                {
                    var cancelToken = new CancellationTokenSource();
                    Task.Run(async () =>
                    {
                        try {
                            await CreateCompletionAsync(new(string, string)[] { ("user", "say test")}, proxy.ToClient(10), cancelToken);
                        } 
                        catch (AggregateException) { cancelToken.Cancel(); }
                        catch (TaskCanceledException) { }
                    });
                    var result = CreateCompletionAsync(messages, proxy.ToClient(3*60), cancelToken).Result;
                    cancelToken.Cancel();
                    proxy.Close(true);
                    return result;
                }
                catch (AggregateException) { proxy.Close(false); }
                catch (TaskCanceledException) { proxy.Close(false); }
            }
            return "to many request was bad"; // TODO: fix me on exeption
        }
        public static string DoCompletionByProxy(IEnumerable<(string, string)> messages, HttpClient client)
        {
            return CreateCompletionAsync(messages, client).Result;
        }
        public static string Md5(string text)
        {
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
            StringBuilder sBuilder = new StringBuilder();
            string hashedText = string.Join("", data.Select(b => b.ToString("x2")));
            string reversed = new string(hashedText.Reverse().ToArray());
            return reversed;
        }

        public static string GetApiKey(string user_agent)
        {
            string part1 = new Random().Next(0, 1000000000).ToString();
            string part2 = Md5(user_agent + Md5(user_agent + Md5(user_agent + part1 + "x"))).ToString();

            return $"tryit-{part1}-{part2}";
        }
        public class RoleMessageContainer
        {
            [JsonProperty("role")]
            public string Role { get; set; }
            [JsonProperty("content")]
            public string Message { get; set; }
            public RoleMessageContainer(string role, string message)
            {
                Role = role; Message = message;
            }
        }
        public async static Task<string> CreateCompletionAsync(
            IEnumerable<(string role, string message)> messages,
            HttpClient httpClient,
            CancellationTokenSource? requestCancelToken = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "https://api.deepai.org/make_me_a_pizza");
            string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
            var generatedApiKey = GetApiKey(userAgent);
            message.Headers.Add("api-key", generatedApiKey);
            message.Headers.UserAgent.ParseAdd(userAgent);

            var content = JsonConvert.SerializeObject(messages.Select(msg => new RoleMessageContainer(msg.role, msg.message))).ToString();

            message.Content = new MultipartFormDataContent()
            {
                {new StringContent("chat"), "chat_style"},
                {new StringContent(content), "chatHistory"}
            };

            HttpResponseMessage response;
            if (requestCancelToken is null)
                response = httpClient.Send(message);
            else
                response = httpClient.Send(message, requestCancelToken.Token);

            response.EnsureSuccessStatusCode();
            using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                string result = await streamReader.ReadToEndAsync();
                response.Dispose();
                return result;
            }
        }
    }
}
