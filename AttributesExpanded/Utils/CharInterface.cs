using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using ImGuizmoNET;
using InteropGenerator.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Game.ClientState.Objects.Types;

namespace AttributesExpanded.Utils
{
    internal class CharInterface : IDisposable
    {
        public CharInterface()
        {
            Service.Log.Information("Attribute Checker starting...");
        }
        public static unsafe void OnCreatingCharacterBase(nint gameObjectAddress, Guid _1, nint _2, nint customizePtr, nint _3)
        {

            // return if not player character
            var gameObj = (GameObject*)gameObjectAddress;
            var draw = (Human*) gameObj->DrawObject;
            Service.Log.Information($"===A cool log message from {*draw}===");

            //foreach (Model* equipmodel in draw->ModelsSpan)
            //{
            //  Service.Log.Information($"===A cool log message from {equipmodel->SlotIndex}===");
            // var atrMap = equipmodel->ModelResourceHandle->Attributes;
            //foreach (var atr in atrMap)
            //{
            //  var temp = atr;

            //                }

            //}





            Service.Log.Information($"===A cool log message from {(nint)gameObj}===");
            
        }
        
        public void Dispose(){}
    }
    
}
