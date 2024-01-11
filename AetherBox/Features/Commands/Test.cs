using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using ECommons;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Features.Commands;

public class Test : CommandFeature
{
	public override string Name => "Testing";

	public override string Command { get; set; } = "/atest";


	public override string Description => "";

	public override bool isDebug => true;

	public override FeatureType FeatureType => FeatureType.Disabled;

	protected unsafe override void OnCommand(List<string> args)
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon))
		{
			(*addon->GetButtonNodeById(45u)).ClickAddonButton((AtkComponentBase*)addon, 26u);
		}
	}
}
