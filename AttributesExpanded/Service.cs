using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;


namespace AttributesExpanded
{
    internal class Service
    {
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IGameInteropProvider GameInteropProvider { get; set; } = null!;
        internal static Plugin plugin { get; set; } = null!;
        
        

    }
}
