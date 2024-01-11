// AetherBox, Version=69.3.0.0, Culture=neutral, PublicKeyToken=null
// AetherBox.Features.Commands.TeleportGrandCompany
using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
namespace AetherBox.Features.Commands;
public class TeleportGrandCompany : CommandFeature
{
	public override string Name => "Teleport to Grand Company";

	public override string Command { get; set; } = "/tpgc";


	public override string[] Alias => new string[1] { "" };

	public override string Description => "";

	public override List<string> Parameters => new List<string> { "" };

	public override FeatureType FeatureType => FeatureType.Commands;

	protected unsafe override void OnCommand(List<string> args)
	{
		switch (UIState.Instance()->PlayerState.GrandCompany)
		{
		case 1:
			Svc.Commands.ProcessCommand($"/tp {Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage).GetRow(8u).PlaceName.Value.Name}");
			break;
		case 2:
			Svc.Commands.ProcessCommand($"/tp {Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage).GetRow(2u).PlaceName.Value.Name}");
			break;
		case 3:
			Svc.Commands.ProcessCommand($"/tp {Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage).GetRow(9u).PlaceName.Value.Name}");
			break;
		}
	}
}
