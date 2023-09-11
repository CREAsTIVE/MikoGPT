using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;




HttpClient httpClient = new HttpClient();
HttpRequestMessage buildRequest(string key, string promt, string model = "text-davinci-003", float temperature = 0.7f, int maxTokens = 800, bool stream = false)
{
    var req = new JObject
            {
                { "model", model },
                { "prompt", promt },
                { "temperature", temperature },
                { "max_tokens", maxTokens },
                { "top_p", 1 },
                { "frequency_penalty", 0 },
                { "presence_penalty", 0 },
                { "stream", stream },
            };
    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://api.openai.com/v1/completions"));
    request.Headers.Add("Authorization", $"Bearer {key}");
    request.Content = new StringContent(req.ToString(),
                                        Encoding.UTF8,
                                        "application/json");//CONTENT-TYPE header
    return request;
}
Stream CompletionStream(string key, string promt, string model = "text-davinci-003", float temperature = 0.7f, int maxTokens = 800)
{
    var request = buildRequest(key, promt, model, temperature, maxTokens, stream: true);
    return httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.ReadAsStream();
}

void Read(Stream stream)
{
    string message = "";
    while (true)
    {
        int nextByte = stream.ReadByte();
        if (nextByte < 0)
            continue;

        char next = (char)nextByte;

        if (next == '\n')
        {
            if (message.Length > 0)
            {
                var match = Regex.Match(message, @"data: ([\s\S]*)");
                if (match.Groups[1].Value == "[DONE]")
                    break;
                else
                {
                    onMessage(match.Groups[1].Value); message = "";
                }
            }
        }
        else
            message += next;
    }
}
void onMessage(string message)
{
    Console.Write(JObject.Parse(message)["choices"][0]["text"]);
}
while (true)
{
    Console.Write("Введите запрос: ");
    var request = $@"A Standard Template Construct (STC) system was an advanced, artificially intelligent computer database created during the Age of Technology said to have contained the sum total of Human scientific and technological knowledge.
    STC systems possessed the ability not just to store information but also to produce new designs to meet changing circumstances.

    User: {Console.ReadLine()}
    STC: ";
    Read(CompletionStream("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", request, maxTokens: 2000));
    Console.WriteLine("\n");
}