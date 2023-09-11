using MikoGPT.apis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VkNet.Model.GroupUpdate;

namespace MikoGPT
{
    public class Config
    {
        public static Config Instance = new Config();
        public static Config LoadFromFile(string path) =>
            JsonSerializer.Deserialize<Config>(File.ReadAllText(path)) ?? throw new FileNotFoundException($"Unable to find config file {path}");


        public string? VKAPIKey { get; set; } = null;

        public ulong GroupId { get; set; }

        public string GroupName { get; set; } = "mikogнpt";

        public string[] OpenAIAPIKeys { get; set; } = new string[0];
        public string DatabasePath { get; set; } = "./database";
        public WebConfig Web { get; set; } = new();

        [JsonIgnore]
        public string GlobalPrefix
        {
            get => $"[id{GroupId}|mikogpt]";
        }
        public static IChatCompletion? MainCompletor;


        static List<ProxyManager.ProxyFetcher>? defaultProxyFetchers;
        public static List<ProxyManager.ProxyFetcher> DefaultProxyFetchers { get => defaultProxyFetchers ?? throw new ArgumentNullException(); set => defaultProxyFetchers = value; }

    }
    public class ConfigParameterException : Exception
    {
        public string? ParameterName;
        public ConfigParameterException() : base() { }
        public ConfigParameterException(string parameterName, string message) : base($"parameter {parameterName} is invalid: {message}") { }
        public ConfigParameterException(string parameterName) : base($"parameter {parameterName} is invalid") { }
    }
    public class WebConfig
    {
        public string[] SupportedLanguages { get; set; } = new string[0];
        public int Port { get; set; } = 0;
        public string Ip { get; set; } = "";
        public string KeysPath { get; set; } = "";
    }
}
