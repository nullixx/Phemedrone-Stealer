using System.Collections.Generic;
using System.Linq;
using Phemedrone.Services;

namespace Phemedrone.Protections
{
    public class AntiVM
    {
        public static bool IsVM()
        {
            var virtualGpus = new List<string>()
            {
                "VirtualBox", "VBox", "VMware Virtual", "VMware", "Hyper-V Video"
            };
            
            var gpus = Information.GetGPUs();
            return _ = virtualGpus.Any(x => gpus.Any(y => y.Contains(x)));
            
        }
    }
}