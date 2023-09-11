using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTVK
{
    public class VKScript
    {
        string script;
        public VKScript(string script) =>
            this.script = script;

        public static VKScript FromPath(string path) =>
            new VKScript(File.ReadAllText(path));

        public string Call(Dictionary<string, object?> args)
        {
            var newCode = applyParams(args);
            return Global.Instance.api.Execute.Execute(newCode).RawJson;
        }

        string applyParams(Dictionary<string, object> param)
        {
            var updatedScript = script;
            foreach (var p in param)
                updatedScript = withParam(updatedScript, p.Key, p.Value?.ToString()??"null");
            return updatedScript;
        }
        static string withParam(string script, string argName, string value) => 
            script.Replace($"$${argName}$$", fixString(value)).Replace($"${argName}$", value);

        static string fixString(string str) => 
            str.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
