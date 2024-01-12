using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AetherBox;
using AetherBox.Features;
using AetherBox.Features.UI;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.UI;

public class WondrousTailsClover : Feature
{
	public record DutyFinderSearchResult(string SearchKey, uint TerritoryType);

	private IEnumerable<WondrousTailsHelpers.WondrousTailsTask> wondrousTailsStatus;

	private const uint GoldenCloverNodeId = 29u;

	private const uint EmptyCloverNodeId = 30u;

	public readonly List<DutyFinderSearchResult> Duties = new List<DutyFinderSearchResult>();

	public override string Name => "Wondrous Tails Clovers";

	public override string Description => "Adds a clover next to duties in the Duty Finder that are part of your Wondrous Tails.";

	public override FeatureType FeatureType => FeatureType.Disabled;

	public override void Enable()
	{
		foreach (ContentFinderCondition cfc2 in from cfc in Svc.Data.GetExcelSheet<ContentFinderCondition>()
			where cfc.Name != string.Empty
			select cfc)
		{
			string simplifiedString;
			simplifiedString = TextHelper.FilterNonAlphanumeric(cfc2.Name.ToString().ToLower());
			Duties.Add(new DutyFinderSearchResult(simplifiedString, cfc2.TerritoryType.Row));
		}
		global::AetherBox.AetherBox.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnUpdate);
		global::AetherBox.AetherBox.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ContentsFinder", OnRefresh);
		global::AetherBox.AetherBox.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "ContentsFinder", OnDraw);
		global::AetherBox.AetherBox.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "ContentsFinder", OnFinalize);
		wondrousTailsStatus = WondrousTailsHelpers.WondrousTailsBook.GetAllTaskData();
		base.Enable();
	}

	public override void Disable()
	{
		global::AetherBox.AetherBox.AddonLifecycle.UnregisterListener(OnUpdate);
		global::AetherBox.AetherBox.AddonLifecycle.UnregisterListener(OnRefresh);
		global::AetherBox.AetherBox.AddonLifecycle.UnregisterListener(OnDraw);
		global::AetherBox.AetherBox.AddonLifecycle.UnregisterListener(OnFinalize);
		base.Disable();
	}

	private void OnRefresh(AddonEvent type, AddonArgs args)
	{
		if (Enabled)
		{
			wondrousTailsStatus = WondrousTailsHelpers.WondrousTailsBook.GetAllTaskData();
		}
	}

	private void OnUpdate(AddonEvent type, AddonArgs args)
	{
		foreach (nint listItem in WondrousTailsHelpers.GetDutyListItems(args.Addon))
		{
			PlayerState.WeeklyBingoTaskStatus? taskState;
			taskState = IsWondrousTailsDuty(listItem);
			if (!taskState.HasValue || !WondrousTailsHelpers.WondrousTailsBook.PlayerHasBook || !Enabled)
			{
				SetCloverNodesVisibility(listItem, WondrousTailsHelpers.CloverState.Hidden);
				continue;
			}
			if (taskState == PlayerState.WeeklyBingoTaskStatus.Claimed)
			{
				SetCloverNodesVisibility(listItem, WondrousTailsHelpers.CloverState.Dark);
				continue;
			}
			bool flag;
			if (taskState.HasValue)
			{
				PlayerState.WeeklyBingoTaskStatus valueOrDefault;
				valueOrDefault = taskState.GetValueOrDefault();
				if ((uint)valueOrDefault <= 1u)
				{
					flag = true;
					goto IL_008a;
				}
			}
			flag = false;
			goto IL_008a;
			IL_008a:
			if (flag)
			{
				SetCloverNodesVisibility(listItem, WondrousTailsHelpers.CloverState.Golden);
			}
		}
	}

	private unsafe void OnDraw(AddonEvent type, AddonArgs args)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (nint listItem in WondrousTailsHelpers.GetDutyListItems(args.Addon))
		{
			if (WondrousTailsHelpers.GetListItemNode<AtkImageNode>(listItem, 29u) == null)
			{
				MakeCloverNode(listItem, 29u);
			}
			if (WondrousTailsHelpers.GetListItemNode<AtkImageNode>(listItem, 30u) == null)
			{
				MakeCloverNode(listItem, 30u);
			}
			AtkResNode* moogleNode;
			moogleNode = WondrousTailsHelpers.GetListItemNode<AtkResNode>(listItem, 6u);
			if (moogleNode != null && moogleNode->X != 285f)
			{
				moogleNode->X = 285f;
			}
			AtkResNode* levelSyncNode;
			levelSyncNode = WondrousTailsHelpers.GetListItemNode<AtkResNode>(listItem, 10u);
			if (levelSyncNode != null && levelSyncNode->X != 305f)
			{
				levelSyncNode->X = 305f;
			}
		}
	}

	private unsafe void OnFinalize(AddonEvent type, AddonArgs args)
	{
		foreach (nint dutyListItem in WondrousTailsHelpers.GetDutyListItems(args.Addon))
		{
			AtkImageNode* goldenNode;
			goldenNode = WondrousTailsHelpers.GetListItemNode<AtkImageNode>(dutyListItem, 29u);
			if (goldenNode != null)
			{
				ImageNode.FreeImageNode(goldenNode);
			}
			AtkImageNode* emptyNode;
			emptyNode = WondrousTailsHelpers.GetListItemNode<AtkImageNode>(dutyListItem, 30u);
			if (emptyNode != null)
			{
				ImageNode.FreeImageNode(emptyNode);
			}
		}
	}

	private PlayerState.WeeklyBingoTaskStatus? IsWondrousTailsDuty(nint item)
	{
		string listItemString;
		listItemString = WondrousTailsHelpers.GetListItemString(item);
		string nodeRegexString;
		nodeRegexString = WondrousTailsHelpers.GetListItemFilteredString(item);
		bool containsEllipsis;
		containsEllipsis = listItemString.Contains("...");
		foreach (DutyFinderSearchResult result in Duties)
		{
			if (containsEllipsis)
			{
				int nodeStringLength;
				nodeStringLength = nodeRegexString.Length;
				if (result.SearchKey.Length > nodeStringLength && result.SearchKey.Substring(0, nodeStringLength) == nodeRegexString)
				{
					return GetWondrousTailsTaskState(result.TerritoryType);
				}
			}
			else if (result.SearchKey == nodeRegexString)
			{
				return GetWondrousTailsTaskState(result.TerritoryType);
			}
		}
		return null;
	}

	private PlayerState.WeeklyBingoTaskStatus? GetWondrousTailsTaskState(uint duty)
	{
		return wondrousTailsStatus.FirstOrDefault((WondrousTailsHelpers.WondrousTailsTask task) => task.DutyList.Contains(duty))?.TaskState;
	}

	private unsafe void SetCloverNodesVisibility(nint listItem, WondrousTailsHelpers.CloverState state)
	{
		AtkImageNode* goldenClover;
		goldenClover = WondrousTailsHelpers.GetListItemNode<AtkImageNode>(listItem, 29u);
		AtkImageNode* emptyClover;
		emptyClover = WondrousTailsHelpers.GetListItemNode<AtkImageNode>(listItem, 30u);
		switch (state)
		{
		case WondrousTailsHelpers.CloverState.Hidden:
			goldenClover->AtkResNode.ToggleVisibility(enable: false);
			emptyClover->AtkResNode.ToggleVisibility(enable: false);
			break;
		case WondrousTailsHelpers.CloverState.Golden:
			goldenClover->AtkResNode.ToggleVisibility(enable: true);
			emptyClover->AtkResNode.ToggleVisibility(enable: false);
			break;
		case WondrousTailsHelpers.CloverState.Dark:
			goldenClover->AtkResNode.ToggleVisibility(enable: false);
			emptyClover->AtkResNode.ToggleVisibility(enable: true);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	private unsafe void MakeCloverNode(nint listItem, uint id)
	{
		if (listItem != IntPtr.Zero)
		{
			AtkResNode* textNode;
			textNode = (AtkResNode*)WondrousTailsHelpers.GetListItemTextNode(listItem);
			if (textNode != null)
			{
				Vector2 textureCoordinates;
				textureCoordinates = ((id == 29) ? new Vector2(97f, 65f) : new Vector2(75f, 63f));
				AtkImageNode* imageNode;
				imageNode = ImageNode.MakeNode(id, textureCoordinates, new Vector2(20f, 20f));
				imageNode->LoadTexture("ui/uld/WeeklyBingo.tex");
				imageNode->AtkResNode.ToggleVisibility(enable: true);
				imageNode->AtkResNode.SetWidth(20);
				imageNode->AtkResNode.SetHeight(20);
				Vector2 positionOffset;
				positionOffset = Vector2.Zero;
				short xPosition;
				xPosition = (short)(325f + positionOffset.X);
				short yPosition;
				yPosition = (short)(2f + positionOffset.Y);
				imageNode->AtkResNode.SetPositionShort(xPosition, yPosition);
				ImageNode.LinkNode((AtkComponentNode*)listItem, textNode, imageNode);
			}
		}
	}
}
