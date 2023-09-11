using ChatGPTVK;
using MikoGPT.apis;
using MikoGPT.ImageApis;
using MikoGPT.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Utils;
using static MikoGPT.ImageApis.Dalle2AiApi;

namespace MikoGPT
{
    public abstract class LanguageModel
    {
        public static VkApi api = new();

        public abstract void Response(Message message, ChatData chatData);
        public virtual string GetModelName() => "LanguageModel";

        public static LanguageModel GetModel(LanguageModelType t) => (LanguageModel?)Activator.CreateInstance(FromType(t)) ?? throw new NotSupportedException();
        public abstract LanguageModelType GetModelType();

        public static (LanguageModelType langModel, Type type)[] Associations = new (LanguageModelType, Type)[]
        {
            (LanguageModelType.ChatGPT3_5, typeof(ChatGPT3_5)),
            (LanguageModelType.Dalle2, typeof(Dalle2)),
            (LanguageModelType.IdkImageAi, typeof(IdkImageAi)),
            (LanguageModelType.DeepAiImage, typeof(DeepAiImage))
        };
        public static LanguageModelType FromType(Type type)
        {
            foreach (var association in Associations)
                if (association.type.Equals(type))
                    return association.langModel;
            throw new NotImplementedException();
        }
        public static Type FromType(LanguageModelType type)
        {
            foreach (var association in Associations)
                if (association.langModel == type)
                    return association.type;
            throw new NotImplementedException();
        }
    }
    namespace Models
    {
        public enum LanguageModelType
        {
            ChatGPT3_5,
            Dalle2,
            IdkImageAi,
            DeepAiImage
        }

        public class DeepAiImage : LanguageModel
        {
            public override LanguageModelType GetModelType() => LanguageModelType.DeepAiImage;

            public override void Response(Message message, ChatData chatData)
            {
                var image = DeepAIImageApi.Instance.GenerateImage(chatData.RemovePrefix(message.Text));
                Photo photo = api.UploadImage(image);
                api.Messages.Send(new()
                {
                    RandomId = Random.Shared.Next(),
                    Attachments = new[] {photo},
                    PeerId = message.PeerId,
                    Forward = new()
                    {
                        IsReply = true,
                        PeerId = message.PeerId,
                        ConversationMessageIds = new[] { message.ConversationMessageId ?? 0 }
                    }
                });
            }
        }

        public class IdkImageAi : LanguageModel
        {
            public override LanguageModelType GetModelType() => LanguageModelType.IdkImageAi;
            public override string GetModelName() => "Better image";

            public override void Response(Message message, ChatData chatData)
            {
                var images = IdkImageAiApi.Instance.GenerateImage(chatData.RemovePrefix(message.Text));
                Photo[] photos = new Photo[9];
                for (var i = 0; i < photos.Length; i++)
                {
                    photos[i] = api.UploadImage(images[i]);
                }

                api.Messages.Send(new()
                {
                    RandomId = Random.Shared.Next(),
                    Attachments = photos,
                    PeerId = message.PeerId,
                    Forward = new()
                    {
                        IsReply = true,
                        PeerId = message.PeerId,
                        ConversationMessageIds = new[] { message.ConversationMessageId ?? 0 }
                    }
                });
            }
        }
        public class Dalle2 : LanguageModel
        {
            public override LanguageModelType GetModelType() => LanguageModelType.Dalle2;

            public override void Response(Message message, ChatData chatData)
            {
                try
                {
                    var image = Instance.GenerateImage(chatData.RemovePrefix(message.Text));
                    try
                    {
                        var photo = api.UploadImage(image);

                        api.Messages.Send(new()
                        {
                            RandomId = Random.Shared.Next(),
                            Attachments = new[] { photo },
                            PeerId = message.PeerId,
                            Forward = new()
                            {
                                IsReply = true,
                                PeerId = message.PeerId,
                                ConversationMessageIds = new[] { message.ConversationMessageId ?? 0 }
                            }
                        });
                    } catch (Exception e) 
                    {
                        if (e is GenerationException)
                            throw e;
                        api.Reply(message, "Не удалось отправить изображение.");
                        var fileName = Databases.JsonDocsDatabase.GetRandomBase64Uid();
                        Logger.Instance?.Log("dalle2 ex", $"vk cringe, image saved in {fileName}");
                        File.WriteAllBytes($"{fileName}.png", image);
                    }
                } catch (GenerationException ex)
                {
                    api.Reply(message, ex.Reason);
                }
            }
            public override string GetModelName() => "DALLE 2";
        }

        public class ChatGPT3_5 : LanguageModel
        {
            public static VKScript GetMessageChainScript = VKScript.FromFile("message-chain");
            public string? Prequel { get; set; } = null;
            struct AuthorMessagePair
            {
                public string Author;
                public string Message;
                public AuthorMessagePair(string author, string message)
                {
                    Author = author;
                    Message = message;
                }
                public AuthorMessagePair(string author, string message, IEnumerable<(string name, string content)> files)
                {
                    Author = author; Message = message;
                    foreach (var file in files)
                        Message += $"\n{file.name}:\n```\n{file.content}\n```";
                }
                public static HttpClient client = new();
                public AuthorMessagePair(string author, string message, IEnumerable<Document> documents) 
                    : this(author, message, documents.Select((document) => (document.Title, client.GetAsync(document.Uri ?? throw new("WTF")).Result.Content.ReadAsStringAsync().Result))) { }
            }
            static HttpClient httpClient = new();
            public override void Response(Message message, ChatData chatData)
            {
                IEnumerable<(string author, string message)> messageChain;
                if (message.ReplyMessage?.ConversationMessageId is not null)
                {
                    VkResponse messageChainRaw = GetMessageChainScript.Execute(api, new Dictionary<string, object?>
                    {
                        {"firstMessageId", message.ReplyMessage?.ConversationMessageId },
                        {"maxMessagesCount", 15 },
                        {"peerId", message.PeerId}
                    });
                    var messageChainJson = JToken.Parse(messageChainRaw.RawJson)["response"];
                    messageChain = messageChainJson
                        .Select((e) => new AuthorMessagePair(
                            (long?)e[0]<0 ? "assistant" : "user", 
                            (string?)e[1] ?? throw new ArgumentNullException(), 
                            e?[2]
                                ?.Where((obj) => ((string?)obj?["type"]) == "doc")
                                ?.Select((obj) => (
                                    (string?)obj?["doc"]?["title"] ?? "file",
                                    httpClient.GetAsync((string?)obj?["doc"]?["url"] ?? throw new ArgumentNullException()).Result.Content.ReadAsStringAsync().Result)) ?? new (string, string)[0]
                                )
                        )
                        .Concat(new AuthorMessagePair[] { 
                            new(
                                "user", 
                                message.Text, 
                                message.Attachments
                                    .Where((att) => att.Instance is Document)
                                    .Select((att) => att.Instance as Document ?? throw new())) 
                        })
                        .Select((p) => (p.Author, p.Message.Trim()));
                } else
                {
                    var pair = new AuthorMessagePair(
                        "user",
                        message.Text,
                        message.Attachments
                            .Where((att) => att.Instance is Document)
                            .Select((att) => att.Instance as Document ?? throw new())
                        );
                    messageChain = new (string, string)[] { ("user", pair.Message) };
                }

                messageChain = messageChain.Select((pair) => (pair.author, unpackUrls(chatData.RemovePrefix(pair.message))));
                if (Prequel != null)
                    messageChain = prequelParser(Prequel).Concat(messageChain);
                else
                    messageChain = new (string, string)[] { ("system", "Весь текст, который надо отправить моноширным шрифтом или код ВСЕГДА должен быть в ``` code ```")}.Concat(messageChain);

                var resultText = Config.MainCompletor.DirectChatCompletion(messageChain);
                var groups = fixGroupsSpaces(resultText);

                Logger.Instance?.Log("model response", $"Responsed on {string.Join(", ", messageChain)} by {resultText}");

                api.Messages.Send(new()
                {
                    RandomId = Random.Shared.Next(),
                    PeerId = message.PeerId,
                    Attachments = groups.photos,
                    Message = groups.newMessage,
                    Forward = new MessageForward()
                    {
                        ConversationMessageIds = new long[] {message.ConversationMessageId ?? 0},
                        IsReply = true,
                        PeerId = message.PeerId
                    }
                }); 

            }
            static Regex regex = new(@"\[http://[^:]*:\d*/code\?code_uid=([^\]]*)\]");
            string unpackUrls(string text)
            {
                return regex.Replace(text, (m) =>
                {
                    return $"```{IDatabase.Instance?.LoadCode(m.Groups[1].Value)}```";
                });
            }
            static IEnumerable<(string author, string message)> prequelParser(string prequel)
            {
                if (prequel.StartsWith("|"))
                {
                    var preq = prequel.Split("\n", 2);
                    var type = preq[0][1..].Split("|").Select(s => s.Trim()).ToArray();
                    var content = preq[1];

                    return type switch
                    {
                        ["parse"] => content.Split(";;", StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().Split(":", 2))
                            .Select(s => (s[0], s[1])),
                        ["parse", var spliter] => content.Split(spliter)
                            .Select(s => s.Trim().Split(":", 2))
                            .Select(s => (s[0], s[1])),
                        _ => new (string, string)[] { ("system", prequel) }
                    };
                }
                return new (string, string)[] { ("system", prequel) };
            }
            static (string newMessage, IEnumerable<Photo> photos) fixGroupsSpaces(string text)
            {
                var parts = text.Split("```");
                var photos = new LinkedList<Photo>();
                for (int i = 0; i < (parts.Length - 1) / 2; i++)
                {
                    string txt = parts[(i * 2) + 1];
                    IEnumerable<string> split = txt.Split("\n");
                    var str = split.First();
                    while (string.IsNullOrEmpty(str))
                    {
                        split = split.Skip(1);
                        str = split.First();
                    }
                    txt = string.Join("\n", split);

                    var image = ImageUtils.DrawText(txt);
                    photos.AddLast(api.UploadImage(image));
                    var codeUid = IDatabase.Instance?.SaveCode(txt);
                    parts[(i * 2) + 1] = $"[{Config.Instance.Web.Ip}/code?code_uid={codeUid}]";
                }

                return (string.Join("", parts), photos);
            }
            public override string GetModelName() => $"ChatGPT 3.5";

            public override LanguageModelType GetModelType() => LanguageModelType.ChatGPT3_5;
        }
    }
    public class LanguageModelConverter : System.Text.Json.Serialization.JsonConverter<LanguageModel>
    {
        public override bool CanConvert(Type type) => typeof(LanguageModel).IsAssignableFrom(type);

        public override LanguageModel Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new System.Text.Json.JsonException();
            }

            if (!reader.Read()
                    || reader.TokenType != JsonTokenType.PropertyName
                    || reader.GetString() != "TypeDiscriminator")
            {
                throw new System.Text.Json.JsonException();
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new System.Text.Json.JsonException();
            }

            LanguageModel baseClass;
            LanguageModelType typeDiscriminator = (LanguageModelType)reader.GetInt32();

            if (!reader.Read() || reader.GetString() != "TypeValue")
                throw new System.Text.Json.JsonException();
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new System.Text.Json.JsonException();

            Type type = LanguageModel.FromType(typeDiscriminator);

            baseClass = System.Text.Json.JsonSerializer.Deserialize(ref reader, type) as LanguageModel ?? throw new ArgumentNullException();

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new System.Text.Json.JsonException();
            }

            return baseClass;
        }

        public override void Write(
            Utf8JsonWriter writer,
            LanguageModel value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value is LanguageModel model)
            {
                writer.WriteNumber("TypeDiscriminator", (int)model.GetModelType());
                writer.WritePropertyName("TypeValue");
                System.Text.Json.JsonSerializer.Serialize(writer, model, LanguageModel.FromType(model.GetModelType()));
            } else
            {
                throw new NotSupportedException();
            }

            writer.WriteEndObject();
        }
    }
}
