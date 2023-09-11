using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT
{
    public class ProxyManager
    {
        public class Proxy
        {
            public string proxyUrl;
            public int errorCount = 0;
            public ProxyManager parent;

            public Proxy(string proxyUrl, ProxyManager parent)
            {
                this.proxyUrl = proxyUrl;
                this.parent = parent;
            }
            public delegate void Success(HttpResponseMessage response);
            public delegate void Failure();
            public void Send(HttpRequestMessage request, int timeout, CancellationToken? cancellationToken = null) =>
                Send(request, TimeSpan.FromSeconds(timeout), cancellationToken);
            public HttpResponseMessage Send(HttpRequestMessage request, TimeSpan timeout, CancellationToken? cancellation = null)
            {
                var httpClient = ToClient(timeout);
                if (cancellation is not null)
                    return httpClient.Send(request, (CancellationToken)cancellation);
                else
                    return httpClient.Send(request);
            }
            public void Close(bool success)
            {
                lock (this)
                {
                    if (!success)
                    {
                        errorCount++;
                        if (errorCount >= 3)
                        {
                            lock (parent.proxies)
                            {
                                parent.proxies.Remove(this);
                            }
                            Task.Run(() =>
                            {
                                Logger.Instance?.Log("proxy kill", $"proxy killed, {(DateTime.Now - parent.lastFetch).TotalSeconds} seconds...");
                                if ((DateTime.Now - parent.lastFetch).TotalMinutes > 5 && !parent.isFetching)
                                    parent.fetchProxies();
                            });
                        }
                    }
                    else
                        errorCount = 0;
                }
            }
            public HttpClient ToClient(TimeSpan timeout) => ProxyManager.ToClient(proxyUrl, timeout);
            public HttpClient ToClient(int timeout = 10) => ProxyManager.ToClient(proxyUrl, timeout);
        }

        public ProxyManager(List<ProxyFetcher> proxyFetchers, ProxyTester proxyTester)
        {
            this.proxyTester = proxyTester; this.proxyFetchers = proxyFetchers;
        }

        public delegate IEnumerable<string> ProxyFetcher();
        public delegate bool ProxyTester(Proxy clientWithProxy);

        List<ProxyFetcher> proxyFetchers = new();
        ProxyTester proxyTester = (c) => true;

        List<Proxy> proxies = new();

        public void AddProxyFetcher(ProxyFetcher proxyFetcher) =>
            proxyFetchers.Add(proxyFetcher);
        
        bool testProxy(Proxy proxy) => proxyTester(proxy);
        bool isFetching = false;
        object fetchLocker = new object();
        DateTime lastFetch = DateTime.Now;
        public void fetchProxies(bool start = false)
        {
            if (isFetching)
                return;
            lock (fetchLocker)
            {   
                if (((DateTime.Now-lastFetch).TotalMinutes < 5 || isFetching) && !start)
                    return;

                isFetching = true;
                var newProxies = getProxies();

                lock (proxies)
                {
                    proxies = newProxies.ToList();
                }

                lastFetch = DateTime.Now;
                isFetching = false;
            }
        }
        IEnumerable<Proxy> getProxies()
        {
            Logger.Instance?.Log("proxy runtime", "Start getting proxies...");
            IEnumerable<string> proxiesUrls = proxyFetchers[0]() ?? new string[0];
            foreach (var fetcher in proxyFetchers.Skip(1))
                proxiesUrls = proxiesUrls.Concat(fetcher());
            Logger.Instance?.Log("proxy runtime", $"Fetched {proxiesUrls.Count()} proxy");

            var proxies = proxiesUrls.Select((url) => new Proxy(url, this));

            LinkedList<Proxy> worked = new();
            LinkedList<Task> tasks = new();

            foreach (var proxy in proxies)
                tasks.AddLast(Task.Run(() =>
                {
                    if (testProxy(proxy))
                        worked.AddLast(proxy);
                }));

            Task.WaitAll(tasks.ToArray());

            Logger.Instance?.Log("proxy runtime", $"Worked {worked.Count()} proxy");

            return worked;
        }
        public int ProxyCounter = 0;
        public Proxy GetNextProxy()
        {
            lock (proxies)
            {
                if (proxies.Count == 0)
                    fetchProxies(true);
                ProxyCounter = (ProxyCounter + 1) % proxies.Count();
                return proxies[ProxyCounter];
            }
        }
        public static HttpClient ToClient(string url) => ToClient(url, 60);
        public static HttpClient ToClient(string url, int timeout) => ToClient(url, TimeSpan.FromSeconds(timeout));
        public static HttpClient ToClient(string url, TimeSpan timeout) =>
            new HttpClient(new HttpClientHandler()
            {
                Proxy = new WebProxy()
                {
                    Address = new Uri($"http://{url}"),
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = false
                }
            }, disposeHandler: true
            )
            {
                Timeout = timeout
            };
    }
}
