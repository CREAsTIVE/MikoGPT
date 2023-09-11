using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT.Web
{
    internal class ApiChatResponse : JSONResponse<string[][]>
    {
        public class ApiResponseObject : JSONResponseObject
        {
            public string? Output { get; set; } = null;
            public ApiResponseObject(string? output)
            {
                Output = output;
                Status = nameof(BaseResponseExeptions.BadObjectExeption);
                if (output is not null)
                    Status = nameof(BaseResponseExeptions.Success);
            }
            public ApiResponseObject() { }
        }
        public override bool ValidateKey(ApiKeysManager.KeyInfo? key) => key is not null;

        public override JSONResponseObject GetObjectResponse(string[][] input)
        {
            string apiRequest;
            try
            {
                apiRequest = Config.MainCompletor?.DirectChatCompletion(
                    input.Select((a) => (a[0], a[1]))) ?? throw new();
                Logger.Instance?.Log("api", $"Responsed \nkey:{currentKey?.UserName}\non: {string.Join("\n", input.Select((e) => $"{e[0]}: {e[1]}"))}\nby: {apiRequest}", Logger.WarningLevel.Log);
            } catch (IndexOutOfRangeException)
            {
                return new ApiResponseObject(null);
            }
            return new ApiResponseObject(apiRequest);
        }
    }
}
