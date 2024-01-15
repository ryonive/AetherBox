using ClickLib.Bases;
using ClickLib.Clicks;

namespace AetherBox.TemporaryFixes;

public sealed class ClickRepairFixed : ClickBase<ClickRepair, AddonRepairFixed>
{
    public ClickRepairFixed(nint addon = 0)
        : base("Repair", addon)
    {
    }

    public static implicit operator ClickRepairFixed(nint addon)
    {
        return new ClickRepairFixed(addon);
    }

    public static ClickRequest Using(nint addon)
    {
        return new ClickRequest(addon);
    }

    public unsafe void RepairAll()
    {
        ClickAddonButton(base.Addon->RepairAllButton, 0u);
    }
}
