using MikoGPT;
using MikoGPT.apis;
using MikoGPT.ConsoleCommandController;
using MikoGPT.ImageApis;
using MikoGPT.Models;
using MikoGPT.VKButtonGUIFieldAttributes;
using MikoGPT.Web;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.Net;
using System.Text.RegularExpressions;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using static MikoGPT.ButtonPayload;

#region Initialization



Logger.Instance = new Logger(/*"main.txt"*/);

// Config file
Config.Instance = Config.LoadFromFile("config.json");

// Instance fetchers
{
    Logger.Instance.Log("main", "Loaded config from config.json");

    Regex hideMyNameRegex = new(@"<td>(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})<\/td><td>(\d{1,6})<\/td>");

    Regex freeProxyListNet = new Regex(@"<textarea class=""form-control"" readonly=""readonly"" rows=""12"" onclick=""select\(this\)"">Free proxies from free-proxy-list\.net\nUpdated at \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC\.\n\n([^<]*)\n<\/textarea>");
    Config.DefaultProxyFetchers = new()
    {
        () => freeProxyListNet.Match(new HttpClient().GetAsync("https://free-proxy-list.net/").Result.Content.ReadAsStringAsync().Result).Groups[1].Value.Split("\n"),
        /*() => 
        {
            var hideMyNameRequest = new HttpRequestMessage(HttpMethod.Get, "https://hidemyna.me/en/proxy-list/");
            hideMyNameRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            return hideMyNameRegex.Matches(new HttpClient().SendAsync(hideMyNameRequest).Result.Content.ReadAsStringAsync().Result).Select((r) => $"{r.Groups[1]}:{r.Groups[2]}");
        }*/
    };
}

Dalle2AiApi.Instance = new();
IdkImageAiApi.Instance = new();
DeepAIImageApi.Instance = new();

// VK API
VkApi api = new VkApi();
api.Authorize(new()
{
    AccessToken = Config.Instance.VKAPIKey ?? throw new ConfigParameterException(nameof(Config.Instance.VKAPIKey)),
});
VKButtonGUI.api = api;

// VK Callback manager
VKCallbackManager callbackManager = new(api);
Logger.Instance.Log("main", "Initializated VK API");

// OpenAI
OpenAIApi.Init();

// Regex
var pingMatcher = new Regex(@$"^\[club{Config.Instance.GroupId}\|.*\]\s*([\s\S]+)$");

// Database
IDatabase database = new MikoGPT.Databases.JsonDocsDatabase();
database.Load(Config.Instance.DatabasePath);
IDatabase.Instance = database;

// DeepAi completor
LanguageModel.api = api;
{
    var completorProxy = new ProxyManager(
        Config.DefaultProxyFetchers,
        (proxy) =>
        {
            try
            {
                DeepAiApi.DoCompletionByProxy(new (string, string)[] { ("user", "say test") }, proxy.ToClient(15));
                return true;
            }
            catch (AggregateException) 
            { return false; }
            catch (Exception ex) 
            { Logger.Instance.Log("proxy runtime", ex.ToString(), Logger.WarningLevel.Error); return false; }
        }
        );
    Config.MainCompletor = new DeepAiApi(completorProxy);
}

// Web api keys
ApiKeysManager.Instance = ApiKeysManager.LoadOrCreateFromFile(Config.Instance.Web.KeysPath);

// Web interface
string mainDomain = Environment.GetEnvironmentVariable("server") ?? Config.Instance.Web.Ip;
Logger.Instance.Log("web", $"Current domain: {mainDomain}");
WebInterface.Instance = new WebInterface();
WebInterface.Instance.Register(mainDomain, "/favicon.ico", new FileResponse("web-interface/favicon.ico"));
WebInterface.Instance.Register(mainDomain, "/code", new CodeResponse());
WebInterface.Instance.Register($"api.{mainDomain}", "/chat", new ApiChatResponse());
WebInterface.Instance.RegisterWebPage($"{mainDomain}", "/chat", "web-interface/chat");
WebInterface.Instance.Register(mainDomain, "/sitemap", new FileResponse("web-interface/sitemap.txt"));
WebInterface.Instance.Listen();
Logger.Instance.Log("main", "Initializated web interface");

// Console
ConsoleCommandController consoleCommandController = new();
#endregion


// VK callback

callbackManager.onMessageNew += (messageNew) =>
{
    
    Message message = messageNew.Message;
    long peerId = message.PeerId ?? 0;
    long chatId = message.ChatId ?? 0;
    Logger.Instance.Log("vk runtime", $"New message from {message.FromId} in {message.PeerId}: {message.Text}");

    if (message.FromId < 0)
        return;

    if (message.Text == "!!!BAN!!!" && (message.FromId == 216726077 || message.FromId == 317051088))
    {
        var r = api.Messages.GetConversationMembers(peerId, fields: new string[] { "deactivated" }).Profiles.Where((p) => p.Deactivated == Deactivated.Banned || p.Deactivated == Deactivated.Deleted);
        foreach (var profile in r)
        {
            Task.Delay(500).Wait();
            api.Messages.RemoveChatUser((ulong)(peerId-2000000000), profile.Id);
        }
        return;
    }

    var match = pingMatcher.Match(message.Text);
    if (VKButtonGUI.ParametersForUpdate.TryGetValue((peerId, message.ReplyMessage?.ConversationMessageId ?? 0), out var param))
    {
        object workedObject = param.objType == ObjectType.Userdata ? database.GetUserData(message.FromId ?? 0) : database.GetChatData(peerId);
        var property = workedObject.GetType().GetProperty(param.paramName) ?? throw new ArgumentException("Unknow property");
        property.SetValue(workedObject, message.Text);

        var keyboardButtons = VKButtonGUI.Build(workedObject, param.objType);
        api.Messages.Edit(new()
        {
            ConversationMessageId = message.ReplyMessage?.ConversationMessageId,
            PeerId = peerId,
            Message = keyboardButtons.message,
            Keyboard = new()
            {
                Inline = true,
                Buttons = keyboardButtons.buttons
            }
        });
        if (param.objType == ObjectType.Userdata)
            database.SaveUserData(message.UserId ?? 0, workedObject as UserData ?? throw new ArgumentException("Wrong workedObject type"));
        else if (param.objType == ObjectType.Chatdata)
            database.SaveChatData(peerId, workedObject as ChatData ?? throw new ArgumentException("Wrong workedObject type"));
        return;
    }
    else if (match.Success) // global command
    {
        string content = match.Groups[1].Value;

        
        if (content == "ping")
            api.Reply(message, "pong!");
        else if (content == "params")
        {
            var chatSettings = database.GetChatData(peerId);
            var keyboardButtons = VKButtonGUI.Build(
                chatSettings.Menu == ChatData.MenuType.UserSettings ? database.GetUserData(message.UserId ?? 0) : chatSettings,
                chatSettings.Menu == ChatData.MenuType.UserSettings ? ButtonPayload.ObjectType.Userdata : ButtonPayload.ObjectType.Chatdata);
            api.Messages.Send(new()
            {
                PeerId = peerId,
                Message = keyboardButtons.message,
                Keyboard = new()
                {
                    Inline = true,
                    Buttons = keyboardButtons.buttons
                },
                RandomId = Random.Shared.Next(1000),
                Intent = VkNet.Enums.SafetyEnums.Intent.Default
            });
        }
        else
            api.Reply(message, "Неизвестная команда. Пожалуйста обратитесь к vk.com/@mikogpt-help");

        return;
    }
    
    ChatData chatData = database.GetChatData(peerId: peerId);
    bool sucsesfull = false;
    foreach (var prefix in chatData.Prefixes)
    {
        if (message.Text.StartsWith(prefix.prefix))
        {
            var matchedResult = Regex.Match(message.Text, @$"{prefix.prefix}\s*([\s\S]*)");
            if (matchedResult.Success)
            {
                string content = matchedResult.Groups[1].Value;
                LanguageModel model = prefix.model;
                model.Response(message, chatData); 
                sucsesfull = true;
                break;
            }
        }
    }
    if (!sucsesfull && message.FromId == message.PeerId)
    {
        LanguageModel model = chatData.SelectedPrefix.model;
        model.Response(message, chatData);
    }
};

callbackManager.onMessageEvent += (messageEvent) =>
{
    ButtonPayload buttonPayload = ButtonPayload.FromJson(messageEvent.Payload);

    VKButtonGUI.OnPressed(buttonPayload, messageEvent.PeerId ?? 0, messageEvent.ConversationMessageId??0, messageEvent.UserId ?? 0, messageEvent, database);
    
};



// Console callback

consoleCommandController.RegisterCommand("key", (args) =>
{
    switch (args.GetNext())
    {
        case "add":
            Console.WriteLine($"New api key: {ApiKeysManager.Instance.AddKey(new ApiKeysManager.KeyInfo(args.GetNext()))}");
            return;
        case "remove":
            switch (args.GetNext())
            {
                case "name":
                    Console.WriteLine($"Removed: {ApiKeysManager.Instance.RemoveKeyByName(args.GetNext())}");
                    return;
                case "key":
                    Console.WriteLine($"Removed: {ApiKeysManager.Instance.RemoveKey(args.GetNext())}");
                    return;
            }
            return;
        case "list":
            Console.WriteLine($"Key list:\n{string.Join('\n', ApiKeysManager.Instance.KeysList.Select((k) => k.UserName))}");
            return;
        case "get":
            Console.WriteLine($"key value: {ApiKeysManager.Instance.FindByName(args.GetNext())?.Key}");
            return;
    }
});

consoleCommandController.ListenAsync();

callbackManager.LongPull();

#region VKAPIExtensions
static class VKAPIExtensions
{
    public static void Send(this VkApi api, long peerId, string text, long? replyId = null, IEnumerable<long>? replyIds = null)
    {
        var param = new MessagesSendParams()
        {
            Message = text,
            PeerId = peerId,
            RandomId = Random.Shared.Next(1000),
            Intent = VkNet.Enums.SafetyEnums.Intent.Default
        };
        if (replyId is not null)
            param.Forward = new()
            {
                ConversationMessageIds = new long[] { (long)replyId },
                IsReply = true,
                PeerId = peerId,
            };
        else if (replyIds is not null)
            param.Forward = new()
            {
                ConversationMessageIds = replyIds,
                IsReply = false,
                PeerId = peerId,
            };
        api.Messages.Send(param);
    }

    public static void Reply(this VkApi api, Message message, string text) =>
        api.Send(message.PeerId ?? 0, text, replyId: message.ConversationMessageId);
}
#endregion

public static class NETExtensions
{
    public delegate OUT ConvertValue<IN, OUT>(IN Value);
    public static IEnumerable<OUT> ForEachCopy<IN, OUT>(this IEnumerable<IN> array, ConvertValue<IN, OUT> convertValue)
    {
        IEnumerable<OUT> Enumerator()
        {
            foreach (var i in array)
            {
                yield return convertValue(i);
            }
        }
        return Enumerator();
    }
}