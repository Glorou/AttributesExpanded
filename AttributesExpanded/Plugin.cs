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
    public MySiggedHook _mySiggedHook;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {

        Service.PluginInterface = pluginInterface;
        Service.configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _ = pluginInterface.Create<Service>();
        Service.plugin = this;

        //_ = pluginInterface.Create<Utils.CharInterface>();
        //_ = pluginInterface.Create<Utils.TextureInterface>();
        // you might normally want to embed resources and load them from the manifest stream
        

        Service.Log.Information($"===A cool log message from {Service.PluginInterface.Manifest.Name}===");

        
        _mySiggedHook = new MySiggedHook();

    }

    public void Dispose()
    {
        _mySiggedHook.Dispose();
        WindowSystem.RemoveAllWindows();
        Service.Commands.RemoveHandler(CommandName);
    }
    
    
    
    /*
     *Consider execution flow:
     * Top happens first but only if one exists, I dont think its possible to *not* have a top? 
     * for now do all processing of the model data and checking if I need to swap there
     * We hook in to the rest of the functions and check if we need to activate them at all, make a dict<String, bool>
     * so I can do if(enableDict["foot"]){actually mutate here} I should also probably calculate the bitmask outside the hook and store it too 
     * oh or just do dict<String, short> and if .Value > 0 means it hit 
     */
    public unsafe class MySiggedHook : IDisposable
    {
        // This method isn't in CS (in theory), so we need to declare our own delegate.
        private delegate void SetupTopModelAttributes(Human* self, byte* data);

        private delegate void SetupLegModelAttributes(Human* self, byte* data);
        
        private delegate void SetupFootModelAttributes(Human* self, byte* data);
        
        private delegate void SetupHandModelAttributes(Human* self, byte* data);
        
        private delegate void SetupFromResourceHandle(Model* _model, ModelResourceHandle* _resourceHandle, nint _renderModelCallback, nint _renderMaterialCallback);

        [Signature("E8 ?? ?? ?? ?? 48 8B 87 ?? ?? ?? ?? 44 0F B6 7C 24", DetourName = nameof(DetourTopModelAttributes))]
        private Hook<SetupTopModelAttributes>? _topUpdateHook;

        [Signature(
            "E8 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B CF E8 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B 87",
            DetourName = nameof(DetourLegModelAttributes))]
        private Hook<SetupLegModelAttributes>? _legUpdateHook;
        
        [Signature("40 53 57 41 56 48 83 EC 20 48 8B 81 ?? ?? ?? ?? 48 8B FA", DetourName = nameof(DetourHandModelAttributes))]
        private Hook<SetupHandModelAttributes>? _handUpdateHook;
        
        [Signature("40 53 56 41 56 48 83 EC ?? 48 8B 99 ?? ?? ?? ?? 48 8B F2", DetourName = nameof(DetourFootModelAttributes))]
        private Hook<SetupFootModelAttributes>? _footUpdateHook;
        
        [Signature("E8 ?? ?? ?? ?? 84 C0 75 ?? 48 8B 07 48 8B CF FF 50 ?? 32 C0", DetourName = nameof(DetourFromResourceHandle))]
        private Hook<SetupFromResourceHandle>? _unknUpdateHook;

        private Dictionary<String, short> tempTopShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempBottomShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempHandShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempFootShapes = new Dictionary<String, short>();

        private Dictionary<short, uint> mask = new Dictionary<short, uint>
        {
            {1,0},
            {2,0},
            {3,0},
            {4,0}
        };

        public MySiggedHook()
        {
            Service.GameInteropProvider.InitializeFromAttributes(this);

            // Nullable because this might not have been initialized from IFA above, e.g. the sig was invalid.
            _topUpdateHook?.Enable();
            _legUpdateHook?.Enable();
            _handUpdateHook?.Enable();
            _footUpdateHook?.Enable();
        }

        public void Dispose()
        {
            _topUpdateHook?.Dispose();
            _legUpdateHook?.Dispose();
            _handUpdateHook?.Dispose();
            _footUpdateHook?.Dispose();
        }


        /// <summary> Function iterates through all checked objects and sets bitmask for matching instances </summary>
        private void CalculateBitmask(Human* self)
        {
            //check if we already did this, 
                
                //this is kinda ass but i'll fix it later
                //Body
                try
                {

                    if (self->ModelsSpan[1] != null)
                    {
                    

                        foreach (var shape in self->ModelsSpan[1].Value->ModelResourceHandle->Shapes)
                        {
                            if (shape.Item1.AsSpan().StartsWith("shp_wa_"u8) || shape.Item1.AsSpan().StartsWith("shp_wr_"u8))
                            {
                                tempTopShapes.Add(shape.Item1.ToString(), shape.Item2);
                            }
                        
                        }
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error(ex, "An error occured when handling Body shapes.");
                }
                
                //Hands
                try
                {
                    if (self->ModelsSpan[2] != null)
                    {

                        foreach (var shape in self->ModelsSpan[2].Value->ModelResourceHandle->Shapes)
                        {
                            if (shape.Item1.AsSpan().StartsWith("shp_wr_"u8))
                            {
                                tempHandShapes.Add(shape.Item1.ToString(), shape.Item2);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error(ex, "An error occured when handling Hand shapes.");
                }

                //Legs
                
                try
                {
                    if (self->ModelsSpan[3] != null)
                    {

                        foreach (var shape in self->ModelsSpan[3].Value->ModelResourceHandle->Shapes)
                        {
                            if (shape.Item1.AsSpan().StartsWith("shp_wa_"u8) || shape.Item1.AsSpan().StartsWith("shp_an_"u8))
                            {
                                tempBottomShapes.Add(shape.Item1.ToString(), shape.Item2);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error(ex, "An error occured when handling Leg shapes.");
                }


                //Foot
                try
                {
                    if (self->ModelsSpan[4] != null)
                    {

                        foreach (var shape in self->ModelsSpan[4].Value->ModelResourceHandle->Shapes)
                        {
                            if (shape.Item1.AsSpan().StartsWith("shp_an"u8))
                            {
                                tempFootShapes.Add(shape.Item1.ToString(), shape.Item2);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error(ex, "An error occured when handling Foot shapes.");
                }

                //Check waist
                if (tempTopShapes.Count > 0 && tempBottomShapes.Count > 0)
                {
                    foreach (var shape in tempTopShapes.Keys)
                    {
                        if (tempBottomShapes.Keys.Contains(shape))
                        {
                            
                            mask[1] |= (uint)(1 << tempTopShapes[shape]);
                            mask[3] |= (uint)(1 << tempBottomShapes[shape]);
                        }
                    }
                }
                //Check wrist
                if (tempTopShapes.Count > 0 && tempHandShapes.Count > 0)
                {
                    foreach (var shape in tempTopShapes.Keys)
                    {
                        if (tempHandShapes.Keys.Contains(shape))
                        {
                            
                            mask[1] |= (uint)(1 << tempTopShapes[shape]);
                            mask[2] |= (uint)(1 << tempHandShapes[shape]);
                        }
                    }
                }
                
                //Check ankles 
                if (tempBottomShapes.Count > 0 && tempFootShapes.Count > 0)
                {
                    foreach (var shape in tempBottomShapes.Keys)
                    {
                        if (tempFootShapes.Keys.Contains(shape))
                        {
                            
                            mask[3] |= (uint)(1 << tempBottomShapes[shape]);
                            mask[4] |= (uint)(1 << tempFootShapes[shape]);
                        }
                    }
                }
                tempTopShapes.Clear();
                tempBottomShapes.Clear();
                tempHandShapes.Clear();
                tempFootShapes.Clear();
                
                return;
            }

        

        private void DetourTopModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Top Detour ");
            CalculateBitmask(self);
            _topUpdateHook!.Original(self, data);
            if (mask[1] != 0 && self->ModelsSpan[1] != null)
            {
                self->ModelsSpan[1].Value->EnabledShapeKeyIndexMask = mask[1];
            }
            mask[1] = 0;
        }

        private void DetourLegModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Leg Detour");
            CalculateBitmask(self);
            _legUpdateHook!.Original(self, data);
            if (mask[3] != 0 && self->ModelsSpan[3] != null)
            {
                self->ModelsSpan[3].Value->EnabledShapeKeyIndexMask = mask[3];
                
            }
            mask[3] = 0;
        }
        private void DetourHandModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Hand Detour");
            CalculateBitmask(self);
            _legUpdateHook!.Original(self, data);
            if (mask[2] != 0 && self->ModelsSpan[2] != null)
            {
                self->ModelsSpan[2].Value->EnabledShapeKeyIndexMask = mask[2];
                
            }
            mask[2] = 0;
        }
        private void DetourFootModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Foot Detour");
            CalculateBitmask(self);
            _legUpdateHook!.Original(self, data );
            if (mask[4] != 0 && self->ModelsSpan[4] != null)
            {
                self->ModelsSpan[4].Value->EnabledShapeKeyIndexMask = mask[4];
                
            }
            mask[4] = 0;
        }
        
        private void DetourFromResourceHandle(Model* _model, ModelResourceHandle* _resourceHandle, nint _renderModelCallback, nint _renderMaterialCallback)
        {
            Service.Log.Information("Resource Handle Detour ");
            _unknUpdateHook!.Original(_model, _resourceHandle, _renderModelCallback, _renderMaterialCallback);
            var model = _model;

        }
        
        
    }
    
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


  

