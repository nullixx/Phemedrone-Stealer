using System.Collections.Generic;

namespace Phemedrone.Services.Browsers
{
    public interface IBrowser
    {
        string GetBrowserName(string root, string location);
        List<string> ListProfiles(string rootLocation);
    }
}