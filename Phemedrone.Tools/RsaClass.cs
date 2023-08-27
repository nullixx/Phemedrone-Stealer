using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Phemedrone.Tools
{
    public class RsaClass
    {
        public static RSAParameters DeserializeKey(string publicKey)
        {
            var ser = new XmlSerializer(typeof(RSAParameters));
            RSAParameters parameters;
            using (var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(publicKey))))
            {
                parameters = (RSAParameters)ser.Deserialize(reader);
            }

            return parameters;
        }
        
        public static byte[] Decrypt(byte[] data, RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);

                // Extract the encrypted symmetric key and encrypted data
                var keySize = rsa.KeySize / 8;
                var encryptedSymmetricKey = new byte[keySize];
                var vector = new byte[16];
                var encryptedData = new byte[data.Length - keySize - 16];
                Buffer.BlockCopy(data, 0, encryptedSymmetricKey, 0, keySize);
                Buffer.BlockCopy(data, keySize, vector, 0, 16);
                Buffer.BlockCopy(data, keySize + 16, encryptedData, 0, encryptedData.Length);

                // Decrypt the symmetric key using RSA decryption
                var symmetricKey = rsa.Decrypt(encryptedSymmetricKey, true);

                // Decrypt the data using the symmetric key
                using (var aes = Aes.Create())
                {
                    aes.Key = symmetricKey;
                    aes.IV = vector;
                    
                    byte[] decryptedData;
                    using (var memoryStream = new MemoryStream())
                    using (var cryptoStream =
                           new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(encryptedData, 0, encryptedData.Length);
                        cryptoStream.FlushFinalBlock();
                        decryptedData = memoryStream.ToArray();
                    }

                    return decryptedData;
                }
            }
        }

        public static (string, string) GenerateKeyPair()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                return (ExportParametersAsString(rsa.ExportParameters(true)),
                    ExportParametersAsString(rsa.ExportParameters(false)));
            }
        }

        private static string ExportParametersAsString(RSAParameters parameters)
        {
            using (var sw = new StringWriter())
            {
                var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
                var serializer = new XmlSerializer(typeof(RSAParameters));
                serializer.Serialize(xmlWriter, parameters);
                return sw.ToString().Replace("encoding=\"utf-16\"", null);
            }
        }
    }
}