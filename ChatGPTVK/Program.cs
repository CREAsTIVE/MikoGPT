 using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;
using VkNet.Model;
using VkNet;
using ChatGPTVK;
using Newtonsoft.Json.Linq;
using VkNet.Model.Keyboard;
using VkNet.Enums.SafetyEnums;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ChatGPTVK.Extensions;

using JsonSer = System.Text.Json.JsonSerializer;
using kCol = VkNet.Enums.SafetyEnums.KeyboardButtonColor;
using kType = VkNet.Enums.SafetyEnums.KeyboardButtonActionType;
using VkNet.Enums;
using ChatGPTVK.Models;
using System.Drawing;


Global.Load();
var settingsList = BotSettingsList.Read();
OpenAIApi.Init();


Global.Instance.api = new();
var api = Global.Instance.api;
api.Authorize(new ApiAuthParams()
{
    AccessToken = "vk1.a.gWk27GYJDvB4ZQ96-Rgbi2OOiwYp5yhi4fJbuoZA_iXYyjHqy6mmnqoAKVHRTTVOOv7UlQZZbn_LxgoM21_1LHPjrQuemTfsXUdjp0v7nx7sMGMgJ9WwrF3zKR3fICzpc_XBltK3UE8qvnEEAewcEx4B4WZFb4XQBWIa_5N9agDzVgopbylXFHchxqOMGi80HMTmxmlFC_ItHEEBYEu51w"
});

Global.Instance.groupId = 218182847;
ulong groupId = Global.Instance.groupId;

Global.Instance.userApi = new();
var userApi = Global.Instance.userApi;
userApi.Authorize(new()
{
    AccessToken = "vk1.a.LfKDfp-EbogE_OSBIRwlZ8aYlOFVvJfHTloi8zxCjGsIXH4V2sT8eUUdtLaZM9lG0l8SmyKU2wrRY4eMI1eHN2i5pn_vvsDTHR8tiT6NdztQlNPQtbPDedQ_hxY49IOlLeCTYlErqYXE9ueNzd1o9ro8rSEcCy_CtY6IytQtpKeK0nKop-u_NBXi_mdIKGRY7QtHUWRt-xTP1SxXMXP9qA"
});

CallbackManager callbackManager = new(api, groupId);

callbackManager.onMessageNew += (messageNew) =>
{
    Message msg = messageNew.Message;

    var settings = settingsList[msg.PeerId ?? 0];

    if (msg.PeerId == -(long)Global.Instance.groupId || msg.PeerId == 2000000021)
        return;

    if (msg.Text == $"{Global.Instance.Prefix}conf")
    {
        api.Reply(msg, "Настройки: ", BuildSettingsKeyboard(settings));
        return;
    }
    else if (msg.Text == "/ping")
        api.Reply(msg, "pong!");
    else if (msg.Text == $"{Global.Instance.Prefix}admConf" && new long[] { 216726077, 505354548 }.Contains(msg.PeerId ?? 0))
    {
        api.Reply(msg, "ЧЗХ, ЭТО ЧЁ, АДМИНСКИЕ КОМАНДЫ?", BuildSettingsKeyboard(Global.Instance));
    }
    else if (msg.Text == $"{Global.Instance.Prefix}help")
    {
        api.Reply(msg, "vk.com/@mikogpt-help");
        return;
    }
    else if (msg.ReplyMessage?.Payload is not null)
    {
        ButtonCallbackPayload payload = ButtonCallbackPayload.fromJson(msg.ReplyMessage.Payload);
        switch (payload.Type)
        {
            case ButtonCallbackType.SettingsSetInput:
                typeof(BotSettings).GetProperty(payload.Name ?? "")?.SetValue(settings, msg.Text);
                typeof(Global).GetProperty(payload.Name ?? "")?.SetValue(Global.Instance, msg.Text); //FIXME: переписать без кринжа
                api.Reply(msg, $"Параметр успешно изменён!\nНастройки:", BuildSettingsKeyboard(settings));
                break;
        }
    }
    else if ((msg.Text.StartsWith(Global.Instance.Prefix) && msg.PeerId > 2000000000) || (/*!msg.Text.StartsWith(Global.Instance.Prefix) && */msg.PeerId < 2000000000))//FIXTHIS
    {
        LanguageModel model = settings.Model switch
        {
            ModelType.ChatGPT3_5 => new ChatGPT3_5("gpt-3.5-turbo-0301"),
            ModelType.CustomChatGPT => new CustomChatGPT()
        };
        if (model != null)
            model.Generate(api, msg, settings);
    }
};

callbackManager.onMessageEvent += (messageEvent) =>
{
    var callback = ButtonCallbackPayload.fromJson(messageEvent.Payload);
    var settings = settingsList[messageEvent.PeerId ?? 0];

    switch (callback.Type)
    {
        case ButtonCallbackType.SettingsSet:
            var prop = typeof(BotSettings).GetProperty(callback.Name ?? "");
            prop?.SetValue(settings, callback.Value);
            prop = typeof(Global).GetProperty(callback.Name ?? "");
            prop?.SetValue(settings, callback.Value);
            api.Messages.SendMessageEventAnswer(messageEvent.EventId, messageEvent.UserId ?? 0, messageEvent.PeerId ?? 0, new()
            {
                Type = MessageEventType.SnowSnackbar,
                Text = $"Параметр {prop.Name} изменил своё значение на {prop.GetValue(settings)}"
            });
            api.Messages.Edit(new()
            {
                PeerId = (long)messageEvent.PeerId,
                ConversationMessageId = messageEvent.ConversationMessageId,
                Keyboard = BuildSettingsKeyboard(settings),
                Message = "Настройки: "
            });
            settingsList.Save();
            break;
        case ButtonCallbackType.SettingsSetChoice:
            prop = typeof(BotSettings).GetProperty(callback.Name ?? "");
            prop?.SetValue(settings, prop.GetCustomAttribute<SettingsButtonAttribute>()?.Selectable?[(string)callback.Value ?? throw new("wtf")]);
            api.Messages.SendMessageEventAnswer(messageEvent.EventId, messageEvent.UserId ?? 0, messageEvent.PeerId ?? 0, new()
            {
                Type = MessageEventType.SnowSnackbar,
                Text = "Ура!", //FIXME
            });
            api.Messages.Edit(new()
            {
                PeerId = (long)messageEvent.PeerId,
                ConversationMessageId = messageEvent.ConversationMessageId,
                Keyboard = BuildSettingsKeyboard<BotSettings>(settings),
                Message = "Настройки: "
            });
            settingsList.Save();
            Global.Save();
            break;
        case ButtonCallbackType.SettingsChoiсe:
            var property = typeof(BotSettings).GetProperty(callback.Name ?? throw new Exception("wtf")) ?? throw new Exception("wtf");
            var attribute = property.GetCustomAttribute<SettingsButtonAttribute>();
            var buttons2d = new LinkedList<LinkedList<MessageKeyboardButton>>();
            var selectableEnumerator = attribute?.Selectable?.GetEnumerator() ?? throw new Exception("wtf");

            (int x, int y) maxSize = (5, 6);
            for ((int x, int y) pos = (0, 0); pos.y < maxSize.y; pos.y++)
            {
                var buttons = new LinkedList<MessageKeyboardButton>();
                buttons2d.AddLast(buttons);
                for (; pos.x < maxSize.x; pos.x++)
                {
                    if (!selectableEnumerator.MoveNext())
                    {
                        api.Messages.Edit(new()
                        {
                            PeerId = (long)messageEvent.PeerId,
                            ConversationMessageId = messageEvent.ConversationMessageId,
                            Keyboard = new MessageKeyboard()
                            {
                                Buttons = buttons2d,
                                Inline = true,
                            },
                            Message = "Настройки: "
                        });
                        return;
                    }
                    var current = selectableEnumerator.Current;
                    buttons.AddLast(new MessageKeyboardButton()
                    {
                        Color = kCol.Default,
                        Action = new()
                        {
                            Label = current.Key,
                            Type = kType.Callback,
                            Payload = new ButtonCallbackPayload()
                            {
                                Type = ButtonCallbackType.SettingsSetChoice,
                                Name = property.Name,
                                Value = current.Key
                            }.ToJson()
                        }
                    });
                }
            }
            break;//throw new Exception("To many variants");
        case ButtonCallbackType.SettingsInput:
            api.Messages.Send(new()
            {
                PeerId = messageEvent.PeerId??0,
                Message = $"Ответьте на это сообщени введя новое значение для параметра {typeof(BotSettings).GetProperty(callback.Name)?.GetCustomAttribute<SettingsButtonAttribute>()?.Name}",
                Payload = new ButtonCallbackPayload()
                {
                    Type = ButtonCallbackType.SettingsSetInput,
                    Name = callback.Name
                }.ToJson(),
                RandomId = Random.Shared.Next()
            });
            api.Messages.SendMessageEventAnswer(messageEvent.EventId, messageEvent.UserId ?? 0, messageEvent.PeerId ?? 0, new()
            {
                Type = MessageEventType.SnowSnackbar,
                Text = $"Введите значение..."
            });
            settingsList.Save();
            Global.Save();
            break;
        case ButtonCallbackType.Execute:
            var method = typeof(BotSettings).GetMethod(callback.Name ?? throw new ArgumentNullException());
            method?.Invoke(settings, new object[] { messageEvent, callback });
            method = typeof(Global).GetMethod(callback.Name ?? throw new ArgumentNullException());
            method?.Invoke(Global.Instance, new object[] { messageEvent, callback });
            api.Messages.SendMessageEventAnswer(messageEvent.EventId, messageEvent.UserId ?? 0, messageEvent.PeerId ?? 0, new()
            {
                Type = MessageEventType.SnowSnackbar,
                Text = $"Делаю..."
            });
            break;
    }
};

callbackManager.Listen();

MessageKeyboard BuildSettingsKeyboard<T>(T param) where T : class
{
    MessageKeyboardButton?[][] buttons2d = new MessageKeyboardButton[6][];

    for (var i = 0; i < 6; i++)
        buttons2d[i] = new MessageKeyboardButton[5];

    foreach (var props in typeof(T).GetProperties())
    {
        var attr = props.GetCustomAttribute<SettingsButtonAttribute>();
        if (attr is null)
            continue;

        var requireAttrs = props.GetCustomAttributes<RequireFieldAttribute>();
        Type t = typeof(T);
        bool skip = false;
        foreach (var i in requireAttrs)
            if (!typeof(T)?.GetProperty(i.FieldName)?.GetValue(param)?.Equals(i.Value)??false)
            {
                skip = true; 
                break;
            }

        if (skip)
            continue;

        buttons2d[attr.Position.y][attr.Position.x] = (attr, props.GetValue(param)) switch
        {
            (null, _) => null,
            ({ Selectable: not null, Name: var name }, _) => new()
            {
                Color = kCol.Default,
                Action = new()
                {
                    Label = name,
                    Type = kType.Callback,
                    Payload = new ButtonCallbackPayload()
                    {
                        Type = ButtonCallbackType.SettingsChoiсe,
                        Name = props.Name,
                        Choices = attr.Selectable.Keys
                    }.ToJson()
                }
            },
            ({ Name: var name }, string or int or float) => new()
            {
                Color = kCol.Default,
                Action = new()
                {
                    Type = kType.Callback,
                    Label = name,
                    Payload = new ButtonCallbackPayload()
                    {
                        Type = ButtonCallbackType.SettingsInput,
                        Name = props.Name,
                    }.ToJson()
                }
            },
            ({ Name: var name}, bool col) => new()
            {
                Color = col? kCol.Positive : kCol.Negative,
                Action = new()
                {
                    Type = kType.Callback,
                    Label = name,
                    Payload = new ButtonCallbackPayload()
                    {
                        Type = ButtonCallbackType.SettingsSet,
                        Name = props.Name,
                        Value = !col
                    }.ToJson()
                }
            },
            _ => null
        };
    }
    foreach (var method in typeof(T).GetMethods())
    {
        var attr = method.GetCustomAttribute<SettingsButtonAttribute>();
        if (attr is null)
            continue;

        var requireAttrs = method.GetCustomAttributes<RequireFieldAttribute>();
        Type t = typeof(T);
        bool skip = false;
        foreach (var i in requireAttrs)
            if (!typeof(T)?.GetProperty(i.FieldName)?.GetValue(param)?.Equals(i.Value) ?? false)
            {
                skip = true;
                break;
            }

        if (skip)
            continue;

        buttons2d[attr.Position.y][attr.Position.x] = new()
        {
            Color = kCol.Primary,
            Action = new()
            {
                Type = kType.Callback,
                Label = attr.Name,
                Payload = new ButtonCallbackPayload()
                {
                    Type = ButtonCallbackType.Execute,
                    Name = method.Name
                }.ToJson()
            }
        };
    }
    LinkedList<LinkedList<MessageKeyboardButton>> clearedArray = new();
    foreach (var y in buttons2d)
    {
        LinkedList<MessageKeyboardButton> list = new();
        foreach (var x in y)
        {
            if (x != null)
                list.AddLast(x);
        }
        if (list.Count > 0)
            clearedArray.AddLast(list);
    }
    return new MessageKeyboard()
    {
        Inline = true,
        Buttons = clearedArray
    };
}




public enum ButtonCallbackType
{
    None,
    SettingsSet, // fields: Name, Value
    SettingsChoiсe,
    SettingsSetChoice,
    SettingsInput,
    SettingsSetInput,
    Execute
}
public class ButtonCallbackPayload
{
    public string ToJson() => JsonSer.Serialize(this);
    public static ButtonCallbackPayload fromJson(string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new objectDeserializer());
        return JsonSer.Deserialize<ButtonCallbackPayload>(json, options: new()
        {
            Converters = {new objectDeserializer()}
        });
    }
    public ButtonCallbackType Type { get; set; } = ButtonCallbackType.None;

    public string? Name { get; set; }
    public object? Value { get; set; }
    public IEnumerable<string>? Choices { get; set; }
}
public class objectDeserializer : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var type = reader.TokenType;

        if (type == JsonTokenType.Number)
        {
            var oki = reader.TryGetInt32(out var vali);
            if (oki)
                return vali;
            var okl = reader.TryGetInt64(out var vall);
            if (okl)
                return vall;
            var okd = reader.TryGetDouble(out var val);
            if (okd)
                return val;
        }

        if (type == JsonTokenType.String)
            return reader.GetString();

        if (type == JsonTokenType.True || type == JsonTokenType.False)
            return reader.GetBoolean();
        // copied from corefx repo:
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
    
}
