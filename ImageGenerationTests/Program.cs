using System.Security.Cryptography;
using System.Text;

HttpClient httpClient = new();
string Md5(string text)
{
    MD5 md5Hash = MD5.Create();
    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
    StringBuilder sBuilder = new StringBuilder();
    string hashedText = string.Join("", data.Select(b => b.ToString("x2")));
    string reversed = new string(hashedText.Reverse().ToArray());
    return reversed;
}
string GetApiKey(string user_agent)
{
    string part1 = new Random().Next(0, 1000000000).ToString();
    string part2 = Md5(user_agent + Md5(user_agent + Md5(user_agent + part1 + "x"))).ToString();

    return $"tryit-{part1}-{part2}";
}

HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepai.org/api/text2img");
var content = new MultipartFormDataContent();
content.Add(new StringContent("Apple"), "text");
request.Content = content;
string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
request.Headers.Add("api-key", GetApiKey(userAgent));
request.Headers.UserAgent.ParseAdd(userAgent);
//request.Headers.Add("Origin", "https://deepai.org");

Console.WriteLine(httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result);