using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Phemedrone.Extensions
{
    public class LockHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }
        
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
            uint nFiles,
            string[] rgsFilenames,
            uint nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        public static List<Process> GetLockingProcesses(string path)
        {
            var key = Guid.NewGuid().ToString();
            var processes = new List<Process>();

            uint handle;
            var ret = RmStartSession(out handle, 0, key);
            if (ret != 0) return new List<Process>();

            try
            {
                const int errorMoreData = 234;
                uint pnProcInfo = 0,
                    lpdwRebootReasons = 0;

                var resources = new[] { path };

                ret = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (ret != 0) return new List<Process>();

                ret = RmGetList(handle, out var pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (ret == errorMoreData)
                {
                    var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    ret = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (ret == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        for (var i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            catch (ArgumentException)
                            {
                            }
                        }
                    }
                    else return new List<Process>();
                }
                else if (ret != 0)
                    return new List<Process>();
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }
        
        public static Process GetOwnerProcess(int childProcessId)
        {
            Process ownerProcess = null;
            var query = $"SELECT * FROM Win32_Process WHERE ProcessId = {childProcessId}";
            var searcher = new ManagementObjectSearcher("root\\CIMV2", query);

            foreach (var o in searcher.Get())
            {
                var process = (ManagementObject)o;
                var parentId = (uint)process["ParentProcessId"];

                try
                {
                    ownerProcess = Process.GetProcessById((int)parentId);
                }
                catch
                {
                    // ignored
                }
            }

            return ownerProcess;
        }
    }
}