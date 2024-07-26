using AetherBox.Features;
using AetherBox.Features.Experiments;
using AetherBox.FeaturesSetup;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
namespace AetherBox.Features.Experiments;
public class AutoRefocus : Feature
{
	private unsafe delegate void SetFocusTargetByObjectIDDelegate(TargetSystem* targetSystem, long objectID);

	[Signature("E8 ?? ?? ?? ?? BA 0C 00 00 00 48 8D 0D", DetourName = "SetFocusTargetByObjectID")]
	private readonly Hook<SetFocusTargetByObjectIDDelegate> setFocusTargetByObjectIDHook;

	private static ulong? FocusTarget;

	public override string Name => "Auto Refocus";

	public override string Description => "Keeps your focus target persistent between zones.";

	public override FeatureType FeatureType => FeatureType.Other;

	public override void Enable()
	{
		base.Enable();
		Svc.Hook.InitializeFromAttributes(this);
		setFocusTargetByObjectIDHook?.Enable();
		Svc.ClientState.TerritoryChanged += OnZoneChange;
	}

	public override void Disable()
	{
		base.Disable();
		setFocusTargetByObjectIDHook.Dispose();
		Svc.ClientState.TerritoryChanged -= OnZoneChange;
		Svc.Framework.Update -= OnUpdate;
	}

	private void OnZoneChange(ushort obj)
	{
		FocusTarget = null;
		Svc.Framework.Update += OnUpdate;
	}

	private unsafe void OnUpdate(IFramework framework)
	{
		if (FocusTarget.HasValue && Svc.Targets.FocusTarget == null)
		{
			setFocusTargetByObjectIDHook.Original(TargetSystem.StaticAddressPointers.pInstance, (long)FocusTarget.Value);
		}
		else
		{
			Svc.Framework.Update -= OnUpdate;
		}
	}

	private unsafe void SetFocusTargetByObjectID(TargetSystem* targetSystem, long objectID)
	{
		if (objectID == 3758096384u)
		{
			objectID = (uint)(((int?)Svc.Targets.Target?.EntityId) ?? (-536870912));
			FocusTarget = Svc.Targets.Target?.EntityId;
		}
		else
		{
			FocusTarget = Svc.Targets.Target.EntityId;
		}
		setFocusTargetByObjectIDHook.Original(targetSystem, objectID);
	}
}
