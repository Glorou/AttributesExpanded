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
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        public static PenumbraIpc penumbraApi { get; set; } = null!;
        internal static Configuration configuration { get; set; } = null!;
        internal static Plugin plugin { get; set; } = null!;
        internal static CharInterface charInterface { get; set; } = null!;

    }
}
