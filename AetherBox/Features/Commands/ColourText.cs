using System;
using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.Automation;
using ECommons.DalamudServices;
using Lumina.Excel.GeneratedSheets;
namespace AetherBox.Features.Commands;
public class ColourText : CommandFeature
{
	public override string Name => "Colour Text";

	public override string Command { get; set; } = "/colourtext";


	public override string Description => "Makes your text a random colour.";

	public override bool isDebug => true;

	public override FeatureType FeatureType => FeatureType.Commands;

	protected override void OnCommand(List<string> args)
	{
		int randomRowIndex;
		randomRowIndex = new Random((int)(DateTime.Now.Ticks / 10000 % int.MaxValue)).Next(1, (int)(Svc.Data.GetExcelSheet<UIColor>().RowCount + 1));
		byte[] bytes;
		bytes = new SeString(new UIForegroundPayload((ushort)Svc.Data.GetExcelSheet<UIColor>().GetRow((uint)randomRowIndex).UIForeground), new TextPayload(string.Join(" ", args)), UIForegroundPayload.UIForegroundOff).Encode();
		Chat.Instance.SendMessageUnsafe(bytes);
	}
}
