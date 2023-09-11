using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTVK
{
    internal class PastebinApi
    {
        static HttpClient httpClient = new HttpClient();
        public static string Poste(string data, string apiKey = "qRHnLBfnBc4aM63rDbALWYiyYeL03H6z")
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://pastebin.com/api/api_post.php");
            request.Headers.Add("api_option", "paste");
            request.Headers.Add("api_dev_key", apiKey);
            request.Headers.Add("api_paste_code", data);
            return httpClient.Send(request).Content.ReadAsStringAsync().Result;
        }
    }
}
