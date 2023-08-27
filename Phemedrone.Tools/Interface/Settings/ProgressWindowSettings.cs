namespace Phemedrone.Tools.Interface.Settings
{
    public class ProgressWindowSettings : ISettings
    {
        public decimal Progress { get; set; }
        public string Stage { get; set; }
    }
}