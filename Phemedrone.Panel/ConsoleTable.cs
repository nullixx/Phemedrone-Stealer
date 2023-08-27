using System.Diagnostics;
using System.Net;

namespace Phemedrone.Panel;

public class ConsoleTable
{
    private List<LogEntry> _objectList = new();
    private (int, int) _startPos;
    private (bool,int,int,bool?) filterMode;
    private (int?, bool) selectedRow;
    private static string selectedfile = "";
    
    private static readonly Dictionary<string, (int, Type)> Columns = new()
    {
        {
            "country", (1, typeof(string))
        },
        {
            "ip", (2, typeof(IPAddress))
        },
        {
            "user", (2, typeof(string))
        },
        {
            "hwid", (1, typeof(string))
        },
        {
            "pass:cookies:wallets", (3, typeof(string))
        }
    };
    private Dictionary<string, KeyBind> _keyBinds = new()
    {
        {
            "Move up", new KeyBind
            {
                Key = ConsoleKey.UpArrow
            }
        },
        {
            "Move down", new KeyBind
            {
                Key = ConsoleKey.DownArrow
            }
        },
        {
            "Column filtering", new KeyBind
            {
                Key = ConsoleKey.F,
                Binds = new Dictionary<string, KeyBind>
                {
                    {
                        "Previous column", new KeyBind
                        {
                            Key = ConsoleKey.LeftArrow
                        }
                    },
                    {
                        "Next column", new KeyBind
                        {
                            Key = ConsoleKey.RightArrow,
                        }
                    },
                    {
                        "Change filtering", new KeyBind
                        {
                            Key = ConsoleKey.Enter,
                        }
                    },
                    {
                        "Exit", new KeyBind
                        {
                            Key = ConsoleKey.F
                        }
                    },
                }
            }
        },
        {
            "Select log", new KeyBind
            {
                Key = ConsoleKey.S,
                Binds = new Dictionary<string, KeyBind>
                {
                    {
                        "Open", new KeyBind
                        {
                            Key = ConsoleKey.Enter
                        }
                    },
                    {
                        "Exit", new KeyBind
                        {
                            Key = ConsoleKey.S
                        }
                    },
                }
            }
        }
    };
    
    public ConsoleTable()
    {
        _keyBinds["Column filtering"].OnKeyPress = () =>
        {
            filterMode.Item1 = !filterMode.Item1;
        };
        _keyBinds["Column filtering"].Binds!["Previous column"].OnKeyPress = () =>
        {
            if (filterMode.Item2 > 0)
            {
                filterMode.Item2--;
            } 
        };
        _keyBinds["Column filtering"].Binds!["Next column"].OnKeyPress = () =>
        {
            if (filterMode.Item2 < Columns.Count-1)
            {
                filterMode.Item2++;
            } 
        };
        _keyBinds["Column filtering"].Binds!["Change filtering"].OnKeyPress = () =>
        {
            var prev = filterMode.Item3;
            filterMode.Item3 = filterMode.Item2;
            if (prev != filterMode.Item3)
            {
                filterMode.Item4 = true;
                return;
            }

            filterMode.Item4 = filterMode.Item4 switch
            {
                null => true,
                true => false,
                _ => null
            };
        };

        _keyBinds["Move up"].OnKeyPress = () =>
        {
            selectedRow.Item1 = selectedRow.Item1 switch
            {
                null => 0,
                > 0 => selectedRow.Item1-1,
                _ => selectedRow.Item1
            };
        };
        _keyBinds["Move down"].OnKeyPress = () =>
        {
            if (selectedRow.Item1 == null)
            {
                selectedRow.Item1 = 0;
            }
            else if(selectedRow.Item1 < _objectList.Count-1)
            {
                selectedRow.Item1++;
            }
        };
        _keyBinds["Select log"].OnKeyPress = () =>
        {
            if (selectedRow.Item1 is null) _keyBinds["Select log"].Expanded = false;
            else selectedRow.Item2 = !selectedRow.Item2;
        };
        _keyBinds["Select log"].Binds!["Open"].OnKeyPress = () =>
        {   
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process cmdProcess = new Process { StartInfo = cmdStartInfo };

            cmdProcess.Start();
            
            cmdProcess.StandardInput.WriteLine($"cd {AppDomain.CurrentDomain.BaseDirectory + $"logs\\"}");
            cmdProcess.StandardInput.WriteLine($"start {selectedfile}");

            cmdProcess.StandardInput.WriteLine("exit");
            cmdProcess.WaitForExit();
            cmdProcess.Close();
        };
    }
    public void Draw(bool redraw = false)
    {
        Console.ResetColor();
        if (!redraw)
        {
            _startPos = (Console.CursorLeft, Console.CursorTop);
        }
        else
        {
            Console.CursorLeft = _startPos.Item1;
            Console.CursorTop = _startPos.Item2;

            Console.Write(string.Join("", Enumerable.Repeat(" ", _startPos.Item1 * _startPos.Item2).ToArray()));
            Console.SetCursorPosition(_startPos.Item1, _startPos.Item2);
        }

        var totalWidth = Columns.Sum(x => x.Value.Item1);
        var fieldWidth = Console.WindowWidth / totalWidth;

        IComparable? GetComparingValue(object v)
        {
            if (v is not IPAddress ipAddress) return v as IComparable ?? v.ToString();
            return new ComparableIpAddress(ipAddress.GetAddressBytes());
        }

        var startPos = (Console.CursorLeft, Console.CursorTop);
        for (var i = 0; i < Columns.Count; i++)
        {
            Console.SetCursorPosition(startPos.CursorLeft, startPos.CursorTop);

            if (filterMode.Item1 && filterMode.Item2 == i)
            {
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
            }

            var columnPadding = Columns.ElementAt(i).Value.Item1 * fieldWidth;
            if (Columns.Count - 1 == i)
            {
                columnPadding = Console.WindowWidth - Columns.Sum(x => x.Value.Item1 * fieldWidth) +
                                Columns.ElementAt(i).Value.Item1 * fieldWidth - 1;
                //if (_objectList.Count > Console.WindowHeight - 3) columnPadding--;
            }
            var columnName = Columns.ElementAt(i).Key.PadRight(columnPadding);
            Console.Write(columnName[..^2] +
                          (filterMode.Item3 == i
                              ? filterMode.Item4 switch 
                              { 
                                  null => "  ", 
                                  true => "↑ ", 
                                  _ => "↓ " 
                              }
                              : "  "));

            var colTop = startPos.CursorTop + 1;

            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;

            var source = filterMode.Item4 switch
            {
                true => _objectList.OrderBy(x => GetComparingValue(x.Values[filterMode.Item3])).ToList(),
                false => _objectList.OrderByDescending(x => GetComparingValue(x.Values[filterMode.Item3])).ToList(),
                _ => _objectList
            };
            var sourceCount = source.Count;
            
            int? current = 0;
            if (sourceCount > Console.WindowHeight - 3)
            {
                var pages = source.Count / (Console.WindowHeight - 3);
                current = (selectedRow.Item1 ?? 0) == 0
                    ? 0
                    : selectedRow.Item1 / (Console.WindowHeight - 3);
                
                source = source.Skip((current ?? 0)  * (Console.WindowHeight - 3)).Take(Console.WindowHeight - 3).ToList();

            }

            for (var j = 0; j < source.Count; j++)
            {
                if (selectedRow.Item1 - (Console.WindowHeight - 3) * (current ?? 0) == j && !filterMode.Item1)
                {
                    Console.BackgroundColor = selectedRow.Item2 ? ConsoleColor.Green : ConsoleColor.Blue;
                    //{e.IP}-{e.Username}-Phemedrone-Report.zip
                    if (Console.BackgroundColor == ConsoleColor.Green)
                    {
                        selectedfile = $"{source[j].Values[1]}-{source[j].Values[2]}-Phemedrone-Report.zip";
                    }
                }
                var item = source[j].Values[i];

                Console.SetCursorPosition(startPos.CursorLeft, colTop);
                var formatValue = item.ToString()?.Length > columnPadding
                    ? item.ToString()?[..(columnPadding - 4)] + "..."
                    : item.ToString()!;
                Console.Write(formatValue.PadRight(columnPadding));

                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.White;
                
                colTop++;
            }

            for (var j = source.Count; j < Console.WindowHeight-2; j++)
            {
                Console.SetCursorPosition(startPos.CursorLeft, colTop);
                Console.Write("".PadRight(columnPadding));
                colTop++;
            }
            
            startPos.CursorLeft += columnPadding;

            /*if (Columns.Count - 1 == i && _objectList.Count > Console.WindowHeight - 3)
            {
                var totalPages = (int)Math.Ceiling((double)_objectList.Count / (Console.WindowHeight - 3));
                var maxSliderPositionPerPage = (totalPages - 1) / (Console.WindowHeight - 2);
                
                var currentPage = (selectedRow ?? 0) == 0
                    ? 0
                    : selectedRow / (Console.WindowHeight - 3);
                var sliderPosition = (currentPage ?? 0) * (Console.WindowHeight - 2);
                
                int sliderPositionOnPage = sliderPosition % (Console.WindowHeight - 2);

                // Adjust slider position if exceeding the maximum on last page
                if (sliderPositionOnPage > maxSliderPositionPerPage)
                    sliderPositionOnPage = maxSliderPositionPerPage;
            }*/
        }

        /*if (sourceCount > Console.WindowHeight - 3)
        {
            var pages = sourceCount / (Console.WindowHeight - 3);
            var current = (selectedRow ?? 0) == 0
                ? 0
                : selectedRow / (Console.WindowHeight - 3);

            var coef = (decimal)(Console.WindowHeight - 3) / pages;
            for (var i = startPos.CursorTop; i < Console.WindowHeight - startPos.CursorTop; i++)
            {
                Console.SetCursorPosition(Console.WindowWidth-1, i);
                
                if ((i - startPos.CursorTop) * coef == (current ?? 0) * coef)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                }
                Console.Write("a");
            }
        }*/
        
        Console.SetCursorPosition(0, Console.WindowHeight-1);
        var bindList = _keyBinds.Where(x => x.Value.Expanded)?.FirstOrDefault().Value?.Binds ?? _keyBinds;
        foreach (var bind in bindList)
        {
            Console.Write($"{bind.Key} ");
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write($"({bind.Value.Key})");
            Console.ResetColor();
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.White;
        }
        Console.Write(string.Join("", Enumerable.Repeat(" ", Console.WindowWidth - Console.CursorLeft - 1)));
    }

    public void AddItem(LogEntry obj, bool redraw = true)
    {
        if (obj.Values.Length < Columns.Count) throw new ArgumentException("Row must contain the same number of elements as columns length");
        for (var i = 0; i < obj.Values.Length; i++)
        {
            if (obj.Values[i].GetType() != Columns.ElementAt(i).Value.Item2)
                throw new ArgumentException($"Row value type mismatch. Expected: {Columns.ElementAt(i).Value.Item2}, got {obj.Values[i].GetType()}");
        }
        _objectList.Add(obj);
        _objectList = _objectList.Distinct().ToList();
        if (redraw) Draw(true);
    }

    public void AddItems(IEnumerable<LogEntry> obj)
    {
        foreach (var o in obj)
        {
            AddItem(o, false);
        }
        Draw(true);
    }

    public void StartKeyListener()
    {
        Task.Factory.StartNew(() =>
        {
            KeyValuePair<string, KeyBind> expanded = default;
            while (true)
            {
                var key = Console.ReadKey(true);

                var selectedBinds = _keyBinds.Where(x => x.Value.Expanded)?.FirstOrDefault().Value?.Binds ?? _keyBinds;
                var expandable = selectedBinds.FirstOrDefault(x => x.Value.Key == key.Key && x.Value.Binds is not null);
                if (expandable.Value == null && expanded.Value != null && expanded.Value?.Key == key.Key)
                {
                    expandable = expanded;
                }
                
                if (expandable.Value != null)
                {
                    expanded = expandable;
                    expandable.Value.Expanded = !expandable.Value.Expanded;
                    expandable.Value.OnKeyPress?.Invoke();

                    if (expandable.Value.Expanded == false) expanded = default;
                }
                else
                {
                    var source = expanded.Value?.Binds ?? _keyBinds;
                    foreach (var bind in source)
                    {
                        if (bind.Value.Key == key.Key)
                        {
                            bind.Value.OnKeyPress?.Invoke();
                            break;
                        }
                    }
                }

                Draw(true);
            }
        });
    }
}