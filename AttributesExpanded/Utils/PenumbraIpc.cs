using Dalamud.Plugin;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using System;
using Penumbra.Api.IpcSubscribers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttributesExpanded.Utils
{
    internal class PenumbraIpc(IDalamudPluginInterface pluginInterface) : IDisposable
    {
        private readonly RedrawAll redrawAll = new(pluginInterface);
        private readonly EventSubscriber<nint, Guid, nint, nint, nint> creatingCharacterBaseEvent =
        CreatingCharacterBase.Subscriber(pluginInterface, Utils.CharInterface.OnCreatingCharacterBase);

        public void Dispose()
        {
            creatingCharacterBaseEvent.Dispose();
        }

        internal void RedrawAll(RedrawType setting)
        {
            try
            {
                redrawAll.Invoke(setting);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
