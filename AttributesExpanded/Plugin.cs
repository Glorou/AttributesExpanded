using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using InteropGenerator.Runtime;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

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
    private uint nextUpdateIndex = 0;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {

        Service.PluginInterface = pluginInterface;
        Service.configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _ = pluginInterface.Create<Service>();
        Service.plugin = this;

        //_ = pluginInterface.Create<Utils.CharInterface>();
        //_ = pluginInterface.Create<Utils.TextureInterface>();
        // you might normally want to embed resources and load them from the manifest stream

    




        Service.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
        });






        Service.Log.Information($"===A cool log message from {Service.PluginInterface.Manifest.Name}===");

        
        var hook = new MySiggedHook();

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

    public unsafe class MySiggedHook : IDisposable
    {
        // This method isn't in CS (in theory), so we need to declare our own delegate.
        private delegate void SetupTopModelAttributes(Human* self, byte* data);

        private delegate void SetupLegModelAttributes(Human* self, byte* data);

        [Signature("E8 ?? ?? ?? ?? 48 8B 87 ?? ?? ?? ?? 44 0F B6 7C 24", DetourName = nameof(DetourTopModelAttributes))]
        private Hook<SetupTopModelAttributes>? _topUpdateHook;

        [Signature(
            "E8 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B CF E8 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B 87",
            DetourName = nameof(DetourLegModelAttributes))]
        private Hook<SetupLegModelAttributes>? _legUpdateHook;

        private Dictionary<String, short> tempTopShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempBottomShapes = new Dictionary<String, short>();

        public MySiggedHook()
        {
            Service.GameInteropProvider.InitializeFromAttributes(this);

            // Nullable because this might not have been initialized from IFA above, e.g. the sig was invalid.
            _topUpdateHook?.Enable();
            _legUpdateHook?.Enable();
        }

        public void Dispose()
        {
            _topUpdateHook?.Dispose();
            _legUpdateHook?.Dispose();
        }

        private void DetourTopModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Top Detour ");
            _topUpdateHook!.Original(self, data);
            self->ModelsSpan[1].Value->EnabledShapeKeyIndexMask = 1;
  /*          try
            {

                if (self->ModelsSpan[1] != null)
                {
                    

                    foreach (var shape in self->ModelsSpan[1].Value->ModelResourceHandle->Shapes)
                    {
                        if (shape.Item1.AsSpan().StartsWith("shpx_"u8))
                        {
                            tempTopShapes.Add(shape.Item1.ToString(), shape.Item2);
                            break;
                        }
                        
                    }
                    self->ModelsSpan[1].Value->EnabledShapeKeyIndexMask = 0;
                }
                
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "An error occured when handling a macro save event.");
            }*/


        }

        private void DetourLegModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Leg Detour");
            _legUpdateHook!.Original(self, data);
            self->ModelsSpan[3].Value->EnabledShapeKeyIndexMask = 1;
           /* try
            {
                if (self->ModelsSpan[3] != null)
                {

                    foreach (var shape in self->ModelsSpan[3].Value->ModelResourceHandle->Shapes)
                    {
                        if (shape.Item1.AsSpan().StartsWith("shpx_"u8))
                        {
                            tempBottomShapes.Add(shape.Item1.ToString(), shape.Item2);

                        }
                    }
                    
                    
                    //Will actually do some logical sorting through here but for now compare index 0 to index 0
                    if (tempTopShapes.Count > 0 && tempBottomShapes.Count > 0)
                    {
                        foreach (var shape in tempTopShapes.Keys)
                        {
                            if (tempBottomShapes.Keys.Contains(shape))
                            {
                                self->ModelsSpan[1].Value->EnabledShapeKeyIndexMask =
                                    (uint)tempTopShapes[shape];
                                self->ModelsSpan[4].Value->EnabledShapeKeyIndexMask =
                                    (uint)tempBottomShapes[shape];
                            }
                        }
                    }
                    

                    tempBottomShapes.Clear();
                    tempTopShapes.Clear();
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "An error occured when handling a macro save event.");
            }
        }*/
    }

/*
    public async Task UpdateObjectbyIndex(uint index)
    {
        foreach (var item in Service.ObjectTable) //Game objects
        {
            if (item.IsValid() && (item.ObjectKind == ObjectKind.Player || item.ObjectKind == ObjectKind.BattleNpc || item.ObjectKind == ObjectKind.EventNpc || item.ObjectKind == ObjectKind.Retainer)) {

                var atrDict = new Dictionary<uint, String>();
                var shpDict = new Dictionary<uint, ValueTuple<short, String>>();
                var gameObj = (GameObject*)item.Address;
                var draw = (Human*)gameObj->DrawObject;
                if (draw->ModelsSpan == null) return;
                var models = draw->ModelsSpan;

                foreach (var model in models)  //collect attributes and keys to check for matches

                {

                    if (model == null) continue;
                    //if (model.Value->ModelResourceHandle->Shapes[model.Value->EnabledShapeKeyIndexMask]) continue;
                    if(model.Value->ModelResourceHandle->Attributes.Count != 0)
                    {
                        foreach (var atr in model.Value->ModelResourceHandle->Attributes)
                        {
                            if (atr.Item1.ToString().Contains("atrx_"))
                            {
                                atrDict.Add(model.Value->RefCount, atr.Item1.Value->ToString().Split('_')[1]);
                            }
                        }
                    }
                    if (model.Value->ModelResourceHandle->Shapes.Count != 0) {
                            foreach (var shp in model.Value->ModelResourceHandle->Shapes)
                            {
                                if (shp.Item1.ToString().Contains("shpx_"))  //!Utils.Constants.VanillaShp.Any
                                {
                                    shpDict.Add(model.Value->RefCount,(shp.Item2, shp.Item1.Value->ToString().Split('_')[1]));
                                }
                            }
                    }
                }



                 * todo: compare non-vanilla shapes to non-vanilla attributes
                 * Gotta figure out what the most memory efficient way of storing the data is, tempted to reference original memory locations but... eh?
                 * I should make this loop through clothing and accessories only, other models arent that useful, yet....
                 * how am I gonna compare these strings?? I need to think of a way short of stripping atr off the front, I'm tempted to enforce atrx_ and shpx_ but then I could have avoided literally everything I just did lol

                foreach(var shp in shpDict)
                {
                    foreach (var atr in atrDict)
                    {
                        if (shp.Key != atr.Key && atr.Value.Contains(shp.Value.Item2))
                        {
                            models[(int)shp.Key].Value->EnabledShapeKeyIndexMask = (uint)shp.Value.Item1;
                        }
                    }

                }

            }
        }
        /

        return;
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
