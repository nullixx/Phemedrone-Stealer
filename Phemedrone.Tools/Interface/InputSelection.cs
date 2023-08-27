using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Interface
{
    public class InputSelection<T> : IWindow<T>
    {
        private readonly InputSelectionSettings<T> _settings;

        public InputSelection(InputSelectionSettings<T> settings)
        {
            _settings = settings;
        }

        public override T Draw()
        {
            Console.Clear();
            
            var normalizedTitle = GetNormalizedText(_settings.Title, Width);
            var normalizedDescription = GetNormalizedText($"(i) {_settings.Description}", Width);
            var normalizedExtra = GetNormalizedText(_settings.IsRequired
                ? "* This value is required"
                : $"Leave blank for {(_settings.DefaultValue.ToString().Length > 0 ? _settings.DefaultValue.ToString() : "none")}", Width);

            InitCoordinates(3 + normalizedTitle.Split('\n').Length +
                            normalizedDescription.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length +
                            normalizedExtra.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length);
            
            PrintLine(GetNormalizedText(_settings.Title, Width), ConsoleColor.White);
            
            if (_settings.Description.Length > 0)
            {
                MoveNextCoordinate();
                PrintLine(normalizedDescription, ConsoleColor.DarkGray);
            }
                
            if (!EqualityComparer<T>.Default.Equals(_settings.DefaultValue, default))
            {
                MoveNextCoordinate();
                PrintLine("Leave blank for ", ConsoleColor.DarkGray, false);
                PrintLine(normalizedExtra.Replace("Leave blank for ", null), ConsoleColor.Blue, false);
            } else if (_settings.IsRequired)
            {
                MoveNextCoordinate();
                PrintLine("* ", ConsoleColor.Red, false);
                PrintLine(normalizedExtra.Replace("* ", null), ConsoleColor.Gray, false);
            }
            
            MoveNextCoordinate();
            Console.WriteLine();
            Console.WriteLine();
            
            var text = string.Empty;
            var selectedOption = 0;
            ConsoleKey key;

            var y = Console.CursorTop;
            do
            {
                Console.SetCursorPosition(CenteredX, y);
                
                var inputLine = $"{text,-35}[Enter to save]";
                if (text.Length >= 34)
                {
                    inputLine = $"...{text.Substring(3 + (text.Length - 34), 34 - 3)} [Enter to save]";
                }
                
                DrawLine(inputLine, inputLine.Length, selectedOption == 0);
                MoveNextCoordinate();
                
                DrawLine("[clear]", inputLine.Length, selectedOption == 1);
                MoveNextCoordinate();
                
                var consoleKey = Console.ReadKey(true);
                key = consoleKey.Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedOption < 1) break;
                        selectedOption--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (selectedOption >= 1) break;
                        selectedOption++;
                        break;
                    case ConsoleKey.Backspace:
                        if (text.Length < 1) break;
                        text = text.Substring(0, text.Length - 1);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedOption == 1)
                        {
                            text = string.Empty;
                            selectedOption = 0;
                            key = default;
                        } else if (selectedOption == 0)
                        {
                            if (!Regex.IsMatch(text, _settings.Regex))
                            {
                                key = default;
                            }
                        }
                        break;
                    default:
                        if (selectedOption == 0 && !char.IsControl(consoleKey.KeyChar))
                        {
                            text += consoleKey.KeyChar;
                        }
                        break;
                }
                
            } while (key != ConsoleKey.Enter);

            if (text == string.Empty) return _settings.DefaultValue;
            
            switch (typeof(T))
            {
                case Type a when a == typeof(int):
                {
                    return (T)(object)Convert.ToInt32(text);
                }
            }
            return (T)(object)text;
        }
    }
}