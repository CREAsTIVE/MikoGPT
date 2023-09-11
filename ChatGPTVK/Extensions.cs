using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model.Attachments;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Model;
using VkNet;

using sImage = System.Drawing.Image;
using vkImage = VkNet.Model.Image;
using System.Drawing.Imaging;

namespace ChatGPTVK
{
    #region Extensions

    static class Extensions
    {
        public delegate OUT ConvertValue<IN, OUT>(IN Value);
        public static IEnumerable<OUT> ForEachCopy<IN, OUT>(this IEnumerable<IN> array, ConvertValue<IN, OUT> convertValue)
        {
            IEnumerable<OUT> Enumerator()
            {
                foreach (var i in array)
                {
                    yield return convertValue(i);
                }
            }
            return Enumerator();
        }
        public static Random random = new Random();
        public static long SendMessage(this VkApi api, string msg, long peerId, long? replyId = null, MessageKeyboard? messageKeyboard = null, Poll? poll = null, Photo? photo = null)
        {
            MessageForward? forward = replyId is null ? null : new MessageForward()
            { IsReply = true, ConversationMessageIds = new List<long>() { (long)replyId }, PeerId = peerId };
            MessagesSendParams messagesSendParams = new()
            {
                Message = msg,
                PeerId = peerId,
                Forward = forward,
                RandomId = random.Next(1000)
            };
            if (messageKeyboard != null)
                messagesSendParams.Keyboard = messageKeyboard;

            var media = new LinkedList<MediaAttachment> { };

            if (poll != null)
                media.AddLast(poll);
            if (photo != null)
                media.AddLast(photo);

            if (media.Count > 0)
                messagesSendParams.Attachments = media;

            return api.Messages.Send(messagesSendParams);
        }
        public static long Reply(this VkApi api, Message message, string text, MessageKeyboard? messageKeyboard = null, Poll? poll = null, Photo? photo = null)
        {
            return api.SendMessage(text, (long)message.PeerId, message.ConversationMessageId, messageKeyboard, poll, photo: photo);
        }
        public static void ReplyRem(this VkApi api, Message message, string text, MessageKeyboard? messageKeyboard = null)
        {
            api.Reply(message, text, messageKeyboard);
            try
            {
                api.Messages.Delete(new ulong[] { (ulong)message.ConversationMessageId }, (ulong)message.PeerId);
            }
            catch (Exception) { }
        }
        public enum MessageEventTypes
        {
            RequestAccessToSetChatKey
        }
        public static bool isAdmin(this VkApi api, long peerId, long userId)
        {
            var members = api.Messages.GetConversationMembers(peerId: peerId);
            foreach (var i in members.Items)
            {
                if (i.MemberId == userId)
                    return i.IsAdmin;
            }
            return false;

        }
        public static bool TryGetIndexOf<T>(this List<T> array, T value, out int result)
        {
            result = -1;
            var f = array.IndexOf(value);
            if (f < 0)
                return false;
            result = f;
            return true;
        }
        public static Dictionary<string, string> ReplaceDictionary = new()
    {
        {"😆", "xD"}, {"»", ">>"}, {"«", "<<"}, {"😊", ":-)"}
    };
        public static string UnformatVK(this string str)
        {
            string newString = str;
            foreach (var i in ReplaceDictionary)
                newString = newString.Replace(i.Key, i.Value);
            return newString;
        }
        public static T2 addRet<T, T2>(this Dictionary<T, T2> collection, T key, T2 obj) where T : notnull
        {
            collection.Add(key, obj);
            return obj;
        }
        public static async Task<string> loadImageToServer(string link, sImage image, string name, ImageFormat format = null)
        {
            HttpClient httpClient = new();
            if (format == null)
                format = ImageFormat.Jpeg;
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, format);
            byte[] fileContents = memoryStream.ToArray();
            var form = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(fileContents);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(imageContent, "file", name);
            HttpResponseMessage resp = await httpClient.PostAsync(link, form);
            var res = Encoding.Default.GetString(await resp.Content.ReadAsByteArrayAsync());
            image.Dispose();
            memoryStream.Close();
            form.Dispose();
            imageContent.Dispose();
            return res;
        }
        public static async Task<string> loadImageToServer(string link, string imagePath, ImageFormat format = null)
        {
            return await loadImageToServer(link, sImage.FromFile(imagePath), imagePath, format);
        }
        
    }
    #endregion
}
