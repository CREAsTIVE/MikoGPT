

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikoGPT.apis
{
    public interface IChatCompletion
    {
        public string DirectChatCompletion(IEnumerable<(string, string)> messages);
    }
    public class OpenAIApi : IChatCompletion
    {
        static HttpClient httpClient = new HttpClient();
        public static void Init()
        {
            //FIXME: to 5 minutes + catch exception
            httpClient.Timeout = Timeout.InfiniteTimeSpan; // Ответ может идти довольно долго
        }

        public static ChatCompletionResult ChatCompletion(
            IEnumerable<(string author, string message)> messages,
            string model = "gpt-3.5-turbo-0301",
            int maxTokens = 1000,
            // Additional parameters
            float? temperature = null,
            float? topP = null,
            float? presencePenalty = null,
            float? frequencyPenalty = null
            )
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri("https://api.openai.com/v1/chat/completions"),
                Method = HttpMethod.Post,
                Headers =
                {
                    //{ "Content-Type", "application/json" },
                    { "Authorization", $"Bearer {Config.Instance.OpenAIAPIKeys[0]}" }
                },
                Content = new StringContent(JsonSerializer.Serialize<ChatCompletionParams>(new()
                {
                    Model = model,
                    Messages = messages.ForEachCopy((v) => new ChatCompletionParams.MessageData() { Role = v.author, Content = v.message }),
                    Temperature = temperature,
                    FrequencyPenalty = frequencyPenalty,
                    MaxTokens = maxTokens,
                    PresencePenalty = presencePenalty,
                    TopP = topP
                }, options: new()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return new(httpClient.Send(request).Content.ReadAsStringAsync().Result);
        }

        public string DirectChatCompletion(IEnumerable<(string, string)> messages)
        {
            return ChatCompletion(messages).Result;
        }

        public class ChatCompletionParams
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = "";

            public class MessageData
            {
                [JsonPropertyName("role")]
                public string Role { get; set; } = "";
                [JsonPropertyName("content")]
                public string Content { get; set; } = "";
            }
            [JsonPropertyName("messages")]
            public IEnumerable<MessageData> Messages { get; set; } = new MessageData[0];

            [JsonPropertyName("temperature")]
            public float? Temperature { get; set; }
            [JsonPropertyName("top_p")]
            public float? TopP { get; set; }
            [JsonPropertyName("n")]
            public int? N { get; set; }
            [JsonPropertyName("stream")]
            public bool? Stream { get; set; }
            [JsonPropertyName("stop")]
            public IEnumerable<string>? Stop { get; set; }
            [JsonPropertyName("max_tokens")]
            public int? MaxTokens { get; set; }
            [JsonPropertyName("presence_penalty")]
            public float? PresencePenalty { get; set; }
            [JsonPropertyName("frequency_penalty")]
            public float? FrequencyPenalty { get; set; }

        }
        public class ChatCompletionResult
        {
            public uint PromptTokens;
            public uint CompletionTokens;
            public uint TotalTokens;

            public string Result;
            public string FinishReason;
            public ChatCompletionResult(string rawOutput)
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(rawOutput);

                Result = (string?)json?["choices"]?[0]?["message"]?["content"] ?? "";
                if (Result.Length <= 0)
                    throw new Exception((string?)json?["error"]?["type"]);
                FinishReason = (string?)json?["choices"]?[0]?["finish_reason"] ?? "";

                PromptTokens = (uint?)json?["usage"]?["prompt_tokens"] ?? 0;
                CompletionTokens = (uint?)json?["usage"]?["completion_tokens"] ?? 0;
                TotalTokens = (uint?)json?["usage"]?["total_tokens"] ?? 0;
            }
        }
    }
}
