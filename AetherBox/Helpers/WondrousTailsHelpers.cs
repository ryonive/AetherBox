using System;
using System.Collections.Generic;
using System.Linq;
using AetherBox.Helpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Helpers;

internal static class WondrousTailsHelpers
{
	public record WondrousTailsTask(PlayerState.WeeklyBingoTaskStatus TaskState, List<uint> DutyList);

	public class WondrousTailsBook
	{
		private static WondrousTailsBook? _instance;

		public static WondrousTailsBook Instance => _instance ?? (_instance = new WondrousTailsBook());

		public unsafe int Stickers => PlayerState.Instance()->WeeklyBingoNumPlacedStickers;

		public unsafe uint SecondChance => PlayerState.Instance()->WeeklyBingoNumSecondChancePoints;

		public unsafe static bool PlayerHasBook => PlayerState.Instance()->HasWeeklyBingoJournal;

		public bool NewBookAvailable => DateTime.Now > Deadline - TimeSpan.FromDays(7.0);

		public bool IsComplete => Stickers == 9;

		public bool NeedsNewBook
		{
			get
			{
				if (NewBookAvailable)
				{
					return IsComplete;
				}
				return false;
			}
		}

		private unsafe DateTime Deadline => DateTimeOffset.FromUnixTimeSeconds(PlayerState.Instance()->GetWeeklyBingoExpireUnixTimestamp()).ToLocalTime().DateTime;

		public static WondrousTailsTask? GetTaskForDuty(uint instanceID)
		{
			return GetAllTaskData().FirstOrDefault((WondrousTailsTask task) => task.DutyList.Contains(instanceID));
		}

		public unsafe static IEnumerable<WondrousTailsTask> GetAllTaskData()
		{
			return (from index in Enumerable.Range(0, 16)
				let taskButtonState = PlayerState.Instance()->GetWeeklyBingoTaskStatus(index)
				let instances = TaskLookup.GetInstanceListFromID(PlayerState.Instance()->WeeklyBingoOrderData[index])
				select new WondrousTailsTask(taskButtonState, instances)).ToList();
		}
	}

	public enum CloverState
	{
		Hidden = 0,
		Golden = 1,
		Dark = 2
	}

	public struct CloverNode
	{
		public unsafe AtkImageNode* GoldenCloverNode;

		public unsafe AtkImageNode* EmptyCloverNode;

		public unsafe CloverNode(AtkImageNode* golden, AtkImageNode* dark)
		{
			GoldenCloverNode = golden;
			EmptyCloverNode = dark;
		}

		public unsafe readonly void SetVisibility(CloverState state)
		{
			if (GoldenCloverNode != null && EmptyCloverNode != null)
			{
				switch (state)
				{
				case CloverState.Hidden:
					GoldenCloverNode->AtkResNode.ToggleVisibility(enable: false);
					EmptyCloverNode->AtkResNode.ToggleVisibility(enable: false);
					break;
				case CloverState.Golden:
					GoldenCloverNode->AtkResNode.ToggleVisibility(enable: true);
					EmptyCloverNode->AtkResNode.ToggleVisibility(enable: false);
					break;
				case CloverState.Dark:
					GoldenCloverNode->AtkResNode.ToggleVisibility(enable: false);
					EmptyCloverNode->AtkResNode.ToggleVisibility(enable: true);
					break;
				}
			}
		}
	}

	public unsafe static IEnumerable<nint> GetDutyListItems(nint addonBase)
	{
		return GetDutyListItems((AtkUnitBase*)addonBase);
	}

	public unsafe static IEnumerable<nint> GetDutyListItems(AtkUnitBase* addonBase)
	{
		AtkComponentNode* treeListNode;
		treeListNode = (AtkComponentNode*)addonBase->GetNodeById(52u);
		if (treeListNode == null)
		{
			return new List<nint>();
		}
		AtkComponentBase* treeListNodeComponent;
		treeListNodeComponent = treeListNode->Component;
		if (treeListNodeComponent == null)
		{
			return new List<nint>();
		}
		return from index in Enumerable.Range(61001, 15).Append(6)
			select (nint)Node.GetNodeByID<AtkComponentNode>(treeListNodeComponent->UldManager, (uint)index);
	}

	public unsafe static T* GetListItemNode<T>(nint listItem, uint nodeId) where T : unmanaged
	{
		if (listItem == IntPtr.Zero)
		{
			return null;
		}
		AtkComponentBase* listItemComponent;
		listItemComponent = ((AtkComponentNode*)listItem)->Component;
		if (listItemComponent == null)
		{
			return null;
		}
		return Node.GetNodeByID<T>(listItemComponent->UldManager, nodeId);
	}

	public unsafe static AtkTextNode* GetListItemTextNode(nint listItem)
	{
		return GetListItemNode<AtkTextNode>(listItem, 5u);
	}

	public static string GetListItemFilteredString(nint listItem)
	{
		return TextHelper.FilterNonAlphanumeric(GetListItemString(listItem)).ToLower();
	}

	public unsafe static string GetListItemString(nint listItem)
	{
		if (listItem == IntPtr.Zero)
		{
			return string.Empty;
		}
		AtkTextNode* textNode;
		textNode = GetListItemTextNode(listItem);
		if (textNode == null)
		{
			return string.Empty;
		}
		return textNode->NodeText.ToString();
	}
}
