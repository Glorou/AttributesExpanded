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
using FFXIVClientStructs.FFXIV.Client.Game.Character;
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


public unsafe class Plugin : IDalamudPlugin
{
    
    
    
    public MySiggedHook _mySiggedHook;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Service.PluginInterface = pluginInterface;
        _ = pluginInterface.Create<Service>();
        Service.plugin = this;
        Service.Log.Information($"==={Service.PluginInterface.Manifest.Name} turning on ===");
        _mySiggedHook = new MySiggedHook();
    }
    
    public void Dispose()
    {
        _mySiggedHook.Dispose();
    }
    
    /// <summary>
    /// Main hook, Everything this plugin does is inside here
    /// </summary>
    public unsafe class MySiggedHook : IDisposable
    {
        private delegate void SetupTopModelAttributes(Human* self, byte* data);
        private delegate void SetupLegModelAttributes(Human* self, byte* data);
        private delegate void SetupFootModelAttributes(Human* self, byte* data);
        private delegate void SetupHandModelAttributes(Human* self, byte* data);
        
        

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
        

        private Dictionary<String, short> tempTopShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempBottomShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempHandShapes = new Dictionary<String, short>();
        private Dictionary<String, short> tempFootShapes = new Dictionary<String, short>();
        
        public MySiggedHook()
        {
            Service.GameInteropProvider.InitializeFromAttributes(this);
            
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
        
        /// <summary>
        /// Function iterates through all checked objects and sets bitmask for matching instances
        /// </summary>
        /// <param name="self"></param>
        private void CalculateBitmask(Human* self)
        {
            //Top
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
            
            //Hands
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

            //Legs
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
            
            //Foot
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
            
            //Check waist
            if (tempTopShapes.Count > 0 && tempBottomShapes.Count > 0)
            {
                foreach (var shape in tempTopShapes.Keys)
                {
                    if (tempBottomShapes.Keys.Contains(shape))
                    {
                        self->ModelsSpan[1].Value->EnabledShapeKeyIndexMask |= (ushort)(1 << tempTopShapes[shape]);
                        self->ModelsSpan[3].Value->EnabledShapeKeyIndexMask |= (ushort)(1 << tempBottomShapes[shape]);
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
                        self->ModelsSpan[1].Value->EnabledShapeKeyIndexMask |= (ushort)(1 << tempTopShapes[shape]);
                        self->ModelsSpan[2].Value->EnabledShapeKeyIndexMask |= (ushort)(1 << tempHandShapes[shape]);
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
                        self->ModelsSpan[3].Value->EnabledShapeKeyIndexMask |= (ushort)(1 << tempBottomShapes[shape]);
                        self->ModelsSpan[4].Value->EnabledShapeKeyIndexMask |= (ushort)(1 << tempFootShapes[shape]);
                    }
                }
            }
            tempTopShapes.Clear();
            tempBottomShapes.Clear();
            tempHandShapes.Clear();
            tempFootShapes.Clear();
        }

        
        /// <summary>
        /// Detours top model processing, checks if legs have been updated, and if they haven't it calls the leg detour
        /// The game is not guaranteed to update both so we have to do it manually.
        /// </summary>
        private void DetourTopModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Top Detour ");
            _topUpdateHook!.Original(self, data);
            CalculateBitmask(self);
        }
        
        /// <summary>
        /// Detours leg model processing, checks if top has been updated, and if they haven't it calls the top detour
        /// The game is not guaranteed to update both so we have to do it manually.
        /// </summary>
        private void DetourLegModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Leg Detour");
            _legUpdateHook!.Original(self, data);
            CalculateBitmask(self);
        }
        
        /// <summary> Detours hands, these should always run when a Chest piece is updated and vice versa. </summary>
        /// <remarks> This actually never hits because it was inlined, but can be useful maybe</remarks>
        private void DetourHandModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Hand Detour");
            _handUpdateHook!.Original(self, data);
            CalculateBitmask(self);
        }
        /// <summary> Detours feet, these should always run when a Leg piece is updated and vice versa. </summary>
        private void DetourFootModelAttributes(Human* self, byte* data)
        {
            Service.Log.Information("Foot Detour");
            _footUpdateHook!.Original(self, data );
            CalculateBitmask(self);
        }
    }
    
}

