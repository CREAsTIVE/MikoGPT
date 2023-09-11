using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model.Attachments;

namespace ChatGPTVK
{
    public static class OpenAIApi
    {
        static HttpClient httpClient = new HttpClient();
        public static void Init()
        {
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        static JArray repairMessagesFormat(IEnumerable<(string role, string content)> messages)
        {
            JArray arr = new JArray();
            foreach (var message in messages)
            {
                arr.Add(new JObject() { { "role", message.role}, { "content", message.content } });
            }
            return arr;
        }

        public static HttpRequestMessage BuidChatRequest(string key, IEnumerable<(string role, string content)> messages, 
            string model = "gpt-3.5-turbo-0301", float temperature = 0.7f, int maxTokens = 800, bool stream = false) // FIX THIS
        {
            var req = new JObject
            {
                { "model", model },
                { "messages", repairMessagesFormat(messages) },
                { "temperature", float.Parse(Global.Instance.Temperature, NumberStyles.Any, CultureInfo.InvariantCulture) },
                { "max_tokens", int.Parse(Global.Instance.MaxTokens) },
                { "top_p", float.Parse(Global.Instance.TopP, NumberStyles.Any, CultureInfo.InvariantCulture) },
                { "frequency_penalty", float.Parse(Global.Instance.FrequancyPenalty, NumberStyles.Any, CultureInfo.InvariantCulture) },
                { "presence_penalty", float.Parse(Global.Instance.PresencePenalty, NumberStyles.Any, CultureInfo.InvariantCulture) },
                { "stream", stream },
            };
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://api.openai.com/v1/chat/completions"));
            request.Headers.Add("Authorization", $"Bearer {key}");
            request.Content = new StringContent(req.ToString(),
                                                Encoding.UTF8,
                                                "application/json");//CONTENT-TYPE header
            return request;
        }
        public static CompletionResult ChatCompletion(string key, IEnumerable<(string role, string content)> messages, 
            string model = "gpt-3.5-turbo-0301", float temperature = 0.7f, int maxTokens = 800)
        {
            var req = BuidChatRequest(key, messages, model, temperature, maxTokens);
            var result = JObject.Parse(httpClient.SendAsync(req).Result.Content.ReadAsStringAsync().Result);
            if (result.ContainsKey("error"))
                throw new OpenAIException((string)result["error"]["type"], (string)result["error"]["message"]);
            return new CompletionResult()
            {
                result = (string)result["choices"][0]["message"]["content"],
                totalTokensSpent = (int)result["usage"]["total_tokens"],
            };
        }
    }
    public class OpenAIException : Exception
    {
        public string message;
        public string name;
        public OpenAIException(string name, string message) : base($"Error {name}:\n{message}")
        {
            this.name = name;
            this.message = message;
        }
    }
    public class CompletionResult
    {
        public string result = "";
        public int totalTokensSpent;
    }
}

