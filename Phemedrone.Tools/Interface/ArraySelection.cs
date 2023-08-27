using System;
using System.Collections;
using System.Linq;
using Phemedrone.Tools.Interface.Settings;

namespace Phemedrone.Tools.Interface
{
    public class ArraySelection<T> : IWindow<T> where T : IList, new()
    {
        private readonly DefaultSettings _settings;
        
        public ArraySelection(DefaultSettings settings)
        {
            _settings = settings;
        }
        
        public override T Draw()
        {
            Console.Clear();
            
            var normalizedTitle = GetNormalizedText(_settings.Title, Width);
            var normalizedDescription = GetNormalizedText($"(i) {_settings.Description}", Width);

            InitCoordinates(5 + normalizedTitle.Split('\n').Length +
                            normalizedDescription.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).Length);
            
            PrintLine(GetNormalizedText(_settings.Title, Width), ConsoleColor.White);
            
            if (_settings.Description.Length > 0)
            {
                MoveNextCoordinate();
                PrintLine(normalizedDescription, ConsoleColor.DarkGray);
            }
            
            var values = new T();
            var isFinished = false;
            
            var y = Console.CursorTop;
            do
            {
                var text = string.Empty;
                var selectedOption = 0;
                ConsoleKey key;
                
                do
                {
                    Console.SetCursorPosition(CenteredX, y);
                
                    MoveNextCoordinate();
                    var selectValue = "Selected values: ";
                    if (values.Count == 0) selectValue += "none";
                    else
                    {
                        var joined = string.Join(", ", values.Cast<string>());
                        selectValue += joined;
                        if (joined.Length > Width - 17)
                        {
                            selectValue = "Selected values: ..." + joined.Substring(joined.Length - (Width - 20), Width - 20);
                        }
                    }
                    PrintLine(selectValue, ConsoleColor.Gray);
                
                    MoveNextCoordinate();
                    PrintLine(string.Join("", Enumerable.Repeat(" ", Width)), ConsoleColor.White);
                    
                    var inputLine = $"{text,-35}[Enter to save]";
                    if (text.Length >= 34)
                    {
                        inputLine = $"...{text.Substring(3 + (text.Length - 34), 34 - 3)} [Enter to save]";
                    }
                    
                    DrawLine(inputLine, inputLine.Length, selectedOption == 0);
                    MoveNextCoordinate();
                    
                    DrawLine("[reset]", inputLine.Length, selectedOption == 1);
                    MoveNextCoordinate();
                    
                    DrawLine("[save]", inputLine.Length, selectedOption == 2);
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
                            if (selectedOption >= 2) break;
                            selectedOption++;
                            break;
                        case ConsoleKey.Backspace:
                            if (text.Length < 1) break;
                            text = text.Substring(0, text.Length - 1);
                            break;
                        case ConsoleKey.Enter:
                            switch (selectedOption)
                            {
                                case 1:
                                    text = string.Empty;
                                    values.Clear();
                                    selectedOption = 0;
                                    key = default;
                                    break;
                                case 2:
                                    isFinished = true;
                                    break;
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
                if (text.Length > 1) values.Add(text);
            } while (!isFinished);

            return values;
        }
    }
}