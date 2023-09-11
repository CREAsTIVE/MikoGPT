using Microsoft.Extensions.Logging;
using MikoGPT.Models;
using MikoGPT.VKButtonGUIFieldAttributes;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model.GroupUpdate;

namespace MikoGPT
{
    public class ChatData
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public IDatabase Database { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public long PeerId { get; set; }
        public ChatData(IDatabase database, long peerId) { Database = database; PeerId = peerId; }

        // Model settings
        [VKButtonCondition(nameof(Menu), MenuType.Settings)]
        [VKButtonCondition(nameof(SelectedLanguageModelType), LanguageModelType.ChatGPT3_5)]
        [VKButton("Sys-приквел", 0, 0)]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Prequel
        {
            get => (SelectedPrefix.model as ChatGPT3_5)?.Prequel ?? throw new("imps");
            set => ((SelectedPrefix.model as ChatGPT3_5) ?? throw new("imps")).Prequel = value;
        }

        public string RemovePrefix(string message)
        {
            foreach (var prefix in Prefixes)
            {
                if (message.StartsWith(prefix.prefix))
                {
                    return message.Remove(0, prefix.prefix.Length);
                }
            }
            return message;
        }
        int mod(int a, int n)
        {
            return ((a % n) + n) % n;
        }
        public string GetText() => Menu switch
        {
            MenuType.Settings => $"Настройки для \"{SelectedPrefix.prefix}\": {SelectedPrefix.model.GetModelName()}: ",
            MenuType.Prefixes => "Префиксы:\n" + string.Join(
                "\n",
                Prefixes.ForEachCopy((v) => (v.prefix == Prefixes[SelectedPrefixIndex].prefix ? "->" : "--") + $"|\"{v.prefix}\": {v.model.GetModelName()}")
                ),
            _ => ""
        };

        [System.Text.Json.Serialization.JsonIgnore]
        LanguageModelType SelectedLanguageModelType { get => SelectedPrefix.model.GetModelType(); set { } }

        //ChatGPT 3.5 params
        /*[System.Text.Json.Serialization.JsonIgnore]
        [VKButtonCondition(nameof(SelectedLanguageModelType), LanguageModelType.ChatGPT3_5)]
        [VKButton("Температура", 0, 0)]
        public string Temperature
        {
            get => "";
            set
            {
                try
                {
                    ((SelectedPrefix.model as ChatGPT3_5) ?? new()).Temperature = float.Parse(value);
                } catch (Exception) { }
            }
        }*/

        // ⬇ ⬆ ➕ ✏ ➖
        /*[VKButton("Параметры", 0, 4, Choosable = false)]
        [VKButtonChoosableVariant("Настройки", MenuType.Settings)]
        [VKButtonChoosableVariant("Перфиксы", MenuType.Prefixes)]*/
        public MenuType Menu { get; set; } = MenuType.Settings;

        [VKButton("Модели", 0, 4, Choosable = false)]
        [VKButtonCondition(nameof(Menu), MenuType.Settings)]
        public bool SwitchToPrefixes(MessageEvent messageEvent) => (Menu = MenuType.Prefixes) == Menu;
        [VKButton("Параметры", 0, 4, Choosable = false)]
        [VKButtonCondition(nameof(Menu), MenuType.Prefixes)]
        public bool SwitchToModels(MessageEvent messageEvent) => (Menu = MenuType.Settings) == Menu;


        public int SelectedPrefixIndex { get; set; } = 0;

        [System.Text.Json.Serialization.JsonIgnore]
        public PrefixModelPair SelectedPrefix { get => Prefixes[SelectedPrefixIndex]; set => Prefixes[SelectedPrefixIndex] = value; }

        public struct PrefixModelPair
        {
            public string prefix { get; set; }
            public LanguageModel model { get; set; }
            public PrefixModelPair(string prefix, LanguageModel model)
            {
                this.prefix = prefix; this.model = model;
            }
        }
        public List<PrefixModelPair> Prefixes { get; set; } = new() {new("miko,", new ChatGPT3_5())};

        [VKButton("⬆", 0, 0)]
        [VKButtonCondition(nameof(Menu), MenuType.Prefixes)]
        public bool MooveUp(MessageEvent messageEvent)
        {
            SelectedPrefixIndex = mod(SelectedPrefixIndex - 1, Prefixes.Count);
            return true;
        }
        [VKButton("⬇", 0, 1)]
        [VKButtonCondition(nameof(Menu), MenuType.Prefixes)]
        public bool MooveDown(MessageEvent messageEvent)
        {
            SelectedPrefixIndex = mod(SelectedPrefixIndex + 1, Prefixes.Count);
            return true;
        }
        [VKButton("➕", 1, 0)]
        [VKButtonCondition(nameof(Menu), MenuType.Prefixes)]
        [System.Text.Json.Serialization.JsonIgnore]
        public string AddElement
        {
            get => "";
            set => Prefixes.Add(new(value, new Models.ChatGPT3_5()));
        }
        [VKButton("✏", 1, 1)]
        [VKButtonCondition(nameof(Menu), MenuType.Prefixes)]
        [VKButtonChoosableVariant("Chat GPT 3.5", LanguageModelType.ChatGPT3_5)]
        [VKButtonChoosableVariant("ImGen", LanguageModelType.DeepAiImage)]
        [System.Text.Json.Serialization.JsonIgnore]
        public LanguageModelType EditElement
        {
            get => Models.LanguageModelType.ChatGPT3_5;
            set => Prefixes[SelectedPrefixIndex] = new(Prefixes[SelectedPrefixIndex].prefix, LanguageModel.GetModel(value));
        }
        [VKButton("➖", 0, 2)]
        [VKButtonCondition(nameof(Menu), MenuType.Prefixes)]
        public bool Remove(MessageEvent messageEvent)
        {
            if (Prefixes.Count <= 1)
                return false;
            Prefixes.RemoveAt(SelectedPrefixIndex);
            SelectedPrefixIndex %= Prefixes.Count;
            return true;
        }


        // prefix: model
        
        public enum MenuType
        {
            Settings,
            Prefixes,
            UserSettings
        }
    }
    public class UserData
    {

    }
    public interface IDatabase
    {
        public static IDatabase? Instance = null;
        public void Load(string path);
        public void Unload();
        public void SaveAllData();

        public ChatData GetChatData(long peerId);
        public void SaveChatData(long peerId, ChatData chatData);

        public UserData GetUserData(long userId);
        public void SaveUserData(long userId, UserData userData);

        public object GetPeerData(long peerId) => peerId switch
        {
            >2000000000 => GetChatData(peerId),
            _ => GetUserData(peerId),
        };
        public string SaveCode(string code);
        public string LoadCode(string uid);

    }
    
}
namespace MikoGPT.Databases
{
    public class JsonDocsDatabase : IDatabase
    {
        public static JsonSerializerOptions soptions = new JsonSerializerOptions
        {
            Converters = { new LanguageModelConverter() },
            WriteIndented = true
        };
        string path = "";
        public void Load(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (!Directory.Exists($"{path}/chats"))
                Directory.CreateDirectory($"{path}/chats");
            if (!Directory.Exists($"{path}/users"))
                Directory.CreateDirectory($"{path}/users");
            if (!Directory.Exists($"{path}/codes"))
                Directory.CreateDirectory($"{path}/codes");
            this.path = path;
        }

        public void SaveAllData() { }
        public void Unload() { }

        public static string GetRandomBase64Uid()
        {
            byte[] randomUid = new byte[16];
            Random.Shared.NextBytes(randomUid);
            return Convert.ToBase64String(randomUid).Replace("/", "_").Replace("=", null).Replace("+", "-");
        }
        public string SaveCode(string code)
        {
            string uid = GetRandomBase64Uid();
            while (Directory.Exists($"{path}/codes/{uid}"))
                uid = GetRandomBase64Uid();
            File.WriteAllText($"{path}/codes/{uid}", code, Encoding.UTF8);
            return uid;
        }
        public string LoadCode(string uid)
        {
            return File.ReadAllText($"{path}/codes/{uid}", Encoding.UTF8);
        }

        public ChatData GetChatData(long peerId)
        {
            string filePath = $"{path}/chats/{peerId}.json";
            
            if (File.Exists(filePath))
                return System.Text.Json.JsonSerializer.Deserialize<ChatData>(File.ReadAllText(filePath), soptions) ?? new ChatData(this, peerId);
            return new ChatData(this, peerId);
        }

        public UserData GetUserData(long userId)
        {
            string filePath = $"{path}/users/{userId}.json";
            if (File.Exists(filePath))
                return System.Text.Json.JsonSerializer.Deserialize<UserData>(File.ReadAllText(filePath)) ?? new UserData();
            return new UserData();
        }

        public void SaveChatData(long peerId, ChatData chatData) // FIXME: пулл активных бесед
        {
            string filePath = $"{path}/chats/{peerId}.json";
            if (!File.Exists(filePath))
                File.Create(filePath).Close();
            File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize(chatData, soptions));
        }

        public void SaveUserData(long userId, UserData userData)
        {
            string filePath = $"{path}/users/{userId}.json";
            if (!File.Exists(filePath))
                File.Create(filePath).Close();
            File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize(userData));
        }
    }
}
