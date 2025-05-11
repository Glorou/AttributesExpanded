using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.Interop.PathResolving;

namespace SimplyShapes;

public unsafe class Plugin : IDalamudPlugin
{
    
    public ShapekeyHookset _shapekeyHookset;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Service.PluginInterface = pluginInterface;
        _ = pluginInterface.Create<Service>();
        Service.plugin = this;
        Service.CollectionResolver = new CollectionResolver();  //This is empty on purpose
        Service.Log.Information($"==={Service.PluginInterface.Manifest.Name} turning on ===");
        _shapekeyHookset = new ShapekeyHookset();
    }
    
    public void Dispose()
    {
        _shapekeyHookset.Dispose();
    }
    
    /// <summary>
    /// Main hook, Everything this plugin does is inside here
    /// </summary>
    public unsafe class ShapekeyHookset : IDisposable
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
        
        public ShapekeyHookset()
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
        /// Check if the Human* draw object is in a collection
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>

        public bool CheckHumanCollection(Human* self)
        {
            return Service.CollectionResolver.IdentifyCollection((DrawObject*)self, true).Valid; //Not entirely sure if this would be cached here
        }

        
        /// <summary>
        /// Check if the model is modded by checking the path to the file, modded should return true
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool IsModelRooted(Model* model)
        {
            return Path.IsPathRooted(model->ModelResourceHandle->FileName.ToString());
        }
        
        /// <summary>
        /// Function checks if a collection even applies to the Human*, if there is one, then checks if any seams have modded items
        /// then iterates through all checked objects and sets bitmask for matching instances if a seam check passes
        /// </summary>
        /// <param name="self"></param>
        private void CalculateBitmask(Human* self)
        {
            if (!CheckHumanCollection(self))
            {
                return;
            }

            bool enableWaist = IsModelRooted(self->ModelsSpan[1]) && IsModelRooted(self->ModelsSpan[3]);
            bool enableWrist = IsModelRooted(self->ModelsSpan[1]) && IsModelRooted(self->ModelsSpan[2]);
            bool enableAnkle = IsModelRooted(self->ModelsSpan[3]) && IsModelRooted(self->ModelsSpan[4]);
            
            if (!(enableWaist || enableWrist || enableAnkle))
            {
                return;
            }
            //Top
            if (self->ModelsSpan[1] != null && (enableWaist || enableWrist))
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
            if (self->ModelsSpan[2] != null && enableWrist)
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
            if (self->ModelsSpan[3] != null && (enableWaist || enableAnkle))
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
            if (self->ModelsSpan[4] != null && enableWrist)
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
