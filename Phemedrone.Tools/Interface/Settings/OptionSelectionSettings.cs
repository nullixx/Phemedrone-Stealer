using System.Collections.Generic;

namespace Phemedrone.Tools.Interface.Settings
{
    public class OptionSelectionSettings<T> : ISettings
    {
        public List<T> Options { get; set; }
    }
}