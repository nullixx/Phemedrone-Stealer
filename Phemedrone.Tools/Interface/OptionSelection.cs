using System;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Interface
{
    public class OptionSelection<T> : IWindow<T>
    {
        private readonly OptionSelectionSettings<T> _settings;
        
        public OptionSelection(OptionSelectionSettings<T> settings)
        {
            _settings = settings;
        }
        
        public override T Draw()
        {
            Console.Clear();
            
            var normalizedTitle = GetNormalizedText(_settings.Title, Width);
            var normalizedDescription = GetNormalizedText($"(i) {_settings.Description}", Width);

            InitCoordinates(1 + normalizedTitle.Split('\n').Length +
                            normalizedDescription.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length +
                            _settings.Options.Count);

            PrintLine(normalizedTitle, ConsoleColor.White);
            
            if (_settings.Description.Length > 0)
            {
                MoveNextCoordinate();
                PrintLine(normalizedDescription, ConsoleColor.DarkGray);
            }
            
            MoveNextCoordinate();
            Console.WriteLine();
            
            ConsoleKey key;
            var selectedOption = 0;

            var y = Console.CursorTop;
            do
            {
                Console.SetCursorPosition(CenteredX, y);
                foreach (var option in _settings.Options)
                {
                    DrawLine(option, 50, _settings.Options[selectedOption].Equals(option));
                    MoveNextCoordinate();
                }

                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedOption < 1) break;
                        selectedOption--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (selectedOption >= _settings.Options.Count - 1) break;
                        selectedOption++;
                        break;
                }
            } while (key != ConsoleKey.Enter);

            return _settings.Options[selectedOption];
        }
    }
}