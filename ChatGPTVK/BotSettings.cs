
using ChatGPTVK.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;
using VkNet.Model.GroupUpdate;

namespace ChatGPTVK
{
    public class Global
    {
        const string path = "config.json";
        public static Global? Load()
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Global>(File.ReadAllText(path));
            return new Global();
        }
        public static void Save()
        {
            File.WriteAllText(path, JsonSerializer.Serialize(Instance));
        }
        public static Global Instance = Load() ?? new Global();
        internal static string OpenAIKey = "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";
        public VkApi api = new VkApi();
        public ulong groupId = 0;
        public VkApi userApi = new VkApi();

        [SettingsButton("Показать", 0, 0)]
        public void ShowValues(MessageEvent e, ButtonCallbackPayload? callback)
        {
            string values = $"temperature: {Temperature}\n" +
                $"top_p: {TopP}\n" +
                $"max_tokens: {MaxTokens}\n" +
                $"presence_penalty: {PresencePenalty}\n" +
                $"frequency_penalty: {FrequancyPenalty}\n";
            api.SendMessage($"{values}Префикс: {Prefix}\nПриквел чата: {ChatPrequel}", e.PeerId??0);
        }

        [SettingsButton("Префикс", 0, 1)]
        public string Prefix { get; set; } = "/";
        [SettingsButton("Приквел чата", 1, 1)]
        public string ChatPrequel { get; set; } = "Закончи то, что сказал бы этот участник диалога. Не начинай писать за нового участника диалога";

        [SettingsButton("temperature", 0, 2)]
        public string Temperature { get; set; } = "0.6";
        [SettingsButton("top_p", 1, 2)]
        public string TopP { get; set; } = "0.2";
        [SettingsButton("max_tokens", 2, 2)]
        public string MaxTokens { get; set; } = "1000";
        [SettingsButton("presence_penalty", 0, 3)]
        public string PresencePenalty { get; set; } = "1";
        [SettingsButton("frequency_penalty", 1, 3)]
        public string FrequancyPenalty { get; set; } = "1";
    }
    public class BotSettings
    {
        [SettingsButton("Модель", 0, 0, "Models")]
        public ModelType Model { get; set; } = ModelType.ChatGPT3_5;

        //ChatGPT3.5
        [SettingsButton("Опросы", 0, 1)]
        [RequireField(nameof(Model), ModelType.ChatGPT3_5)]
        public bool Polls { get; set; } = false;

        //[SettingsButton("Изображения", 1, 1)]
        [RequireField(nameof(Model), ModelType.ChatGPT3_5)]
        public bool SVGImage { get; set; } = false;

        //Custom ChatGPT
        [SettingsButton("Имя", 0, 1)]
        [RequireField(nameof(Model), ModelType.CustomChatGPT)]
        public string Name { get; set; } = "Assistent";

        [SettingsButton("Приквел", 1, 1)]
        [RequireField(nameof(Model), ModelType.CustomChatGPT)]
        public string Prequel { get; set; } = "Assistent - это ассистент.";

        [SettingsButton("18+", 2, 1)]
        [RequireField(nameof(Model), ModelType.CustomChatGPT)]
        public bool Bloody { get; set; } = false;
    }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    class SettingsButtonAttribute : Attribute
    {
        public string Name { get; }
        public Dictionary<string, object>? Selectable { get; set; } = null;
        public (int x, int y) Position = (0, 0);
        public SettingsButtonAttribute(string name, int xPos, int yPos) => (Name, Position) = (name, (xPos, yPos));
        public SettingsButtonAttribute(string name, int xPos, int yPos, string dict) => (Name, Position, Selectable) = (name, (xPos, yPos), Global[dict]);

        public static Dictionary<string, Dictionary<string, object>> Global = new()
        {
            {"Models", new()
            {
                {"ChatGPT 3.5", ModelType.ChatGPT3_5 },
                {"Custom ChatGPT", ModelType.CustomChatGPT }
                //{"ChatGPT 3.5", new Models.ModelSelector(){ Type = Models.ModelSelector.ModelType.ChatGPT3_5, ModelName="gpt-3.5-turbo"}}
            }
            }

        };

    }
    [AttributeUsage(AttributeTargets.Property)]
    class RequireFieldAttribute : Attribute
    {
        public string FieldName { get; set; }
        public object Value { get; set; }
        public RequireFieldAttribute(string fieldName, object value)
        {
            FieldName = fieldName;
            Value = value;
        }
    }

    public class BotSettingsList
    {
        const string fileName = "data.json";
        Dictionary<long, BotSettings> BotSettings { get; set; } = new();
        public BotSettings this[long peerId] => BotSettings.GetValueOrDefault(peerId, null) ?? BotSettings.addRet(peerId, new BotSettings());
        public void Save() => File.WriteAllText(fileName, JsonSerializer.Serialize(BotSettings));
        public static BotSettingsList Read()
        {
            Dictionary<long, BotSettings>? bs = new();
            if (File.Exists(fileName))
                bs = JsonSerializer.Deserialize<Dictionary<long, BotSettings>>(File.ReadAllText(fileName));
            return new BotSettingsList() { BotSettings = bs ?? new() };
        }
    }
}
