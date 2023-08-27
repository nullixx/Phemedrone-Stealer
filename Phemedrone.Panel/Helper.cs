using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Phemedrone.Panel;

public class Helper
{
    public static void CheckDirect()
    {
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
        
        string jsonContent = File.ReadAllText($"config.json");
            
        JObject config = JsonConvert.DeserializeObject<JObject>(jsonContent);

        Program.ip = IPAddress.Parse(config["Panel"]["IpAddress"].ToString());;
        Program.Port = int.Parse(config["Panel"]["Port"].ToString());
    }
}