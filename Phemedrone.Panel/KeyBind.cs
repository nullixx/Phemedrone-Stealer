namespace Phemedrone.Panel;

public class KeyBind
{
    public ConsoleKey Key;
    public Dictionary<string, KeyBind>? Binds;
    public bool Expanded;
    public Action OnKeyPress;
}