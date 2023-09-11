using Microsoft.Extensions.Logging;
using MikoGPT;
using MikoGPT.apis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatGPTVK
{
    class OldProxyManager
    {
        public static Proxy Get() => Instance.GetProxy();
        public class Proxy
        {
            public Proxy(string url) => Url = url;
            public string Url;
            public int ErrorsCount = 0;
            public void Close(bool sucsess)
            {
                lock (Url)
                {
                    if (!sucsess)
                        Instance.Err(this);
                    else
                        ErrorsCount = 0;
                }
            }
        }
        public static OldProxyManager Instance = new OldProxyManager();
        DateTime lastProxyReciveTime = DateTime.Now;
        public List<Proxy> ProxyList = new List<Proxy>();
        public int ProxyCounter;
        public bool ContainsProxy(Proxy other)
        {
            foreach (var proxy in ProxyList)
                if (proxy.Url == other.Url) return true;
            return false;
        }
        public void Err(Proxy proxy)
        {
            lock (proxy.Url)
            {
                Logger.Instance?.Log("proxy runtime", $"Proxy \"{proxy.Url}\" call exception!");
                proxy.ErrorsCount++;
                if (proxy.ErrorsCount > 2)
                {
                    lock (ProxyList)
                    {
                        Logger.Instance?.Log("proxy runtime", $"PROXY \"{proxy.Url}\" HAS BEEN DESTROYED", Logger.WarningLevel.Warning);
                        ProxyList.Remove(proxy);
                        var diff = DateTime.Now - lastProxyReciveTime;
                        if (diff.TotalMinutes > 11 && !isFetching)
                            Task.Run(() => FetchProxies(true)); 
                    }
                }
            }
        }
        static HttpClient HttpClient = new HttpClient();
        static Regex regex = new Regex(@"<textarea class=""form-control"" readonly=""readonly"" rows=""12"" onclick=""select\(this\)"">Free proxies from free-proxy-list\.net\nUpdated at \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC\.\n\n([^<]*)\n<\/textarea>");
        bool testProxy(string url)
        {
            try
            {
                //var result = DeepAiApi.DoCompletionByProxy(new (string, string)[] { ("user", "say test") }, url, 15);
                var httpClient = new HttpClient();
                var result = httpClient.GetAsync("https://reqres.in/api/users").Result.Content.ReadAsStringAsync().Result;
                return true;
            } catch (AggregateException) { 
                return false;
            }
        }
        void FetchProxies(bool lockProxyList)
        {
            Logger.Instance?.Log("proxy runtime", "Fetching proxies...");
            var newProxies = FetchFree1().Concat(FetchFree2()).ToArray();
            Logger.Instance?.Log("proxy runtime", $"Fetched {newProxies.Count()}. Testing...");

            LinkedList<string> workedProxies = new LinkedList<string>();
            LinkedList<Task> workedTasks = new LinkedList<Task>();

            foreach (var proxy in newProxies)
            {
                workedTasks.AddLast(Task.Run(() =>
                {
                    if (testProxy(proxy))
                        workedProxies.AddLast(proxy);
                }));
            }

            Task.WaitAll(workedTasks.ToArray());
            Logger.Instance?.Log("proxy runtime", $"Work {workedProxies.Count} proxies. Added.");

            if (lockProxyList)
                Monitor.Enter(ProxyList);

            foreach (var proxy in workedProxies)
                if (!ContainsProxy(new Proxy(proxy)))
                    ProxyList.Add(new Proxy(proxy));

            if (lockProxyList)
                Monitor.Exit(ProxyList);

            Logger.Instance?.Log("proxy runtime", $"Finalazed. Total {ProxyList.Count} proxies");

            lastProxyReciveTime = DateTime.Now;
            isFetching = false;
        }

        private static IEnumerable<string> FetchFree1()
        {
            Uri url = new Uri("https://free-proxy-list.net/");
            var requestResult = HttpClient.GetAsync(url).Result;
            var requestContent = requestResult.Content.ReadAsStringAsync().Result;
            Match match = regex.Match(requestContent);
            var newProxies = match.Groups[1].Value.Split('\n');
            return newProxies;
        }
        static bool gettedDefault = false;
        static Regex hideMyNameRegex = new(@"<td>(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})<\/td><td>(\d{1,6})<\/td>");
        static IEnumerable<string> FetchFree2()
        {
            Uri url = new Uri("https://hidemyna.me/en/proxy-list/");
            HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            var response = HttpClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result ?? throw new Exception("gog");
            return hideMyNameRegex.Matches(response).Select((r) => $"{r.Groups[1]}:{r.Groups[2]}");
        }

        bool isFetching = false;
        public Proxy GetProxy()
        {
            lock (ProxyList)
            {
                if (ProxyList.Count == 0)
                {
                    FetchProxies(false);
                }
                ProxyCounter = (1 + ProxyCounter) % ProxyList.Count;
                return ProxyList[ProxyCounter];
            }
        }
    }
}
