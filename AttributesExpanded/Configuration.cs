using System;
using Dalamud.Configuration;

namespace AttributesExpanded;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public bool stayOn { get; set; } = true;
    public bool enabled { get; set; } = true;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }

}
