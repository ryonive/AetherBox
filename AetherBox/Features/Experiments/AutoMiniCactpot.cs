// Could be improved if it checked how many tries a player has left and spend all daily entree's (and maybe make that a option for user to pick)

using System;
using System.Collections.Generic;
using System.Linq;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
namespace AetherBox.Features.Experiments;
internal class AutoMiniCactpot : Feature
{
	private static readonly Dictionary<uint, uint> BlockNodeIds = new Dictionary<uint, uint>
	{
		{ 30u, 0u },
		{ 31u, 1u },
		{ 32u, 2u },
		{ 33u, 3u },
		{ 34u, 4u },
		{ 35u, 5u },
		{ 36u, 6u },
		{ 37u, 7u },
		{ 38u, 8u }
	};

	private static readonly uint[] LineNodeIds = new uint[8] { 28u, 27u, 26u, 21u, 22u, 23u, 24u, 25u };

	private static readonly Dictionary<uint, List<uint>> LineToBlocks = new Dictionary<uint, List<uint>>
	{
		{
			28u,
			new List<uint>(3) { 36u, 37u, 38u }
		},
		{
			27u,
			new List<uint>(3) { 33u, 34u, 35u }
		},
		{
			26u,
			new List<uint>(3) { 30u, 31u, 32u }
		},
		{
			21u,
			new List<uint>(3) { 30u, 34u, 38u }
		},
		{
			22u,
			new List<uint>(3) { 30u, 33u, 36u }
		},
		{
			23u,
			new List<uint>(3) { 31u, 34u, 37u }
		},
		{
			24u,
			new List<uint>(3) { 32u, 35u, 38u }
		},
		{
			25u,
			new List<uint>(3) { 32u, 34u, 36u }
		}
	};

	public override string Name => "Auto Mini Cactpot";

	public override string Description => "Auto play the Mini Cactpot minigame in the Gold Saucer. Needs ezMiniCactpot to play well.";

	public override FeatureType FeatureType => FeatureType.Other;

	public bool Initialized { get; set; }

	public override void Enable()
	{
		base.Enable();
		Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LotteryDaily", OnAddonSetup);
		Initialized = true;
	}

	public override void Disable()
	{
		base.Disable();
		Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
		TaskManager?.Abort();
		Initialized = false;
	}

	private void OnAddonSetup(AddonEvent type, AddonArgs args)
	{
		if (IsEzMiniCactpotInstalled())
		{
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            TaskManager.Enqueue((Func<bool?>)WaitLotteryDailyAddon, (string)null);
            TaskManager.Enqueue((Func<bool?>)ClickRecommendBlock, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickRecommendBlock, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickRecommendBlock, (string)null);
			TaskManager.Enqueue((Func<bool?>)WaitLotteryDailyAddon, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickRecommendLine, (string)null);
			TaskManager.Enqueue((Func<bool?>)WaitLotteryDailyAddon, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickExit, (string)null);
		}
		else
		{
			TaskManager.Enqueue((Func<bool?>)WaitLotteryDailyAddon, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickRandomBlocks, (string)null);
			TaskManager.Enqueue((Func<bool?>)WaitLotteryDailyAddon, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickRandomLine, (string)null);
			TaskManager.Enqueue((Func<bool?>)WaitLotteryDailyAddon, (string)null);
			TaskManager.Enqueue((Func<bool?>)ClickExit, (string)null);
		}
	}

	private unsafe static bool? ClickRandomBlocks()
	{
		if (ECommons.GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && ECommons.GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			AtkUnitBase* ui;
			ui = &addon->AtkUnitBase;
			Random rnd;
			rnd = new Random();
			uint[] array;
			array = BlockNodeIds.Keys.OrderBy((uint x) => rnd.Next()).Take(4).ToArray();
			foreach (uint id in array)
			{
				if (ui->GetComponentNodeById(id) != null)
				{
					Callback.Fire(&addon->AtkUnitBase, true, 1, BlockNodeIds[id]);
				}
			}
			return true;
		}
		return false;
	}

	private unsafe static bool? ClickRandomLine()
	{
		if (ECommons.GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && ECommons.GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			AtkUnitBase* num;
			num = &addon->AtkUnitBase;
			Random rnd;
			rnd = new Random();
			uint selectedLine;
			selectedLine = LineNodeIds.OrderBy((uint x) => rnd.Next()).LastOrDefault();
			List<uint> blocks;
			blocks = LineToBlocks[selectedLine];
			AtkResNodeHelper.ClickAddonCheckBox(num, (AtkComponentCheckBox*)num->GetComponentNodeById(blocks[0]), 5u);
			AtkResNodeHelper.ClickAddonCheckBox(num, (AtkComponentCheckBox*)num->GetComponentNodeById(blocks[1]), 5u);
			AtkResNodeHelper.ClickAddonCheckBox(num, (AtkComponentCheckBox*)num->GetComponentNodeById(blocks[2]), 5u);
			Callback.Fire(&addon->AtkUnitBase, true, 2, 0);
			return true;
		}
		return false;
	}

	private unsafe static bool? ClickExit()
	{
		if (ECommons.GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && ECommons.GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			Callback.Fire(&addon->AtkUnitBase, true, -1);
			return true;
		}
		return false;
	}

	private unsafe static bool? ClickRecommendBlock()
	{
		if (ECommons.GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && ECommons.GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			AtkUnitBase* ui;
			ui = &addon->AtkUnitBase;
			foreach (KeyValuePair<uint, uint> block in BlockNodeIds)
			{
				AtkResNode node;
				node = ui->GetComponentNodeById(block.Key)->AtkResNode;
				if (node.MultiplyBlue == 0 && node.MultiplyRed == 0 && node.MultiplyGreen == 100)
				{
					Callback.Fire(&addon->AtkUnitBase, true, 1, block.Value);
					break;
				}
			}
			return true;
		}
		return false;
	}

	private unsafe static bool? ClickRecommendLine()
	{
		if (ECommons.GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && ECommons.GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			AtkUnitBase* ui;
			ui = &addon->AtkUnitBase;
			uint[] lineNodeIds;
			lineNodeIds = LineNodeIds;
			foreach (uint block in lineNodeIds)
			{
				AtkResNode node;
				node = ui->GetComponentNodeById(block)->AtkResNode;
				AtkComponentRadioButton* button;
				button = (AtkComponentRadioButton*)ui->GetComponentNodeById(block);
				if (node.MultiplyBlue == 0 && node.MultiplyRed == 0 && node.MultiplyGreen == 100)
				{
					List<uint> blocks;
					blocks = LineToBlocks[node.NodeId];
					AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[0]), 5u);
					AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[1]), 5u);
					AtkResNodeHelper.ClickAddonCheckBox(ui, (AtkComponentCheckBox*)ui->GetComponentNodeById(blocks[2]), 5u);
					break;
				}
			}
			Callback.Fire(&addon->AtkUnitBase, true, 2, 0);
			return true;
		}
		return false;
	}

	internal static bool IsEzMiniCactpotInstalled()
	{
		return Svc.PluginInterface.InstalledPlugins.Any((plugin) => (object)plugin != null && plugin.Name == "ezMiniCactpot" && plugin.IsLoaded);
	}

	private unsafe static bool? WaitLotteryDailyAddon()
	{
		if (ECommons.GenericHelpers.TryGetAddonByName<AddonLotteryDaily>("LotteryDaily", out var addon) && ECommons.GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			AtkUnitBase* ui;
			ui = &addon->AtkUnitBase;
			return !ui->GetImageNodeById(4u)->AtkResNode.IsVisible() && !ui->GetTextNodeById(3u)->AtkResNode.IsVisible() && !ui->GetTextNodeById(2u)->AtkResNode.IsVisible();
		}
		return false;
	}
}
