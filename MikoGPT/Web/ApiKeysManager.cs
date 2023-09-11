using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT.Web
{
    public class ApiKeysManager
    {
        public class KeyInfo
        {
            public KeyInfo(string userName="None")
            {
                byte[] randomUid = new byte[16];
                Random.Shared.NextBytes(randomUid);
                Key = Convert.ToBase64String(randomUid);
                UserName = userName;
            }

            public string Key { get; set; }
            public string UserName { get; set; } = "None";
        }
        public static ApiKeysManager? Instance;

        public List<KeyInfo> KeysList { get; set; } = new();
        [JsonIgnore]
        public string? pathToSave;

        public ApiKeysManager SetPathToSave(string path)
        {
            pathToSave = path;
            return this;
        }
        public KeyInfo? this[string key] => KeysList.Find((k) => k.Key == key);
        public string AddKey(KeyInfo keyInfo)
        {
            KeysList.Add(keyInfo);
            Save();
            return keyInfo.Key;
        }
        void Save() => File.WriteAllText(pathToSave ?? "temp", JsonConvert.SerializeObject(this));
        public bool RemoveKey(string key)
        {
            bool result = KeysList.Remove(this[key] ?? new KeyInfo());
            Save();
            return result;
        }
        public bool RemoveKeyByName(string name)
        {
            bool result = KeysList.Remove(KeysList.Find((k) => k.UserName == name) ?? new KeyInfo());
            Save();
            return result;
        }
        public KeyInfo? FindByName(string name) => KeysList.Find((k) => k.UserName == name);

        public static ApiKeysManager LoadOrCreateFromFile(string path)
        {
            if (File.Exists(path)) return JsonConvert.DeserializeObject<ApiKeysManager>(File.ReadAllText(path))?.SetPathToSave(path) ?? new ApiKeysManager() { pathToSave=path};
            return new ApiKeysManager() { pathToSave = path };
        }
    }
}
