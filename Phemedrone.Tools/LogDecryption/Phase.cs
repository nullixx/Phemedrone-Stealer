using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Phemedrone.Tools.Interface;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.LogDecryption
{
    public class Phase
    {
        public static void Begin()
        {
            var success = 0;
            var total = 0;
            
            var mode = new OptionSelection<string>(new OptionSelectionSettings<string>
            {
                Title = "Decryption mode",
                Description = "Select how many logs would you like to decrypt",
                Options = new List<string>
                {
                    "Single log",
                    "Multiple logs"
                }
            }).Draw();

            var keyFile = new InputSelection<string>(new InputSelectionSettings<string>
            {
                Title = "Enter key file path",
                Description = "Copy and paste a path to private key file.",
                IsRequired = true
            }).Draw().Replace("\"", null);

            if (!File.Exists(keyFile))
            {
                new Popup(new DefaultSettings
                {
                    Title = "Invalid key file path",
                    Description = "Your key file was not found. Try again"
                }).Draw();
                Environment.Exit(-1);
            }

            RSAParameters key;
            try
            {
                var keyData = File.ReadAllBytes(keyFile);
                key = RsaClass.DeserializeKey(Encoding.UTF8.GetString(keyData));
            }
            catch
            {
                new Popup(new DefaultSettings
                {
                    Title = "Could not read key",
                    Description = "Your key file might be corrupted. Make sure you selected proper key file and try again"
                }).Draw();
                return;
            }

            switch (mode)
            {
                case "Single log":
                    var file = new InputSelection<string>(new InputSelectionSettings<string>
                    {
                        Title = "Enter log file path",
                        Description = "Copy and paste a path to your encrypted log",
                        IsRequired = true
                    }).Draw().Replace("\"", null);

                    if (!File.Exists(file))
                    {
                        new Popup(new DefaultSettings
                        {
                            Title = "Invalid file path",
                            Description = "Your file was not found. Try again"
                        }).Draw();
                        Environment.Exit(-1);
                    }

                    var isok = DecryptFile(key, file);
                    success += isok ? 1 : 0;
                    total++;
                    break;
                case "Multiple logs":
                    var folder = new InputSelection<string>(new InputSelectionSettings<string>
                    {
                        Title = "Enter path to a folder with logs",
                        Description = "Copy and paste a path to folder, containing encrypted logs",
                        IsRequired = true
                    }).Draw().Replace("\"", null);

                    if (!Directory.Exists(folder))
                    {
                        new Popup(new DefaultSettings
                        {
                            Title = "Invalid folder path",
                            Description = "Your folder was not found. Try again"
                        }).Draw();
                        Environment.Exit(-1);
                    }

                    var files = Directory.GetFiles(folder, "*.phem");
                    var progress = new ProgressWindow(new ProgressWindowSettings
                    {
                        Title = "Decrypting your logs",
                        Description = "",
                        Progress = 0,
                        Stage = ".."
                    });
                    
                    foreach (var encFile in files)
                    {
                        progress.Update("Decrypting " + encFile.Split('\\').Last(), (decimal)total / files.Length);
                        var result = DecryptFile(key, encFile);
                        success += result ? 1 : 0;
                        total++;
                    }
                    break;
            }
            
            new Popup(new DefaultSettings
            {
                Title = "Operation completed",
                Description = $"Successfully decrypted {success} logs out of {total}"
            }).Draw();
        }

        private static bool DecryptFile(RSAParameters key, string file)
        {
            try
            {
                byte[] data;
                using (var fileStream = new FileStream(file, FileMode.Open))
                {
                    data = new byte[fileStream.Length];
                    fileStream.Read(data, 0, data.Length);
                }
                
                var decryptedBuffer = RsaClass.Decrypt(data, key);

                using (var fileStream = new FileStream(file.Substring(0, file.Length - 5) + ".zip", FileMode.Create))
                {
                    fileStream.Write(decryptedBuffer, 0, decryptedBuffer.Length);
                }
            }
            catch
            {
                return false;
            }

            File.Delete(file);
            return true;
        }
    }
}