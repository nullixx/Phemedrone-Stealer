using System;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Interface
{
    public class BooleanSelection : IWindow<bool>
    {
        private readonly BooleanSelectionSettings _settings;

        public BooleanSelection(BooleanSelectionSettings settings)
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
                PrintLine(normalizedDescription, ConsoleColor.DarkGray);
            }
            
            Console.WriteLine();
            MoveNextCoordinate();
            
            ConsoleKey key;
            var selectedOption = _settings.DefaultValue ? 1 : 0;
            
            do
            {
                Console.SetCursorPosition(CenteredX, Console.CursorTop);
                
                DrawLine("[no]", 25, selectedOption == 0, false);
                DrawLine("[yes]", 25, selectedOption == 1, false);

                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        if (selectedOption < 1) break;
                        selectedOption--;
                        break;
                    case ConsoleKey.RightArrow:
                        if (selectedOption >= 1) break;
                        selectedOption++;
                        break;
                }
            } while (key != ConsoleKey.Enter);

            return selectedOption == 1;
        }
    }
}