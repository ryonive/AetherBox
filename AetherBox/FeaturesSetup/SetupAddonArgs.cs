using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

#nullable disable
namespace AetherBox
{
    public class SetupAddonArgs
    {
        private string addonName;

        public unsafe AtkUnitBase* Addon { get; init; }

        public unsafe string AddonName
        {
            get
            {
                return addonName ?? (addonName = MemoryHelper.ReadString(new IntPtr(Addon->Name), 32).Split(char.MinValue)[0]);
            }
        }
    }
}
