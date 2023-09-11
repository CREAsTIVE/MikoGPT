using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MikoGPT.apis
{
    public class ChatGPTDemoApi
    {
        public class Chat
        {
            [JsonProperty("chat_name")]
            string? chatName { get; set; } = "Name";
            [JsonProperty("_id")]
            string? id { get; set; } = null;
        }
        HttpClient httpClient = new();
        public Chat[] UserChatsReq
        {
            get => JsonConvert.DeserializeObject<Chat[]>(
                httpClient.PostAsync("https://chat.chatgptdemo.net/get_user_chat", new StringContent($$"""{user_id: {{userId}}}""")).Result.Content.ReadAsStringAsync().Result
                ) ?? new Chat[0];
        }

        Chat createChat() => JsonConvert.DeserializeObject<Chat>(
            httpClient.PostAsync("https://chat.chatgptdemo.net/new_chat", new StringContent($$"""{user_id: {{userId}}}""")).Result.Content.ReadAsStringAsync().Result) ?? throw new();

        public string userId = string.Empty;
        public string token = string.Empty;

        static readonly Regex userIdRegex = new("""<div id="USERID" style="display: none">([a-z0-9]+)</div>\s*<div id="TTT" style="display: none">([a-z0-9%A-Z_~]+)</div>""");
        void createUser()
        {
            string httpResult = httpClient.GetAsync("https://chat.chatgptdemo.net/").Result.Content.ReadAsStringAsync().Result;
            var regexResult = userIdRegex.Match(httpResult);
            userId = regexResult.Groups[1].Value;
            token = HttpUtility.UrlDecode(regexResult.Groups[2].Value, Encoding.UTF8);
        }

        public ChatGPTDemoApi() => createUser();
        public ChatGPTDemoApi(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            createUser();
        }
        public ChatGPTDemoApi(HttpClient httpClient, string userId)
        {
            this.httpClient = httpClient;
            this.userId = userId;
        }
    }
    class ProxableChatGPTDemoApi
    {

    }
}
