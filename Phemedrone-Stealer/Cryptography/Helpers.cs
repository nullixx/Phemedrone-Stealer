using System.Globalization;

namespace Phemedrone.Cryptography
{
    public class Helpers
    {
        public static byte[] HexToBytes(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                return null;
            }

            var hexAsBytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexAsBytes.Length; i++)
            {
                var byteValue = hexString.Substring(i * 2, 2);
                hexAsBytes[i] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return hexAsBytes;
        }
    }
}