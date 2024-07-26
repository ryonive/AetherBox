using System.Linq;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
namespace AetherBox.Features.Other;
public class GMAlert : Feature
{
	public override string Name => "GM Alert";

	public override string Description => "Chat message when a GM is nearby";

	public override FeatureType FeatureType => FeatureType.Other;

	public override void Enable()
	{
		base.Enable();
		Svc.Framework.Update += OnUpdate;
	}

	public override void Disable()
	{
		base.Disable();
		Svc.Framework.Update -= OnUpdate;
	}

	private unsafe void OnUpdate(IFramework framework)
	{
		if (Svc.ClientState.LocalPlayer == null)
		{
			return;
		}
		foreach (IPlayerCharacter player in from pc in Svc.Objects.OfType<IPlayerCharacter>()
			where pc.EntityId != 234881024
			select pc)
		{
			byte onlineStatus;
			onlineStatus = ((Character*)player.Address)->CharacterData.OnlineStatus;
			if (onlineStatus <= 3 && onlineStatus > 0)
			{
				PrintModuleMessage($"Pssst, there's a GM nearby: " + player.Name + $"Position: " + player.Position);
			}
		}
	}
}
