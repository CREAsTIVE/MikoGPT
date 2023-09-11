using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.GroupUpdate;

namespace MikoGPT
{
    class VKCallbackManager
    {
        VkApi api;

        public delegate void OnMessageNew(MessageNew update);
        public event OnMessageNew? onMessageNew;

        public delegate void OnMessageEvent(MessageEvent update);
        public event OnMessageEvent? onMessageEvent;

        public VKCallbackManager(VkApi api) => this.api = api;

        public void LongPull()
        {
            var lpData = api.Groups.GetLongPollServer(Config.Instance.GroupId);
            BotsLongPollHistoryResponse? lp = null;
            while (true)
            {
                try
                {
                    lp = api.Groups.GetBotsLongPollHistory(new()
                    {
                        Key = lpData.Key,
                        Server = lpData.Server,
                        Ts = lpData.Ts,
                        Wait = 25
                    });
                    lpData.Ts = lp.Ts;
                } catch (LongPollKeyExpiredException)
                {
                    lpData = api.Groups.GetLongPollServer(Config.Instance.GroupId);
                    continue;
                }
                if (lp is null)
                    continue;

                foreach (var update in lp.Updates)
                {
                    Task.Run(() =>
                    {
                        if (update.Instance is MessageNew messageNew)
                            onMessageNew?.Invoke(messageNew);
                        else if (update.Instance is MessageEvent messageEvent)
                            onMessageEvent?.Invoke(messageEvent);
                    });
                }
            }
        }
    }
}
