using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.Automation;
namespace AetherBox.Features.Commands;
public class FakeTranslate : CommandFeature
{
	public override string Name => "Fake Translate";

	public override string Command { get; set; } = "/faketranslate";


	public override string Description => "Use those funny translate arrows on any text";

	public override bool isDebug => true;

	public override FeatureType FeatureType => FeatureType.Commands;

	protected override void OnCommand(List<string> args)
	{
		byte[] bytes;
		bytes = new SeString(new IconPayload(BitmapFontIcon.AutoTranslateBegin), new TextPayload(string.Join(" ", args)), new IconPayload(BitmapFontIcon.AutoTranslateEnd)).Encode();
		Chat.Instance.SendMessageUnsafe(bytes);
	}
}
