
using Buplic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using VkNet;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;


HttpClient httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer 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");
httpClient.BaseAddress = new Uri("https://api.openai.com/v1/completions");
httpClient.DefaultRequestHeaders
      .Accept
      .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header


VkApi api = new VkApi();
api.Authorize(new ApiAuthParams() { 
    AccessToken = "vk1.a.gWk27GYJDvB4ZQ96-Rgbi2OOiwYp5yhi4fJbuoZA_iXYyjHqy6mmnqoAKVHRTTVOOv7UlQZZbn_LxgoM21_1LHPjrQuemTfsXUdjp0v7nx7sMGMgJ9WwrF3zKR3fICzpc_XBltK3UE8qvnEEAewcEx4B4WZFb4XQBWIa_5N9agDzVgopbylXFHchxqOMGi80HMTmxmlFC_ItHEEBYEu51w" 
});

AdditionalApi addApi = new AdditionalApi(api.Token);



Dictionary<long, List<string>> chatsMemory;
Dictionary<long, string> names = new Dictionary<long, string>() { { 659963950, "Александр Лисандр"} };

Console.WriteLine(File.Exists("data.json"));
if (File.Exists("data.json"))
{
    ChatSetting.Chats = System.Text.Json.JsonSerializer.Deserialize<Dictionary<long, ChatSetting>>(File.ReadAllText("data.json"));
}
else
{
    ChatSetting.Chats = new Dictionary<long, ChatSetting>();
}


Command[] commands = new Command[]
{
    new Command(new() { @"/enable", "/вкл" }, 
        (arg, msg) => {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            cs.BotEnabled = !cs.BotEnabled;
            string t = cs.BotEnabled ? "Включён":"Выключен";
            api.SendMessage($"бот теперь {t}", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
            }
    ),
    new Command(new() { @"/chance (\d{1,3})"}, (arg, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            cs.Chance = int.Parse(arg[1]);
            api.SendMessage($"Шанс отправить сообщение теперь {cs.Chance}%", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
        }
    ),
    new Command(new() { @"/chance"}, (arg, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            api.SendMessage($"Шанс отправить сообщение {cs.Chance}%", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
        }
    ),
    new Command(new() { @"/name ([\d\D]*)"}, (arg, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            cs.Name = arg[1];
            api.SendMessage($"Имя бота теперь {cs.Name}", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
        }
    ),
    new Command(new() { @"/prequel ([\d\D]*)"}, (arg, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            cs.Prequel = arg[1];
            api.SendMessage($"Приквел установлен", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
        }
    ),
    new Command(new(){ @"/who", "/кто"}, (arg, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            api.SendMessage($"Приквел: {cs.Prequel}\n\nИмя: {cs.Name}", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
        }
    ),
    new Command(new() { @"/forgot all"}, (args, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            api.SendMessage("Ок, я всё забыл", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
            cs.Memory.Clear();
        }
    ),
    new Command(new() { @"/memory size ([12]?\d)"}, (args, msg) =>
        {
            ChatSetting cs = ChatSetting.Get((long)msg.Message.PeerId);
            api.SendMessage($"Максимальный размер памяти установлен на {args[1]}", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
            cs.MemorySize = int.Parse(args[1]);
        }
    ),
    new Command(new() { "/help"}, (args, msg) =>
        {
            api.SendMessage(@"Список команд:
/вкл - вкл/выкл бота
/chance - посмотреть шанс отправки сообщения ботом
/chance number - изменить шанс отправки сообщения
/name text... - Изменить имя бота
/prequel text... - Изменить Приквел бота
/who - посмотреть информацию об личности бота
/forgot all - забыть всё", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
        }
    )
};
int counter = 0;



var s = api.Groups.GetLongPollServer(218182847);
resetG:
while (true)
{
    var poll = api.Groups.GetBotsLongPollHistory(new BotsLongPollHistoryParams()
    {
        Server = s.Server,
        Key = s.Key,
        Ts = s.Ts,
        Wait = 5
    });
    counter++;
    if (counter > 20)
    {
        ChatSetting.SaveData();
        counter = 0;
    }
    s.Ts = poll.Ts;
    foreach (var ev in poll.Updates)
    {
        if (ev.Instance is MessageNew)
        {
            var msg = (MessageNew)ev.Instance;
            if (msg.Message.FromId < 0) { }
            else if (msg.Message.Text.StartsWith('/'))
            {
                var cmd = msg.Message.Text;
                bool sucsess = false;
                foreach (var command in commands)
                {
                    if (command.ExecuteCommand(cmd, msg))
                    { sucsess = true; break; }
                }
                ChatSetting.SaveData();
                if (!sucsess)
                {
                    api.SendMessage("Неизвестная команда или неправильный формат команды", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
                }
            }
            else
            {
                if (ChatSetting.Get((long)msg.Message.PeerId).BotEnabled)
                {
                    if (!names.ContainsKey(msg.Message.FromId.Value))
                    {
                        var user = api.Users.Get(new long[] { (long)msg.Message.FromId })[0];
                        names.Add((long)msg.Message.FromId, $"{user.FirstName} {user.LastName}");
                    }
                    ChatSetting chatSetting = ChatSetting.Get((long)msg.Message.PeerId);
                    chatSetting.Memory.Add($"{names[(long)msg.Message.FromId]}: {msg.Message.Text.Trim('_')}\n");
                    if (chatSetting.Memory.Count > chatSetting.MemorySize)
                        chatSetting.Memory = chatSetting.Memory.TakeLast(chatSetting.MemorySize).ToList();
                        
                    if (Extensions.random.Next(100) < chatSetting.Chance || msg.Message.Text.StartsWith("_"))
                    {
                        new Task(() =>
                        {
                            try
                            {
                                bool isAnswer = msg.Message.Text.StartsWith("_");
                                string t = isAnswer ? " отвечает" : "";
                                var result = requestOpenAI(
                                    $"{chatSetting.Prequel}\n\n{ChatSetting.getStringMemory(chatSetting.Memory)}\n{chatSetting.Name}{t}: "
                                    );
                                chatSetting.Memory.Add($"{chatSetting.Name}{t}: {result}");
                                api.SendMessage(result, (long)msg.Message.PeerId, isAnswer ? msg.Message.ConversationMessageId : null);
                            }
                            catch (OpenAIException e)
                            {
                                Console.WriteLine(e.Message);
                                if (e.name == "server_error")
                                {
                                    api.SendMessage("Опс, ошибка сервера... Попробуйте ещё раз", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
                                }
                                else
                                {
                                    chatSetting.Memory.Clear();
                                    api.SendMessage("Память переполнена... Очищаю... Попробуйте ещё раз", (long)msg.Message.PeerId, msg.Message.ConversationMessageId);
                                }
                            }
                        }).Start();
                    }
                }
            }
        }
    }
}

goto resetG;

string requestOpenAI(string promt)
{
    /*model: "text-davinci-003",
    prompt: prompt,
    temperature: 0.7,
    max_tokens: 1000,
    top_p: 1,
    frequency_penalty: 0,
    presence_penalty: 0,*/

    var req = new JObject
            {
                { "model", "text-davinci-003" },
                { "prompt", promt },
                { "temperature", 0.7 },
                { "max_tokens", 400 },
                { "top_p", 1 },
                { "frequency_penalty", 0 },
                { "presence_penalty", 0 },
            };
    //Console.WriteLine(promt);
    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://api.openai.com/v1/completions"));
    request.Content = new StringContent(req.ToString(),
                                        Encoding.UTF8,
                                        "application/json");//CONTENT-TYPE header
    var result = JObject.Parse(httpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result);
    Console.WriteLine(result);
    Console.WriteLine(promt);

    if (result.ContainsKey("error"))
        throw new OpenAIException((string)result["error"]["type"], (string)result["error"]["message"]);
    return (string)result["choices"][0]["text"];
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
public static class Extensions
{
    public static Random random = new Random();
    public static void SendMessage(this VkApi api, string msg, long peerId, long? replyId)
    {
        MessageForward forward = replyId is null ? null : new MessageForward() { IsReply = true, ConversationMessageIds = new List<long>() { (long)replyId }, PeerId = peerId };
        api.Messages.Send(new() { Message = msg, PeerId = peerId, Forward = forward, RandomId = random.Next(1000)});
    }
    public static void SendMessage(this AdditionalApi api, string msg, long peerId, long? replyId)
    {
        api.call("messages.send", new Dictionary<string, object>()
            { { "message", msg }, { "peer_id", peerId}, { "reply_to", replyId }, { "random_id", random.Next(10000) } }).Wait();
    }
    public static IEnumerable<T> TakeLast<T>(this IList<T> list, int count)
    {
        int begin = list.Count - count;
        if (begin < 0)
            begin = 0;

        for (; begin < list.Count; begin++)
            yield return list[begin];
    }
}
public class Command
{
    public delegate void CommandCallback(string[] args, MessageNew msg);
    List<string> _commandCallbacks;
    CommandCallback callback;
    public Command(List<string> commandSyntaxes, CommandCallback callback)
    {
        _commandCallbacks = commandSyntaxes;
        this.callback = callback;
    }
    public bool ExecuteCommand(string cmd, MessageNew msg)
    {
        foreach(var command in _commandCallbacks)
        {
            var regex = Regex.Match(cmd, $"^{command}$");
            if (regex.Success)
            {
                callback(regex.Groups.Values.ForEach((v) => v.Value).ToArray(), msg);
                return true;
            }
        }
        return false;
    }
}

static class ForEachClass
{
    public delegate OUT ConvertValue<IN, OUT>(IN Value);
    public static IEnumerable<OUT> ForEach<IN, OUT>(this IEnumerable<IN> array, ConvertValue<IN, OUT> convertValue)
    {
        IEnumerable<OUT> Enumerator()
        {
            foreach(var i in array)
            {
                yield return convertValue(i);
            }
        }
        return Enumerator();
    }
}

public class ChatSetting
{
    public bool BotEnabled { get; set; }
    public List<string> Memory { get; set; }
    public string Name { get; set; }
    public string Prequel { get; set; }
    public int Chance { get; set; }
    public int MemorySize { get; set; }

    public static ChatSetting Get(long peerId)
    {
        if (!Chats.ContainsKey(peerId))
        {
            Chats.Add(peerId, new ChatSetting()
            {
                BotEnabled = false,
                Memory = new List<string>(),
                Name = "Мико",
                Prequel = "Мико это Искусственный Интелект, который всегда готов помочь ответив на волнующие людей вопросы, и впринципе хороший собеседник, который любит общаться",
                Chance = 0,
                MemorySize = 1
            });
        }
        return Chats[peerId];
    }
    public static string getStringMemory(List<string> memory)
    {
        return string.Join("\n", memory.ToArray());
    }
    
    public static Dictionary<long, ChatSetting> Chats;

    public static void SaveData()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(Chats);
        File.WriteAllText("data.json", json);
    }
}