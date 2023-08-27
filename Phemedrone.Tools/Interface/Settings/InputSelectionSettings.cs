namespace Phemedrone.Tools.Interface.Settings
{
    public class InputSelectionSettings<T> : ISettings
    {
        public T DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public string Regex { get; set; } = ".*";
    }
}