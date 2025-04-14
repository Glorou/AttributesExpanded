using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Memory;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using static Lumina.Models.Models.Model;
using System.Globalization;
using System;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Penumbra.Api;
using Penumbra.Api.Api;
using AttributesExpanded.Utils;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using Penumbra.Api.IpcSubscribers.Legacy;
using RedrawAll = Penumbra.Api.IpcSubscribers.RedrawAll;
using CreatingCharacterBase = Penumbra.Api.IpcSubscribers.CreatingCharacterBase;

namespace AttributesExpanded;

/* Goals of this plugin:
 * Allow mod authors to make custom atributes and shapekeys
 * Eventually allow more than binary options
 * Set C+ presets for particular mods automatically
 * https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Graphics/Render/Model.cs has [FieldOffset(0xC8)] public uint EnabledShapeKeyIndexMask;
 * https://github.com/WorkingRobot/Meddle/blob/main/Meddle/Meddle.Plugin/UI/CharacterTab.cs#L114-L119
 */
public unsafe class Plugin : IDalamudPlugin
{

    private const string CommandName = "/atrex";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Attributes Expanded");
  

    public Plugin(IDalamudPluginInterface pluginInterface)
    {

        Service.PluginInterface = pluginInterface;
        Service.configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _ = pluginInterface.Create<Service>();
        Service.plugin = this;
        Service.penumbraApi = new PenumbraIpc(pluginInterface);
        _ = pluginInterface.Create<Utils.CharInterface>();
        // you might normally want to embed resources and load them from the manifest stream






        Service.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
        });


        //var models = Service.penumbraApi.ResourceTree.GetPlayerResourcesOfType(Penumbra.Api.Enums.ResourceType.Mdl, false);
    // Add a simple message to the log with level set to information
    // Use /xllog to open the log window in-game
    // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===



    Service.Log.Information($"===A cool log message from {Service.PluginInterface.Manifest.Name}===");
        

        //var modelMetas = new List<ModelMeta>();
        //foreach(var modelPtr in human->CharacterBase.ModelsSpan)
        //{
        //    var model = modelPtr.Value;
        //    if (model == null)
        //        continue;
        //    if (model->ModelResourceHandle == null)
        //        continue;
        //    var shapes = model->ModelResourceHandle->Shapes.ToDictionary(kv => MemoryHelper.ReadStringNullTerminated((nint)kv.Item1.Value), kv => kv.Item2);
        //    var attributes = model->ModelResourceHandle->Attributes.ToDictionary(kv => MemoryHelper.ReadStringNullTerminated((nint)kv.Item1.Value), kv => kv.Item2);
        //    modelMetas.Add(new()
        //    {
        //        ModelPath = model->ModelResourceHandle->ResourceHandle.FileName.ToString(),
        //        EnabledShapes = shapes.Where(kv => ((1 << kv.Value) & model->EnabledShapeKeyIndexMask) != 0).Select(kv => kv.Key).ToArray(),
        //        EnabledAttributes = attributes.Where(kv => ((1 << kv.Value) & model->EnabledAttributeIndexMask) != 0).Select(kv => kv.Key).ToArray(),
        //        ShapesMask = model->EnabledShapeKeyIndexMask,
        //        AttributesMask = model->EnabledAttributeIndexMask,
        //    });
        //}
    }

 /*   public unsafe Human* GetLocalPlayer()
    {
        var localPlayer = PluginService.ClientState.LocalPlayer;
        if (localPlayer == null) { return null; }   
        var localPlayerAddress = localPlayer.Address;
        var localGO = (GameObject*)localPlayerAddress;
        var localCB = (CharacterBase*)localGO->DrawObject;
        var localHuman = (Human*)localCB;
        return localHuman;
    }
    private static float? CheckModelSlot(Human* human, ModelSlot slot)
    {
        var modelArray = human->CharacterBase.Models;
        if (modelArray == null) return null;
        var feetModel = modelArray[(byte)slot];
        if (feetModel == null) return null;
        var modelResource = feetModel->ModelResourceHandle;
        if (modelResource == null) return null;

        foreach (var attr in modelResource->Attributes)
        {
            var str = MemoryHelper.ReadStringNullTerminated(new nint(attr.Item1.Value));
            if (str.StartsWith("heels_offset=", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(str[13..].Replace(',', '.'), CultureInfo.InvariantCulture, out var offsetAttr)) return offsetAttr * human->CharacterBase.DrawObject.Object.Scale.Y;
            }
            else if (str.StartsWith("heels_offset_", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = str[13..].Replace("n_", "-").Replace('a', '0').Replace('b', '1').Replace('c', '2').Replace('d', '3').Replace('e', '4').Replace('f', '5').Replace('g', '6').Replace('h', '7').Replace('i', '8').Replace('j', '9').Replace('_', '.');

                if (float.TryParse(valueStr, CultureInfo.InvariantCulture, out var value)) return value * human->CharacterBase.DrawObject.Object.Scale.Y;
            }
        }

        return null;
    } */
    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        Service.Commands.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui

    }





   
}
