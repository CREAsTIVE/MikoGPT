using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Web;

namespace Buplic
{
    public class AdditionalApi
    {
        public static HttpClient httpClient = new HttpClient();
        string token;

        public AdditionalApi(string token)
        {
            this.token = token;
        }
        string getRequestLink(string methodName, string args)
        {
            return $"https://api.vk.com/method/{methodName}?{args}&access_token={token}&v=5.131";
        }
        public async Task<JToken> call(string methodName, Dictionary<string, object> parameters)
        {
            List<string> val = new List<string>();
            foreach (string key in parameters.Keys)
            {
                if (parameters[key] != null)
                {
                    val.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(parameters[key].ToString())}");
                }
            }
            return JToken.Parse( Uri.UnescapeDataString(
                    httpClient.GetAsync(getRequestLink(methodName, string.Join('&', val))).Result.Content.ReadAsStringAsync().Result))["response"];
        }

        public JObject GetLongPollServer(int groupId)
        {
            return (JObject)call("groups.getLongPollServer", new Dictionary<string, object>() { { "group_id", groupId } }).Result;
        }
        public string server="";
        public string key="";
        public string ts="";
        public JToken Listen(int groupId)
        {
            if (server == "")
            {
                var data = GetLongPollServer(groupId);
                server = (string)data["server"];
                key = (string)data["key"];
                ts = (string)data["ts"];
            }
            var res = JObject.Parse(httpClient.GetAsync($"{server}?act=a_check&key={key}&ts={ts}&wait=25").Result.Content.ReadAsStringAsync().Result);

            ts = (string)res["ts"];
            return res["updates"];
        }
    }
}
