using System.Net;
using System.Text;

namespace MikoGPT.Web
{
    public interface IResponsable
    {
        public void GetResponse(HttpListenerContext context);
    }
    public class WebInterface
    {
        
        HttpListener listener;
        public static WebInterface? Instance;
        public WebInterface()
        {
            listener = new HttpListener();
        }
        Dictionary<string, Dictionary<string, IResponsable>> responses = new();
        public void Register(string hostName, string path, IResponsable responsable)
        {
            listener.Prefixes.Add($"http://+:{Config.Instance.Web.Port}{path}/");
            if (!responses.ContainsKey(hostName))
                responses.Add(hostName, new());
            responses[hostName].Add(path, responsable);
            Logger.Instance?.Log("web init", $"registered {hostName}{path}");
        }
        public void RegisterWebPage(string hostName, string webPath, string serverPath)
        {
            string[] files = Directory.GetFiles(serverPath);
            foreach (var file in files)
            {
                Register(hostName, webPath + "/" + Path.GetFileName(file), new FileResponse(file));
            }
            Register(hostName, webPath, new FileResponse($"{serverPath}/index.html"));
        }
        public void Listen()
        {
            listener.Start();
            Task.Run(startListen);
        }
        void startListen()
        {
            while (true)
            {
                var context = listener.GetContext();
                Task.Run(() =>
                {
                    try
                    {
                        Logger.Instance?.Log("web runtime", $"New connection from {context.Request.RemoteEndPoint}");

                        var hostName = context.Request.UserHostName.Split(":", 2)[0];
                        var path = context.Request.Url?.AbsolutePath ?? "";
                        if (responses.TryGetValue(hostName, out var paths) && paths.TryGetValue(path, out var obj))
                            obj.GetResponse(context);
                        else
                            context.Response.OutputStream.Write(Encoding.UTF8.GetBytes("Ошибка!"));
                    } catch (Exception ex)
                    {
                        Logger.Instance?.Log("new connection", ex);
                    }
                });
            }
        }
        ~WebInterface()
        {
            listener.Stop();
        }
    }
}
