using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Utils;

namespace MikoGPT
{
    public class VKScript
    {
        string content;
        public static VKScript FromFile(string name)
        {
            return new VKScript(File.ReadAllText($"vkscripts/{name}.js"));
        }
        public VKScript(string content)
        {
            this.content = content;
        }
        public VkResponse Execute(VkApi api, Dictionary<string, object?> args)
        {
            string modifContent = content;
            foreach (var arg in args)
            {
                modifContent = modifContent.Replace($"_{arg.Key}_", arg.Value?.ToString() ?? "null");
            }
            return api.Execute.Execute(modifContent);
        }
    }
}
