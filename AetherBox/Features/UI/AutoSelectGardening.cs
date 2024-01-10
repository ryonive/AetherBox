// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Features.UI.AutoSelectGardening
using System;
using System.Collections.Generic;
using System.Linq;
using AetherBox.Features;
using AetherBox.Features.UI;
using AetherBox.FeaturesSetup;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Components;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
namespace AetherBox.Features.UI;
public class AutoSelectGardening : Feature
{
	public class Configs : FeatureConfig
	{
		public uint SelectedSoil;

		public uint SelectedSeed;

		public bool IncludeFertilzing;

		public uint SelectedFertilizer;

		public bool AutoConfirm;

		public bool Fallback;

		public bool OnlyShowInventoryItems;
	}

	public override string Name => "Auto-select Gardening Soil/Seeds";

	public override string Description => "Automatically fill in gardening windows with seeds and soil.";

	public override FeatureType FeatureType => FeatureType.UI;

	public Dictionary<uint, Item> Seeds { get; set; }

	public Dictionary<uint, Item> Soils { get; set; }

	public Dictionary<uint, Item> Fertilizers { get; set; }

	public Dictionary<uint, Addon> AddonText { get; set; }

	public Configs Config { get; private set; }

	private bool Fertilized { get; set; }

	private List<int> SlotsFilled { get; set; } = new List<int>();


	protected unsafe override DrawConfigDelegate DrawConfigTree => delegate
	{
		bool flag = false;
		if (ImGui.Checkbox("Show Only Inventory Items", ref Config.OnlyShowInventoryItems))
		{
			flag = true;
		}
		KeyValuePair<uint, Item>[] array = (Config.OnlyShowInventoryItems ? Soils.Where((KeyValuePair<uint, Item> x) => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0).ToArray() : Soils.ToArray());
		KeyValuePair<uint, Item>[] array2 = (Config.OnlyShowInventoryItems ? Seeds.Where((KeyValuePair<uint, Item> x) => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0).ToArray() : Seeds.ToArray());
		KeyValuePair<uint, Item>[] array3 = (Config.OnlyShowInventoryItems ? Fertilizers.Where((KeyValuePair<uint, Item> x) => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0).ToArray() : Fertilizers.ToArray());
		string preview_value = ((Config.SelectedSoil == 0) ? "" : Soils[Config.SelectedSoil].Name.ExtractText());
		if (ImGui.BeginCombo("Soil", preview_value))
		{
			if (ImGui.Selectable("", Config.SelectedSoil == 0))
			{
				Config.SelectedSoil = 0u;
				flag = true;
			}
			KeyValuePair<uint, Item>[] array4 = array;
			for (int i = 0; i < array4.Length; i++)
			{
				KeyValuePair<uint, Item> keyValuePair = array4[i];
				if (ImGui.Selectable(keyValuePair.Value.Name.ExtractText(), Config.SelectedSoil == keyValuePair.Key))
				{
					Config.SelectedSoil = keyValuePair.Key;
					flag = true;
				}
			}
			ImGui.EndCombo();
		}
		string preview_value2 = ((Config.SelectedSeed == 0) ? "" : Seeds[Config.SelectedSeed].Name.ExtractText());
		if (ImGui.BeginCombo("Seed", preview_value2))
		{
			if (ImGui.Selectable("", Config.SelectedSeed == 0))
			{
				Config.SelectedSeed = 0u;
				flag = true;
			}
			KeyValuePair<uint, Item>[] array4 = array2;
			for (int i = 0; i < array4.Length; i++)
			{
				KeyValuePair<uint, Item> keyValuePair2 = array4[i];
				if (ImGui.Selectable(keyValuePair2.Value.Name.ExtractText(), Config.SelectedSeed == keyValuePair2.Key))
				{
					Config.SelectedSeed = keyValuePair2.Key;
					flag = true;
				}
			}
			ImGui.EndCombo();
		}
		ImGui.Checkbox("Include Fertilizing", ref Config.IncludeFertilzing);
		if (Config.IncludeFertilzing)
		{
			string preview_value3 = ((Config.SelectedFertilizer == 0) ? "" : Fertilizers[Config.SelectedFertilizer].Name.ExtractText());
			if (ImGui.BeginCombo("Fertilizer", preview_value3))
			{
				if (ImGui.Selectable("", Config.SelectedFertilizer == 0))
				{
					Config.SelectedFertilizer = 0u;
					flag = true;
				}
				KeyValuePair<uint, Item>[] array4 = array3;
				for (int i = 0; i < array4.Length; i++)
				{
					KeyValuePair<uint, Item> keyValuePair3 = array4[i];
					if (ImGui.Selectable(keyValuePair3.Value.Name.ExtractText(), Config.SelectedFertilizer == keyValuePair3.Key))
					{
						Config.SelectedFertilizer = keyValuePair3.Key;
						flag = true;
					}
				}
				ImGui.EndCombo();
			}
		}
		if (ImGui.Checkbox("Soil/Seed Fallback", ref Config.Fallback))
		{
			flag = true;
		}
		ImGuiComponents.HelpMarker("When enabled, this will select the first soil/seed found in your inventory if the\nprimary ones chosen are not found.");
		if (ImGui.Checkbox("Auto Confirm", ref Config.AutoConfirm))
		{
			flag = true;
		}
		if (flag)
		{
			SaveConfig(Config);
		}
	};

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		Seeds = (from x in Svc.Data.GetExcelSheet<Item>()
			where x.ItemUICategory.Row == 82 && x.FilterGroup == 20
			select x).ToDictionary((Item x) => x.RowId, (Item x) => x);
		Soils = (from x in Svc.Data.GetExcelSheet<Item>()
			where x.ItemUICategory.Row == 82 && x.FilterGroup == 21
			select x).ToDictionary((Item x) => x.RowId, (Item x) => x);
		Fertilizers = (from x in Svc.Data.GetExcelSheet<Item>()
			where x.ItemUICategory.Row == 82 && x.FilterGroup == 22
			select x).ToDictionary((Item x) => x.RowId, (Item x) => x);
		AddonText = Svc.Data.GetExcelSheet<Addon>().ToDictionary((Addon x) => x.RowId, (Addon x) => x);
		Svc.Framework.Update += RunFeature;
		base.Enable();
	}

	private unsafe void RunFeature(IFramework framework)
	{
		if (Svc.ClientState.LocalPlayer == null)
		{
			return;
		}
		if (Config.IncludeFertilzing && Svc.GameGui.GetAddonByName("InventoryExpansion") != IntPtr.Zero && !Fertilized)
		{
			if (Config.SelectedFertilizer != 0)
			{
				AtkUnitBase* addon2 = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InventoryExpansion");
				if (addon2->IsVisible)
				{
					if (addon2->AtkValuesCount <= 5)
					{
						return;
					}
					if (MemoryHelper.ReadSeStringNullTerminated(new IntPtr(addon2->AtkValues[5].String)).ExtractText() == AddonText[6417u].Text.ExtractText())
					{
						InventoryManager* intPtr = InventoryManager.Instance();
						InventoryContainer* inv1 = intPtr->GetInventoryContainer(InventoryType.Inventory1);
						InventoryContainer* inv3 = intPtr->GetInventoryContainer(InventoryType.Inventory2);
						InventoryContainer* inv5 = intPtr->GetInventoryContainer(InventoryType.Inventory3);
						InventoryContainer* inv7 = intPtr->GetInventoryContainer(InventoryType.Inventory4);
						InventoryContainer*[] array = new InventoryContainer*[4] { inv1, inv3, inv5, inv7 };
						foreach (InventoryContainer* cont in array)
						{
							for (int i = 0; i < cont->Size; i++)
							{
								if (cont->GetInventorySlot(i)->ItemID == Config.SelectedFertilizer)
								{
									cont->GetInventorySlot(i);
									AgentInventoryContext.Instance()->OpenForItemSlot(cont->Type, i, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID());
									AtkUnitBase* contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
									if (contextMenu != null)
									{
										Callback.Fire(contextMenu, true, 0, 0, 0, 0, 0);
										Fertilized = true;
									}
									return;
								}
							}
						}
						return;
					}
				}
			}
		}
		else
		{
			Fertilized = false;
		}
		if (Svc.GameGui.GetAddonByName("HousingGardening") != IntPtr.Zero)
		{
			if (Config.SelectedSeed == 0 && Config.SelectedSoil == 0)
			{
				return;
			}
			List<uint> invSoil = (from x in Soils
				where InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0
				select x.Key).ToList();
			List<uint> invSeeds = (from x in Seeds
				where InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0
				select x.Key).ToList();
			InventoryManager* intPtr2 = InventoryManager.Instance();
			InventoryContainer* inv2 = intPtr2->GetInventoryContainer(InventoryType.Inventory1);
			InventoryContainer* inv4 = intPtr2->GetInventoryContainer(InventoryType.Inventory2);
			InventoryContainer* inv6 = intPtr2->GetInventoryContainer(InventoryType.Inventory3);
			InventoryContainer* inv8 = intPtr2->GetInventoryContainer(InventoryType.Inventory4);
			InventoryContainer*[] container = new InventoryContainer*[4] { inv2, inv4, inv6, inv8 };
			int soilIndex = 0;
			InventoryContainer*[] array = container;
			int n = 0;
			while (true)
			{
				if (n < array.Length)
				{
					InventoryContainer* cont4 = array[n];
					int l;
					for (l = 0; l < cont4->Size; l++)
					{
						if (invSoil.Any((uint x) => cont4->GetInventorySlot(l)->ItemID == x))
						{
							if (cont4->GetInventorySlot(l)->ItemID == Config.SelectedSoil)
							{
								goto end_IL_03f5;
							}
							soilIndex++;
						}
					}
					n++;
					continue;
				}
				if (!Config.Fallback)
				{
					break;
				}
				soilIndex = 0;
				array = container;
				foreach (InventoryContainer* cont5 in array)
				{
					int m;
					for (m = 0; m < cont5->Size; m++)
					{
						if (invSoil.Any((uint x) => cont5->GetInventorySlot(m)->ItemID == x))
						{
							if (cont5->GetInventorySlot(m)->ItemID == invSoil[0])
							{
								goto end_IL_03f5;
							}
							soilIndex++;
						}
					}
				}
				break;
				continue;
				end_IL_03f5:
				break;
			}
			int seedIndex = 0;
			array = container;
			n = 0;
			while (true)
			{
				if (n < array.Length)
				{
					InventoryContainer* cont2 = array[n];
					int j;
					for (j = 0; j < cont2->Size; j++)
					{
						if (invSeeds.Any((uint x) => cont2->GetInventorySlot(j)->ItemID == x))
						{
							if (cont2->GetInventorySlot(j)->ItemID == Config.SelectedSeed)
							{
								goto end_IL_05ab;
							}
							seedIndex++;
						}
					}
					n++;
					continue;
				}
				if (!Config.Fallback)
				{
					break;
				}
				seedIndex = 0;
				array = container;
				foreach (InventoryContainer* cont3 in array)
				{
					int k;
					for (k = 0; k < cont3->Size; k++)
					{
						if (invSeeds.Any((uint x) => cont3->GetInventorySlot(k)->ItemID == x))
						{
							if (cont3->GetInventorySlot(k)->ItemID == invSeeds[0])
							{
								goto end_IL_05ab;
							}
							seedIndex++;
						}
					}
				}
				break;
				continue;
				end_IL_05ab:
				break;
			}
			AtkUnitBase* addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("HousingGardening");
			if (TaskManager.IsBusy)
			{
				return;
			}
			if (soilIndex != -1)
			{
				if (SlotsFilled.Contains(1))
				{
					TaskManager.Abort();
				}
				if (SlotsFilled.Contains(1))
				{
					return;
				}
				TaskManager.DelayNext("Gardening1", 100);
				TaskManager.Enqueue(() => TryClickItem(addon, 1, soilIndex));
			}
			if (seedIndex != -1)
			{
				if (SlotsFilled.Contains(2))
				{
					TaskManager.Abort();
				}
				if (SlotsFilled.Contains(2))
				{
					return;
				}
				TaskManager.DelayNext("Gardening2", 100);
				TaskManager.Enqueue(() => TryClickItem(addon, 2, seedIndex));
			}
			if (Config.AutoConfirm)
			{
				TaskManager.DelayNext("Confirming", 100);
				TaskManager.Enqueue(delegate
				{
					Callback.Fire(addon, false, 0, 0, 0, 0, 0);
				}, 300, abortOnTimeout: false);
				TaskManager.Enqueue(() => ConfirmYesNo(), 300, abortOnTimeout: false);
			}
		}
		else
		{
			SlotsFilled.Clear();
			TaskManager.Abort();
		}
	}

	private unsafe bool? TryClickItem(AtkUnitBase* addon, int i, int itemIndex)
	{
		if (SlotsFilled.Contains(i))
		{
			return true;
		}
		AtkUnitBase* contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu");
		if (contextMenu == null || !contextMenu->IsVisible)
		{
			int slot = i - 1;
			Svc.Log.Debug($"{slot}");
			AtkValue* values = stackalloc AtkValue[5];
			*values = new AtkValue
			{
				Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
				Int = 2
			};
			values[1] = new AtkValue
			{
				Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
				UInt = (uint)slot
			};
			values[2] = new AtkValue
			{
				Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
				Int = 0
			};
			values[3] = new AtkValue
			{
				Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
				Int = 0
			};
			values[4] = new AtkValue
			{
				Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
				UInt = 1u
			};
			addon->FireCallback(5, values, null);
			CloseItemDetail();
			return false;
		}
		uint value = ((i == 1) ? 27405u : 27451u);
		AtkValue* values2 = stackalloc AtkValue[5];
		*values2 = new AtkValue
		{
			Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
			Int = 0
		};
		values2[1] = new AtkValue
		{
			Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
			Int = itemIndex
		};
		values2[2] = new AtkValue
		{
			Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
			UInt = value
		};
		values2[3] = new AtkValue
		{
			Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
			UInt = 0u
		};
		values2[4] = new AtkValue
		{
			Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
			UInt = 0u
		};
		contextMenu->FireCallback(5, values2, (void*)2476827163393uL);
		Svc.Log.Debug($"Filled slot {i}");
		SlotsFilled.Add(i);
		return true;
	}

	private unsafe bool CloseItemDetail()
	{
		AtkUnitBase* itemDetail = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ItemDetail");
		if (itemDetail == null || !itemDetail->IsVisible)
		{
			return false;
		}
		AtkValue* values = stackalloc AtkValue[1];
		*values = new AtkValue
		{
			Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
			Int = -1
		};
		itemDetail->FireCallback(1, values, null);
		return true;
	}

	internal unsafe static bool ConfirmYesNo()
	{
		if (Svc.Condition[ConditionFlag.Occupied39])
		{
			return false;
		}
		AtkUnitBase* hg = (AtkUnitBase*)Svc.GameGui.GetAddonByName("HousingGardening");
		if (hg == null)
		{
			return false;
		}
		if (hg->IsVisible && GenericHelpers.TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) && addon->AtkUnitBase.IsVisible && addon->YesButton->IsEnabled && addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
		{
			new ClickSelectYesNo((nint)addon).Yes();
			return true;
		}
		return false;
	}

	public override void Disable()
	{
		SaveConfig(Config);
		Seeds = null;
		Soils = null;
		AddonText = null;
		Fertilizers = null;
		Svc.Framework.Update -= RunFeature;
		SlotsFilled.Clear();
		base.Disable();
	}
}
