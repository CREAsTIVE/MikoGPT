using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MikoGPT.Web
{
    class CodeResponse : IResponsable
    {
        static string webPage = File.ReadAllText("web-interface/display-code.html");
        public void GetResponse(HttpListenerContext context)
        {
            var response = context.Response;
            try
            {
                var props = context.Request.QueryString;
                var codeUid = props?["code_uid"] ?? throw new ArgumentException();
                var code = IDatabase.Instance?.LoadCode(codeUid);
                code = HttpUtility.HtmlEncode(code);
                var split = code.Split('\n', 2);
                bool containsFirstLine = Config.Instance.Web.SupportedLanguages.Contains(split[0]);
                string responseText = webPage
                    .Replace("_tag_", containsFirstLine ? $"class=\"language-{split[0]}\"" : "")
                    .Replace("_code_", containsFirstLine ? split[1] : code);
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = buffer.Length;
                using Stream output = response.OutputStream;
                output.Write(buffer);
                output.Flush();
                output.Close();
            }
            catch (Exception) { }
        }
    }
}
