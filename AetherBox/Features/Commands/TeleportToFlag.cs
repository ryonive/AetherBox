using System;
using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace AetherBox.Features.Commands;

public class TeleportToFlag : CommandFeature
{
	public override string Name => "Teleport to Flag";

	public override string Command { get; set; } = "/teleportflag";


	public override string[] Alias => new string[1] { "/tpf" };

	public override string Description => "Teleports you to the aetheryte nearest your <flag>";

	public override FeatureType FeatureType => FeatureType.Commands;

	protected unsafe override void OnCommand(List<string> args)
	{
		CoordinatesHelper.TeleportToAetheryte(new CoordinatesHelper.MapLinkMessage(0, "", "", AgentMap.Instance()->FlagMapMarker.XFloat, AgentMap.Instance()->FlagMapMarker.YFloat, 100f, AgentMap.Instance()->FlagMapMarker.TerritoryId, "", DateTime.Now));
	}
}
