using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.GroupUpdate;

namespace ChatGPTVK
{
    class CallbackManager
    {
        VkApi api;

        public delegate void OnMessageNew(MessageNew messageNew);
        public event OnMessageNew onMessageNew;

        public delegate void OnMessageEvent(MessageEvent messageEvent);
        public event OnMessageEvent onMessageEvent;

        ulong groupId;
        public CallbackManager(VkApi api, ulong groupId)
        {
            this.api = api; this.groupId= groupId;
        }
        BotsLongPollHistoryResponse getHistory(LongPollServerResponse lp) => api.Groups.GetBotsLongPollHistory(new()
        {
            Key = lp.Key,
            Server = lp.Server,
            Ts = lp.Ts,
            Wait = 20
        });
        public void Listen()
        {

            var lp = api.Groups.GetLongPollServer(groupId);
            while (true)
            {
                try
                {
                    BotsLongPollHistoryResponse? lpHistory = null;
                    try
                    {
                        lpHistory = getHistory(lp);
                    }
                    catch (LongPollKeyExpiredException)
                    {
                        lp = api.Groups.GetLongPollServer(groupId);
                        lpHistory = getHistory(lp);
                    }

                    lp.Ts = lpHistory.Ts;
                    if (lpHistory?.Updates is null)
                        continue;

                    foreach (var update in lpHistory.Updates)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                if (update.Instance is MessageNew messageNew)
                                    onMessageNew(messageNew);
                                else if (update.Instance is MessageEvent messageEvent)
                                    onMessageEvent(messageEvent);
                            } catch (Exception e)
                            {
                                Log.error($"Process update error: \n{e}");
                            }

                        });
                    }
                }
                catch (Exception e)
                {
                    Log.error($"CallbackManager error: \n{e}");
                    lp = api.Groups.GetLongPollServer(groupId);
                }
            }
        }
        
    }
}
