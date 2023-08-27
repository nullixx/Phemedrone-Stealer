using System;
using System.Text;

namespace Phemedrone.Tools.Xor
{
    public class Decryption
    {
        private static readonly string Key = "";
        
        public static string Decrypt(string input)
        {
            var plain = Convert.FromBase64String(input);
            var encrypted = new StringBuilder();

            for (var i = 0; i < plain.Length; i++)
            {
                var c = plain[i];
                var keyChar = Key[i % Key.Length];

                var encryptedChar = (char)(c ^ keyChar);
                encrypted.Append(encryptedChar);
            }

            return encrypted.ToString();
        }
    }
}