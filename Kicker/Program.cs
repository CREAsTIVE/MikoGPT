using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Enums.StringEnums;
using VkNet.Exception;
using VkNet.Model;

var api = new VkApi();
api.Authorize(new ApiAuthParams()
{
    AccessToken = "vk1.a.gWk27GYJDvB4ZQ96-Rgbi2OOiwYp5yhi4fJbuoZA_iXYyjHqy6mmnqoAKVHRTTVOOv7UlQZZbn_LxgoM21_1LHPjrQuemTfsXUdjp0v7nx7sMGMgJ9WwrF3zKR3fICzpc_XBltK3UE8qvnEEAewcEx4B4WZFb4XQBWIa_5N9agDzVgopbylXFHchxqOMGi80HMTmxmlFC_ItHEEBYEu51w"
});
ulong groupId = 218182847;
long? chatPeerId = File.Exists("key.txt") ? long.Parse(File.ReadAllText("key.txt")) : null;

List<long> whiteList = null;
if (chatPeerId != null)
    whiteList = api.Messages.GetConversationMembers(chatPeerId.Value).Items.ForEachCopy((val) => val.MemberId).ToList();


Console.WriteLine("пон");

while (true)
{
    try
    {
        var lpServer = api.Groups.GetLongPollServer(groupId);
        while (true)
        {
            BotsLongPollHistoryResponse? lp = null;
            try
            {
                lp = api.Groups.GetBotsLongPollHistory(new()
                {
                    Key = lpServer.Key,
                    Server = lpServer.Server,
                    Ts = lpServer.Ts,
                    Wait = 20
                });
            }
            catch (LongPollKeyExpiredException)
            {
                lpServer = api.Groups.GetLongPollServer(groupId);
            }
            if (lp == null)
                continue;
            lpServer.Ts = lp.Ts;
            foreach (var i in lp.Updates)
            {
                if (i.Instance is MessageNew msgNew)
                {
                    var msg = msgNew.Message;

                    if (msg.Text == "///here" /*&& new long[] { 216726077 }.Contains((long)msg.FromId)*/)
                    {
                        chatPeerId = msg.PeerId; File.WriteAllText("key.txt", chatPeerId.ToString());
                        whiteList = api.Messages.GetConversationMembers(chatPeerId.Value).Items.ForEachCopy((val) => val.MemberId).ToList();
                        DeleteMessage(msg.ConversationMessageId ?? 0, msg.PeerId ?? 0);
                    }
                    if (chatPeerId == msg.PeerId)
                    {
                        if (!whiteList.Contains((long)msg.FromId))
                        {
                            //var result = api.Messages.Delete(conversationMessageIds: new ulong[] {(ulong)msg.ConversationMessageId}, peerId: (ulong)msg.PeerId, deleteForAll: true);
                            DeleteMessage(msg.ConversationMessageId ?? 0, msg.PeerId ?? 0);
                            api.SendMessage($"новое сообщение от [id{msg.FromId}|чел]:\n{msg.Text}", 317051088);
                            whiteList.Add((long)msg.FromId);
                            Console.WriteLine(msg.Text);
                        }
                    }
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}


async void DeleteMessage(long id, long peerId)
{
    var client = new HttpClient();
    var request = new HttpRequestMessage
    {
        Method = HttpMethod.Post,
        RequestUri = new Uri("https://api.vk.com/method/messages.delete?v=5.131"),
        Content = new FormUrlEncodedContent(new Dictionary<string, string>{
        { "access_token", api?.Token },
        { "cmids", id.ToString() },
        { "peer_id", peerId.ToString() },
        { "delete_for_all", "1" },
        { "v", "5.131" }
        })
    };
    var response = await client.SendAsync(request);
    var responseContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine(responseContent);
}

#region Extensions
static class Extensions
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
    public static Random random = new Random();
    public static long SendMessage(this VkApi api, string msg, long peerId, long? replyId = null, MessageKeyboard? messageKeyboard = null)
    {
        MessageForward? forward = replyId is null ? null : new MessageForward() { IsReply = true, ConversationMessageIds = new List<long>() { (long)replyId }, PeerId = peerId };
        MessagesSendParams messagesSendParams = new()
        {
            Message = msg,
            PeerId = peerId,
            Forward = forward,
            RandomId = random.Next(1000)
        };
        if (messageKeyboard != null)
            messagesSendParams.Keyboard = messageKeyboard;
        return api.Messages.Send(messagesSendParams);
    }
    public static long Reply(this VkApi api, Message message, string text, MessageKeyboard? messageKeyboard = null)
    {
        return api.SendMessage(text, (long)message.PeerId, message.ConversationMessageId, messageKeyboard);
    }
    public static void ReplyRem(this VkApi api, Message message, string text, MessageKeyboard? messageKeyboard = null)
    {
        api.Reply(message, text, messageKeyboard);
        try
        {
            api.Messages.Delete(new ulong[] { (ulong)message.ConversationMessageId }, (ulong)message.PeerId);
        }
        catch (Exception e) { }
    }
    public enum MessageEventTypes
    {
        RequestAccessToSetChatKey
    }
    public static MessageKeyboard BuildRequestAccessToSetChatKeyButton(int key)
    {
        JObject payload = new JObject()
        {
            { "type", MessageEventTypes.RequestAccessToSetChatKey.ToString()},
            { "key", key }
        };
        MessageKeyboard keyboard = new MessageKeyboard()
        {
            Inline = true,
            Buttons = new MessageKeyboardButton[][]
            {
                new MessageKeyboardButton[] {
                    new() { Action = new() { Type = KeyboardButtonActionType.Callback, Label = "Разрешить", Payload = payload.ToString() } }
                }
            }
        };
        return keyboard;
    }
    public static bool isAdmin(this VkApi api, long peerId, long userId)
    {
        var members = api.Messages.GetConversationMembers(peerId: peerId);
        foreach (var i in members.Items)
        {
            if (i.MemberId == userId)
                return i.IsAdmin;
        }
        return false;

    }
    public static bool TryGetIndexOf<T>(this List<T> array, T value, out int result)
    {
        result = -1;
        var f = array.IndexOf(value);
        if (f < 0)
            return false;
        result = f;
        return true;
    }
    public static Dictionary<string, string> ReplaceDictionary = new()
    {
        {"😆", "xD"}, {"»", ">>"}, {"«", "<<"}, {"😊", ":-)"}
    };
    public static string UnformatVK(this string str)
    {
        string newString = str;
        foreach (var i in ReplaceDictionary)
            newString = newString.Replace(i.Key, i.Value);
        return newString;
    }
}
#endregion

