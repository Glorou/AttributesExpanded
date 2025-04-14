using AttributesExpanded;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

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
    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;
    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }
}
