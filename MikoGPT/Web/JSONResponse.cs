using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VkNet.Utils;

namespace MikoGPT.Web
{
    public class JSONResponseObject
    {
        public JSONResponseObject() { }
        public JSONResponseObject(BaseResponseExeptions baseResponseExeptions) => 
            Status = Enum.GetName(baseResponseExeptions) ?? nameof(BaseResponseExeptions.InternalExeption);
        public enum BaseResponseExeptions
        {
            InternalExeption,
            BadObjectExeption,
            Success,
            KeyValidationFiled
        }
        public string Status { get; set; } = nameof(BaseResponseExeptions.InternalExeption);
        public override string ToString() => 
            JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy() { ProcessDictionaryKeys = true },
                }
            });
        public byte[] ToBytes() =>
            Encoding.UTF8.GetBytes(ToString());
    }
    internal class JSONResponse<TInput> : IResponsable 
        where TInput : class
    {
        protected static void AllowCorps(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                context.Response.AddHeader("Access-Control-Max-Age", "1728000");
            }
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
        }
        protected ApiKeysManager.KeyInfo? currentKey;
        public virtual bool ValidateKey(ApiKeysManager.KeyInfo? key)
        {
            return true;
        }
        public class Wrapper<T>
        {
            public T? Data { get; set; }
            public string? Key { get; set; }
            public int? Version { get; set; }
        }
        public void GetResponse(HttpListenerContext context)
        {
            try
            {
                context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                
                using var outputStream = context.Response.OutputStream;
                string input = new StreamReader(context.Request.InputStream).ReadToEnd();
                Wrapper<TInput> inputObj;
                try
                {
                    inputObj = JsonConvert.DeserializeObject<Wrapper<TInput>>(input) ?? throw new JsonException();
                } catch (JsonException)
                {
                    outputStream.Write(new JSONResponseObject(JSONResponseObject.BaseResponseExeptions.BadObjectExeption).ToBytes());
                    outputStream.Flush();
                    return;
                }
                try
                {   
                    currentKey = null;
                    try
                    {
                        currentKey = ApiKeysManager.Instance?[inputObj.Key ?? throw new()];
                    } catch(Exception) { }
                    if (!ValidateKey(currentKey))
                    {
                        outputStream.Write(new JSONResponseObject(JSONResponseObject.BaseResponseExeptions.KeyValidationFiled).ToBytes());
                        outputStream.Flush();
                        return;
                    }
                    if (inputObj.Data == null) return;
                    outputStream.Write(GetObjectResponse(inputObj.Data).ToBytes());
                    outputStream.Flush();
                    return;
                } catch (Exception e)
                {
                    Logger.Instance?.Log("web runtime", $"back;\n{e}", Logger.WarningLevel.Error);
                    outputStream.Write(new JSONResponseObject().ToBytes());
                    outputStream.Flush();
                    return;
                }
            } catch(Exception e)
            {
                Logger.Instance?.Log("web runtime", e.ToString(), Logger.WarningLevel.Error);
            }
        }
        public virtual JSONResponseObject GetObjectResponse(TInput input)
        {
            return new JSONResponseObject();
        }
    }
}
