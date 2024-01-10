using System.Collections.Generic;

namespace AetherBox.Debugging;

public class DebugConfig
{
    public string SelectedPage = string.Empty;

    public Dictionary<string, object> SavedValues = new Dictionary<string, object>();
}
