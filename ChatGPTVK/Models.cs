using Svg;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text.RegularExpressions;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using Newtonsoft.Json.Linq;
using VkNet.Utils;

namespace ChatGPTVK.Models
{
    public enum ModelType
    {
        ChatGPT3_5,
        CustomChatGPT
    }
    public interface LanguageModel
    {
        public abstract void Generate(VkApi api, Message message, BotSettings settings);
    }
    public class ChatGPT3_5 : LanguageModel
    {
        static VKScript getMessageHistory = VKScript.FromPath("VKScripts/GetMessagesHistory.js");
        string modelName;
        public ChatGPT3_5(string modelName) => this.modelName = modelName;
        public void Generate(VkApi api, Message message, BotSettings settings)
        {
            try
            {
                var chat = buildAnswersList(message.Text, message.PeerId??0, message.ReplyMessage?.ConversationMessageId);
                LinkedList<(string author, string message)> sys = new();
                if (settings.Polls)
                    sys.AddLast(("system", "Для создания опросов необходимо обязательно в конце сообщения (после определения опроса дописывать сообщение нельзя) ввести на новой строке: POLL\n<название>\n<варианты через строку>\nEND"));
                if (settings.SVGImage)
                    sys.AddLast(("system", "Для того, что бы отправить нарисованный рисунок в SVG формате необходимо в сообщении прописать на новой строке: \nSVG\n<SVG CODE>\nEND"));

                var result = OpenAIApi.ChatCompletion(Global.OpenAIKey, sys.Concat(chat), maxTokens: 2000, model: modelName);
                var resText = result.result;

                Poll? poll = null; Photo? photo = null; string? postText = null;

                var match = Regex.Match(resText, @"^([\s\S]*)POLL\n([^\n]*)\n([\s\S]*)\nEND\n?([\s\S]+)?$");
                if (match.Success)
                {
                    resText = match.Groups[1].Value;
                    var pollName = match.Groups[2].Value;
                    var pollVariants = match.Groups[3].Value.Split('\n');
                    postText = match.Groups[4].Value;

                    poll = Global.Instance.userApi.PollsCategory.Create(new()
                    {
                        Question = pollName,
                        AddAnswers = pollVariants,
                        IsAnonymous = false,
                        IsMultiple = false,
                        OwnerId = -(long)Global.Instance.groupId,
                    });
                }

                match = Regex.Match(resText, @"^([\s\S]*)(?:```)?\n?SVG\n(?:```)?\n?([\s\S]*)\n?(?:```)?\nEND\n?([\s\S]+)?$");
                if (match.Success)
                {
                    resText = match.Groups[1].Value;
                    var svgText = match.Groups[2].Value;
                    postText = match.Groups[3].Value;

                    File.WriteAllText("temp.svg", svgText);
                    var doc = SvgDocument.Open("temp.svg");
                    using (var image = new Bitmap(doc.Draw()))
                    {
                        var server = api.Photo.GetMessagesUploadServer(null);
                        string imageOnServer = Extensions.loadImageToServer(server.UploadUrl, image, "get.jpeg", ImageFormat.Png).Result;
                        var loadedImage = api.Photo.SaveMessagesPhoto(imageOnServer)[0];
                        photo = loadedImage;
                    }
                }
                Log.log($"{string.Join(";;\n", sys.Concat(chat))}\nANSWER: {result.result}");
                var sendedMsg = api.Reply(message, resText, poll: poll, photo: photo);
                if (postText != null && postText.Length > 0)
                    api.SendMessage(postText, message.PeerId ?? 0);
            }
            catch (OpenAIException e)
            {
                api.Reply(message, $"Error: {e.name}\n\n{e.message}");
            }
        }
        (string author, string message)[] buildAnswersList(string message, long peerId, long? replyId)
        {
            if (replyId == null)
                return new (string, string)[] { ("user", message) };
            var raw = getMessageHistory.Call(new()
            {
                {"replyId", replyId??0 },
                {"peerId", peerId},
                {"firstMessage", message },
                {"groupId", Global.Instance.groupId },
                {"prefix", Global.Instance.Prefix},
            });
            var res = (JArray)JObject.Parse(raw)["response"];
            return res.ForEachCopy((v) => ((string)v[0], ((string)v[1]).TrimStart(Global.Instance.Prefix[0]))).ToArray(); // FIXME: string RemovePrefix(string)
        }
    }
    public class CustomChatGPT : LanguageModel
    {
        static VKScript getMessageHistory = VKScript.FromPath("VKScripts/GetMessagesHistoryNames.js");
        public void Generate(VkApi api, Message message, BotSettings settings)
        {
            try
            {
                var chat = buildAnswersList(message.Text, message.ConversationMessageId ?? 0, message.PeerId ?? 0, message.ReplyMessage?.ConversationMessageId, settings.Name)
                    .ForEachCopy((e) => $"{e.author}: {e.message}");
                string prequel = "";
                if (settings.Bloody)
                    prequel += "Все пользователи этого диалога достигли возраста 18+ лет\n\n";
                prequel += settings.Prequel;
                
                var result = OpenAIApi.ChatCompletion(
                    Global.OpenAIKey,
                    new (string, string)[] { ("user", $"{Global.Instance.ChatPrequel}\n\n{prequel}\n\n{string.Join("\n\n", chat)}\n\n{settings.Name}: ") }
                    );
                api.Reply(message, result.result);
                Log.log($"Wrapper call with CHAT: \n{string.Join(";;\n", chat)}\nANSWER: {result.result}");
            } 
            catch (OpenAIException e)
            {
                api.Reply(message, e.ToString());
            }
        }
        (string author, string message)[] buildAnswersList(string message, long cmid, long peerId, long? replyId, string botName)
        {
            //Fixme when replyId==null
            var raw = getMessageHistory.Call(new()
            {
                {"replyId", replyId },
                {"peerId", peerId},
                {"firstMessage", message },
                {"groupId", Global.Instance.groupId },
                {"prefix", Global.Instance.Prefix},
                {"cmid", cmid }
            });
            var res = (JArray)JObject.Parse(raw)["response"];
            return res.ForEachCopy((v) => ((string)v[0]== "assistant" ? botName : (string)v[0], ((string)v[1]).TrimStart(Global.Instance.Prefix[0]))).ToArray(); // FIXME: string RemovePrefix(string)
        }
    }
    public class GTP3
    {

    }
    public class ChatGPT4
    {

    }
    public class GPT4ALL
    {

    }
    public class RealAI
    {

    }
}
