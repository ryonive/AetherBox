using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox;

namespace AetherBox.Features.UI;

public class AutoFocus : Feature
{
    public override string Name => "Auto-Focus Marketboard Search";

    public override string Description => "Automatically focuses the search bar for the marketboard.";

    public override FeatureType FeatureType => FeatureType.UI;

    private unsafe void AddonSetup(SetupAddonArgs obj)
    {
        if (!(obj.AddonName != "ItemSearch"))
        {
            obj.Addon->SetFocusNode(obj.Addon->CollisionNodeList[11]);
        }
    }

    public override void Enable()
    {
        Common.OnAddonSetup += AddonSetup;
        base.Enable();
    }

    public override void Disable()
    {
        Common.OnAddonSetup -= AddonSetup;
        base.Disable();
    }
}
