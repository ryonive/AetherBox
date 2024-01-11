using System.Numerics;
using AetherBox;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.UI;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Features.UI;

public class FelicitousTokenInMenu : Feature
{
	private Overlays overlay;

	private float height;

	internal bool active;

	public override string Name => "Show Felicitous Tokens In Menu";

	public override string Description => "Shows the amount of Felicitous Tokens you have in the main Island menu.";

	public override FeatureType FeatureType => FeatureType.Disabled;

	private unsafe static AtkUnitBase* AddonMJIHud => Common.GetUnitBase("MJIHud");

	public override void Enable()
	{
		overlay = new Overlays(this);
		Svc.Framework.Update += OnUpdate;
		base.Enable();
	}

	public override void Disable()
	{
		Plugin.WindowSystem.RemoveWindow(overlay);
		Svc.Framework.Update -= OnUpdate;
		base.Disable();
	}

	public unsafe override void Draw()
	{
		GenericHelpers.TryGetAddonByName<AtkUnitBase>("MJIHud", out var _);
	}

	private unsafe void OnUpdate(IFramework framework)
	{
		if (UiHelper.IsAddonReady(AddonMJIHud))
		{
			AtkResNode* currencyPositionNode;
			currencyPositionNode = Common.GetNodeByID(&AddonMJIHud->UldManager, 3u);
			if (currencyPositionNode != null)
			{
				new Vector2(currencyPositionNode->X, currencyPositionNode->Y);
			}
		}
	}

	private unsafe void TryMakeIconNode(uint nodeId, Vector2 position, int icon, bool hqIcon, string? tooltipText = null)
	{
		AtkResNode* iconNode;
		iconNode = Common.GetNodeByID(&AddonMJIHud->UldManager, nodeId);
		if (iconNode == null)
		{
			MakeIconNode(nodeId, position, icon, hqIcon, tooltipText);
		}
		else
		{
			iconNode->SetPositionFloat(position.X, position.Y);
		}
	}

	private unsafe void MakeIconNode(uint nodeId, Vector2 position, int icon, bool hqIcon, string? tooltipText = null)
	{
		AtkImageNode* intPtr;
		intPtr = UiHelper.MakeImageNode(nodeId, new UiHelper.PartInfo(0, 0, 36, 36));
		intPtr->AtkResNode.NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;
		intPtr->WrapMode = 1;
		intPtr->Flags = 128;
		intPtr->LoadIconTexture(hqIcon ? (icon + 1000000) : icon, 0);
		intPtr->AtkResNode.ToggleVisibility(enable: true);
		intPtr->AtkResNode.SetWidth(36);
		intPtr->AtkResNode.SetHeight(36);
		intPtr->AtkResNode.SetPositionShort((short)position.X, (short)position.Y);
		UiHelper.LinkNodeAtEnd((AtkResNode*)intPtr, AddonMJIHud);
	}
}
