using System;
using System.Linq;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Interface
{
    public class ProgressWindow : IWindow<bool>
    {
        private readonly ProgressWindowSettings _settings;
        private int _lastY;
        
        public ProgressWindow(ProgressWindowSettings settings)
        {
            _settings = settings;
        }
        
        public override bool Draw()
        {
            Console.Clear();
            
            var normalizedTitle = GetNormalizedText(_settings.Title, Width);
            var normalizedDescription = GetNormalizedText($"(i) {_settings.Description}", Width);
            var normalizedStage = GetNormalizedText(_settings.Stage, Width);
            
            InitCoordinates(2 + normalizedTitle.Split('\n').Length +
                            normalizedDescription.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length +
                            normalizedStage.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length);
            
            PrintLine(normalizedTitle, ConsoleColor.White);
            
            if (_settings.Description.Length > 0)
            {
                MoveNextCoordinate();
                PrintLine(normalizedDescription, ConsoleColor.DarkGray);
                Console.ResetColor();
            }
            
            Console.WriteLine();
            MoveNextCoordinate();

            _lastY = Console.CursorTop;
            var completedWidth = (int)Math.Floor(_settings.Progress * Width);
            
            Console.BackgroundColor = ConsoleColor.White;
            Console.Write(string.Join("", Enumerable.Repeat(" ", completedWidth)));
            
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(string.Join("", Enumerable.Repeat(" ", Width - completedWidth)));
            Console.ResetColor();
            
            Console.WriteLine();
            MoveNextCoordinate();
            PrintLine(normalizedStage, ConsoleColor.DarkGray);
            return true;
        }

        public void Update(string stage, decimal progress)
        {
            Console.SetCursorPosition(CenteredX, _lastY);

            var normalizedStage = GetNormalizedText(stage, Width);
            var completedWidth = (int)Math.Floor(progress * Width);
            
            Console.BackgroundColor = ConsoleColor.White;
            Console.Write(string.Join("", Enumerable.Repeat(" ", completedWidth)));
            
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(string.Join("", Enumerable.Repeat(" ", Width - completedWidth)));
            Console.ResetColor();
            
            Console.WriteLine();
            MoveNextCoordinate();
            PrintLine(normalizedStage, ConsoleColor.DarkGray);
        }
    }
}