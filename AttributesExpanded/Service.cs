using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Penumbra.Api;
using AttributesExpanded.Utils;
using Penumbra.Api.Api;


namespace AttributesExpanded
{
    internal class Service
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        internal static IPenumbraApi penumbraApi { get; private set; } = null!;
        internal static Configuration configuration { get; set; } = null!;

    }
}
