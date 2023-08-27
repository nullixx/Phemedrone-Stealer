using System;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Interface
{
    public class Popup : IWindow<bool>
    {
        private readonly DefaultSettings _settings;
        
        public Popup(DefaultSettings settings)
        {
            _settings = settings;
        }
        
        public override bool Draw()
        {
            Console.Clear();
            
            var normalizedTitle = GetNormalizedText(_settings.Title, Width);
            var normalizedDescription = GetNormalizedText($"(i) {_settings.Description}", Width);
            
            InitCoordinates(2 + normalizedTitle.Split('\n').Length +
                            normalizedDescription.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length);
            
            PrintLine(normalizedTitle, ConsoleColor.White);
            
            if (_settings.Description.Length > 0)
            {
                MoveNextCoordinate();
                PrintLine(normalizedDescription, ConsoleColor.DarkGray);
                Console.ResetColor();
            }
            
            Console.WriteLine();
            MoveNextCoordinate();
            
            ConsoleKey key;
            do
            {
                DrawLine("[ok]", 50, true);

                key = Console.ReadKey(true).Key;
            } while (key != ConsoleKey.Enter);

            return true;
        }
    }
}