using System;
using System.Linq;

namespace Phemedrone.Tools.Xor
{
    public class Encryption
    {
        public static string Encrypt(string input, string key)
        {
            var encrypted = Array.Empty<byte>();

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                var keyChar = key[i % key.Length];

                var encryptedByte = (byte)(c ^ keyChar);
                encrypted = encrypted.Append(encryptedByte).ToArray();
            }

            return Convert.ToBase64String(encrypted);
        }
    }
}