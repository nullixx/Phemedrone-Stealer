using System;
using System.Collections.Generic;
using System.Linq;

namespace Phemedrone.Tools.Interface
{
    public abstract class IWindow<T>
    {
        protected const int Width = 50;
        
        protected int CenteredX = 0;
        private int CenteredY = 0;
        
        public abstract T Draw();

        protected void InitCoordinates(int linesCount)
        {
            CenteredX = (Console.WindowWidth - Width) / 2;
            CenteredY = (Console.WindowHeight - linesCount) / 2;
            
            Console.SetCursorPosition(CenteredX, CenteredY);
        }

        protected void MoveNextCoordinate()
        {
            Console.SetCursorPosition(CenteredX, Console.CursorTop);
        }

        protected string GetNormalizedText(string text, int width)
        {
            var normalized = new List<string>();

            if (text.Length <= width) return string.Join("\n", normalized.Append(text));

            for (var i = width - 1; i > 0; i--)
            {
                if (text[i] != ' ') continue;

                normalized.AddRange(new[]
                {
                    text.Substring(0, i),
                    GetNormalizedText(text.Substring(i + 1, text.Length - i - 1), width)
                });
                break;
            }

            return string.Join("\n", normalized);
        }
        
        
        protected static void DrawLine<T1>(T1 text, int width, bool isSelected, bool newLine = true)
        {
            var padding = (width - text.ToString().Length)/2;

            if (isSelected)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            
            Console.Write(string.Join("", Enumerable.Repeat(" ", width-padding-text.ToString().Length)));
            Console.Write(text);
            
            if (padding > 0) Console.Write(string.Join("", Enumerable.Repeat(" ", padding)));
            
            Console.ResetColor();
            if (newLine) Console.Write("\n");
        }

        protected void PrintLine(string text, ConsoleColor color, bool newLine = true)
        {
            var lines = text.Split('\n');
            Console.ForegroundColor = color;
            foreach (var line in lines)
            {
                if (!newLine)
                {
                    Console.Write(line);
                    continue;
                }
                Console.WriteLine(line + string.Join("", Enumerable.Repeat(" ", Width - line.Length)));
                MoveNextCoordinate();
            }
            Console.ResetColor();
        }
    }
}