using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT.Web
{
    internal class FileResponse : IResponsable
    {
        public FileResponse(string fileName)
        {
            this.fileContent = File.ReadAllBytes(fileName);
        }
        byte[] fileContent;
        public void GetResponse(HttpListenerContext context)
        {
            try
            {
                using var output = context.Response.OutputStream;
                output.Write(fileContent);
                output.Flush();
            } catch (Exception ex)
            {
                Logger.Instance?.Log("FileResponse", ex.ToString(), Logger.WarningLevel.Error);
            }
        }
    }
}
