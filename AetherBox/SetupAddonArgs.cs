using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox;

public class SetupAddonArgs
{
    private string addonName;

    public unsafe AtkUnitBase* Addon { get; init; }

    public unsafe string AddonName => addonName ?? (addonName = MemoryHelper.ReadString(new nint(Addon->Name), 32).Split('\0')[0]);
}
