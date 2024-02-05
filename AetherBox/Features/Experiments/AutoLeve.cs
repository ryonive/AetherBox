using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers.NPCLocations;
using AetherBox.UI;
using ClickLib;
using ClickLib.Clicks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
namespace AetherBox.Features.Experiments;
public class AutoLeve : Feature
{
	private Overlays overlay;

	private static Dictionary<uint, (string, uint)> LeveQuests = new Dictionary<uint, (string, uint)>();

	private static readonly HashSet<uint> QualifiedLeveCategories = new HashSet<uint> { 9u, 10u, 11u, 12u, 13u, 14u, 15u, 16u };

	private static (uint, string, uint)? SelectedLeve;

	private static uint LeveMeteDataId;

	private static uint LeveReceiverDataId;

	private static int Allowances;

	private static string SearchString = string.Empty;

	private readonly Dictionary<uint, NpcLocation> npcLocations = new Dictionary<uint, NpcLocation>();

	private readonly List<uint> leveNPCs = new List<uint>();

	private ExcelSheet<ENpcResident> eNpcResidents;

	private ExcelSheet<Map> maps;

	private ExcelSheet<TerritoryType> territoryType;

	private static bool IsOnProcessing;

	public override string Name => "Auto Leve";

	public override string Description => "Hand in leves on repeat";

	public override FeatureType FeatureType => FeatureType.Other;

	protected override DrawConfigDelegate DrawConfigTree => delegate
	{
		if (ImGui.Button("debug"))
		{
			try
			{
				foreach (KeyValuePair<uint, NpcLocation> current in npcLocations)
				{
					Svc.Log.Info($"{current.Key}: {eNpcResidents.GetRow(current.Key).Singular} - {current.Value.TerritoryType}");
				}
			}
			catch (Exception ex)
			{
				Svc.Log.Error(ex.ToString());
			}
		}
	};

	public override void Enable()
	{
		base.Enable();
		overlay = new Overlays(this);
		eNpcResidents = Svc.Data.GetExcelSheet<ENpcResident>();
		maps = Svc.Data.GetExcelSheet<Map>();
		territoryType = Svc.Data.GetExcelSheet<TerritoryType>();
		BuildLeveNPCs();
		BuildNpcLocation();
		FilterLocations();
		Svc.ClientState.TerritoryChanged += OnZoneChanged;
	}

	public override void Disable()
	{
		base.Disable();
		P.Ws.RemoveWindow(overlay);
		Svc.ClientState.TerritoryChanged -= OnZoneChanged;
		EndProcessHandler();
	}

	private void OnZoneChanged(ushort obj)
	{
		LeveQuests.Clear();
	}

	private static float GetDistanceToNpc(int npcId, out Dalamud.Game.ClientState.Objects.Types.GameObject? o)
	{
		foreach (Dalamud.Game.ClientState.Objects.Types.GameObject obj in Svc.Objects)
		{
			if (obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc && obj is Character c && Marshal.ReadInt32(obj.Address + 128) == npcId)
			{
				o = obj;
				return Vector3.Distance(Svc.ClientState.LocalPlayer?.Position ?? Vector3.Zero, c.Position);
			}
		}
		o = null;
		return float.MaxValue;
	}

	public unsafe override void Draw()
	{
		if (GameMain.Instance()->CurrentContentFinderConditionId != 0 || npcLocations.Values.ToList().All((NpcLocation x) => x.TerritoryType != Svc.ClientState.TerritoryType))
		{
			return;
		}
		foreach (KeyValuePair<uint, NpcLocation> item in npcLocations.Where((KeyValuePair<uint, NpcLocation> x) => x.Value.TerritoryType == Svc.ClientState.TerritoryType).ToDictionary((KeyValuePair<uint, NpcLocation> x) => x.Key, (KeyValuePair<uint, NpcLocation> x) => x.Value))
		{
			if (GetDistanceToNpc((int)item.Key, out Dalamud.Game.ClientState.Objects.Types.GameObject _) > 5f)
			{
				return;
			}
		}
		try
		{
			if (ImGui.Begin("AutoLeve"))
			{
				ImGui.Text("yippee");
				using (ImRaii.Disabled(IsOnProcessing))
				{
					ImGui.Text("SelectedLeve");
					ImGui.SameLine();
					ImGui.SetNextItemWidth(400f);
					using (ImRaii.Combo("##SelectedLeve", (!SelectedLeve.HasValue) ? "" : $"{SelectedLeve.Value.Item1} | {SelectedLeve.Value.Item2}"))
					{
						if (ImGui.Button("GetAreaLeveData"))
						{
							GetRecentLeveQuests();
						}
						ImGui.SetNextItemWidth(-1f);
						ImGui.SameLine();
						ImGui.InputText("##AutoLeveQuests-SearchLeveQuest", ref SearchString, 100u);
						ImGui.Separator();
						if (LeveQuests.Any())
						{
							foreach (KeyValuePair<uint, (string, uint)> leveToSelect in LeveQuests)
							{
								if (string.IsNullOrEmpty(SearchString) || leveToSelect.Value.Item1.Contains(SearchString, StringComparison.OrdinalIgnoreCase) || leveToSelect.Key.ToString().Contains(SearchString, StringComparison.OrdinalIgnoreCase))
								{
									if (ImGui.Selectable($"{leveToSelect.Key} | {leveToSelect.Value.Item1}"))
									{
										SelectedLeve = (leveToSelect.Key, leveToSelect.Value.Item1, leveToSelect.Value.Item2);
									}
									if (SelectedLeve.HasValue && ImGui.IsWindowAppearing() && SelectedLeve.Value.Item1 == leveToSelect.Key)
									{
										ImGui.SetScrollHereY();
									}
								}
							}
						}
					}
					ImGui.SameLine();
					using (ImRaii.Disabled(!SelectedLeve.HasValue || LeveMeteDataId == LeveReceiverDataId || LeveMeteDataId == 0 || LeveReceiverDataId == 0))
					{
						if (ImGui.Button("Start"))
						{
							IsOnProcessing = true;
							Svc.AddonLifeCycle.RegisterListener(AddonEvent.PostDraw, "SelectYesno", AlwaysYes);
							TaskManager.Enqueue((Func<bool?>)InteractWithMete, (string)null);
						}
					}
				}
				ImGui.SameLine();
				if (ImGui.Button("Stop"))
				{
					EndProcessHandler();
				}
				using (ImRaii.Disabled(IsOnProcessing))
				{
					if (ImGui.Button("ObtainLevemeteID"))
					{
						GetCurrentTargetDataID(out LeveMeteDataId);
					}
					ImGui.SameLine();
					ImGui.Text(LeveMeteDataId.ToString());
					ImGui.SameLine();
					ImGui.Spacing();
					ImGui.SameLine();
					if (ImGui.Button("ObtainLeveClientID"))
					{
						GetCurrentTargetDataID(out LeveReceiverDataId);
					}
					ImGui.SameLine();
					ImGui.Text(LeveReceiverDataId.ToString());
				}
			}
			ImGui.End();
		}
		catch (Exception e)
		{
			Svc.Log.Error(e.ToString());
		}
	}

	private void EndProcessHandler()
	{
		TaskManager?.Abort();
		Svc.AddonLifeCycle.UnregisterListener(AlwaysYes);
		IsOnProcessing = false;
	}

	private static void AlwaysYes(AddonEvent type, AddonArgs args)
	{
		Click.SendClick("select_yes");
	}

	private static void GetRecentLeveQuests()
	{
		uint? currentTerritoryPlaceNameId;
		currentTerritoryPlaceNameId = Svc.Data.GetExcelSheet<TerritoryType>().FirstOrDefault((TerritoryType y) => y.RowId == Svc.ClientState.TerritoryType)?.PlaceName.RawRow.RowId;
		if (currentTerritoryPlaceNameId.HasValue)
		{
			LeveQuests = (from x in Svc.Data.GetExcelSheet<Leve>()
				where !string.IsNullOrEmpty(x.Name.RawString) && QualifiedLeveCategories.Contains(x.ClassJobCategory.RawRow.RowId) && x.PlaceNameIssued.RawRow.RowId == currentTerritoryPlaceNameId.Value
				select x).ToDictionary((Leve x) => x.RowId, (Leve x) => (RawString: x.Name.RawString, RowId: x.ClassJobCategory.RawRow.RowId));
			Svc.Log.Debug($"Obtained {LeveQuests.Count} leve quests");
		}
	}

	private static void GetCurrentTargetDataID(out uint targetDataId)
	{
		Dalamud.Game.ClientState.Objects.Types.GameObject currentTarget;
		currentTarget = Svc.Targets.Target;
		targetDataId = ((!(currentTarget == null)) ? currentTarget.DataId : 0u);
	}

	private unsafe bool? InteractWithMete()
	{
		if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			int i;
			for (i = 1; i < 8 && !addon->PopupMenu.PopupMenu.List->AtkComponentBase.UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText.ExtractText().Contains("Finish"); i++)
			{
			}
			new ClickSelectString().SelectItem((ushort)(i - 1));
		}
		if (GenericHelpers.IsOccupied())
		{
			return false;
		}
		if (FindObjectToInteractWith(LeveMeteDataId, out var foundObject))
		{
			TargetSystem.Instance()->InteractWithObject(foundObject);
			TaskManager.Enqueue((Func<bool?>)ClickCraftingLeve, (string)null);
			return true;
		}
		return false;
	}

	private unsafe static bool FindObjectToInteractWith(uint dataId, out FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* foundObject)
	{
		foreach (Dalamud.Game.ClientState.Objects.Types.GameObject obj in Svc.Objects.Where((Dalamud.Game.ClientState.Objects.Types.GameObject o) => o.DataId == dataId))
		{
			if (obj.IsTargetable)
			{
				foundObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Address;
				return true;
			}
		}
		foundObject = null;
		return false;
	}

	private unsafe bool? ClickCraftingLeve()
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectString", out var addon) && GenericHelpers.IsAddonReady(addon))
		{
			new ClickSelectString().SelectItem2();
			TaskManager.Enqueue((Func<bool?>)ClickLeveQuest, (string)null);
			return true;
		}
		return false;
	}

	private unsafe bool? ClickLeveQuest()
	{
		if (!SelectedLeve.HasValue)
		{
			return false;
		}
		if (GenericHelpers.TryGetAddonByName<AddonGuildLeve>("GuildLeve", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			Allowances = (int.TryParse(addon->AtkComponentBase290->UldManager.NodeList[2]->GetAsAtkTextNode()->NodeText.ExtractText(), out var result) ? result : 0);
			if (Allowances <= 0)
			{
				EndProcessHandler();
			}
			if (GenericHelpers.TryGetAddonByName<AddonJournalDetail>("JournalDetail", out var addon2) && GenericHelpers.IsAddonReady(&addon2->AtkUnitBase))
			{
				Callback.Fire(&addon->AtkUnitBase, true, 3, (int)SelectedLeve.Value.Item1);
				TaskManager.Enqueue((Func<bool?>)ClickExit, (string)null);
				return true;
			}
		}
		return false;
	}

	internal unsafe bool? ClickExit()
	{
		if (GenericHelpers.TryGetAddonByName<AddonGuildLeve>("GuildLeve", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			AtkUnitBase* ui;
			ui = &addon->AtkUnitBase;
			Callback.Fire(ui, true, -2);
			Callback.Fire(ui, true, -1);
			TaskManager.Enqueue((Func<bool?>)ClickSelectStringExit, (string)null);
			ui->Close(fireCallback: true);
			return true;
		}
		return false;
	}

	private unsafe bool? ClickSelectStringExit()
	{
		if (!SelectedLeve.HasValue)
		{
			return false;
		}
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectString", out var addon) && GenericHelpers.IsAddonReady(addon))
		{
			int i;
			for (i = 1; i < 8 && !((AddonSelectString*)addon)->PopupMenu.PopupMenu.List->AtkComponentBase.UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText.ExtractText().Contains("取消"); i++)
			{
			}
			new ClickSelectString().SelectItem((ushort)(i - 1));
			TaskManager.Enqueue((Func<bool?>)InteractWithReceiver, (string)null);
			addon->Close(fireCallback: true);
			return true;
		}
		return false;
	}

	private unsafe bool? InteractWithReceiver()
	{
		if (GenericHelpers.IsOccupied())
		{
			return false;
		}
		if (FindObjectToInteractWith(LeveReceiverDataId, out var foundObject))
		{
			TargetSystem.Instance()->InteractWithObject(foundObject);
			Span<LeveWork> levesSpan = QuestManager.Instance()->LeveQuestsSpan;
			int qualifiedCount;
			qualifiedCount = 0;
			for (int i = 0; i < levesSpan.Length; i++)
			{
				if (LeveQuests.ContainsKey(levesSpan[i].LeveId))
				{
					qualifiedCount++;
				}
			}
			TaskManager.Enqueue((qualifiedCount > 1) ? new Func<bool?>(ClickSelectQuest) : new Func<bool?>(InteractWithMete));
			return true;
		}
		return false;
	}

	private unsafe bool? ClickSelectQuest()
	{
		if (!SelectedLeve.HasValue)
		{
			return false;
		}
		if (GenericHelpers.TryGetAddonByName<AddonSelectIconString>("SelectIconString", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
		{
			int i;
			for (i = 1; i < 8 && !(addon->PopupMenu.PopupMenu.List->AtkComponentBase.UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText.ExtractText() == SelectedLeve.Value.Item2); i++)
			{
			}
			new ClickSelectIconString().SelectItem((ushort)(i - 1));
			TaskManager.Enqueue((Func<bool?>)InteractWithMete, (string)null);
			return true;
		}
		return false;
	}

	private void BuildLeveNPCs()
	{
		Parallel.ForEach((IEnumerable<GuildleveAssignment>)Svc.Data.GetExcelSheet<GuildleveAssignment>(), (Action<GuildleveAssignment>)delegate(GuildleveAssignment assigner)
		{
			if (Svc.Data.Excel.GetSheet<ENpcBase>().TryGetFirst((ENpcBase x) => x.ENpcData.Any((uint y) => y == assigner.RowId), out var value))
			{
				lock (leveNPCs)
				{
					leveNPCs.Add(value.RowId);
				}
			}
		});
	}

	private void FilterLocations()
	{
		npcLocations.Keys.ToList().ForEach(delegate(uint key)
		{
			if (!leveNPCs.Contains(key))
			{
				npcLocations.Remove(key);
			}
		});
	}

	private void BuildNpcLocation()
	{
		foreach (TerritoryType sTerritoryType in territoryType)
		{
			string bg;
			bg = sTerritoryType.Bg.ToString();
			if (!string.IsNullOrEmpty(bg))
			{
				string lgbFileName;
				lgbFileName = "bg/" + bg.Substring(0, bg.IndexOf("/level/", StringComparison.Ordinal) + 1) + "level/planevent.lgb";
				LgbFile sLgbFile;
				sLgbFile = Svc.Data.GetFile<LgbFile>(lgbFileName);
				if (sLgbFile != null)
				{
					ParseLgbFile(sLgbFile, sTerritoryType);
				}
			}
		}
		foreach (Level level in Svc.Data.GetExcelSheet<Level>())
		{
			if (level.Type == 8 && !npcLocations.ContainsKey(level.Object) && level.Territory.Value != null)
			{
				npcLocations.Add(level.Object, new NpcLocation(level.X, level.Z, level.Territory.Value));
			}
		}
		TerritoryType corrected;
		corrected = territoryType.GetRow(698u);
		uint[] array;
		array = new uint[7] { 1004418u, 1006747u, 1002299u, 1002281u, 1001766u, 1001945u, 1001821u };
		foreach (uint key in array)
		{
			if (npcLocations.ContainsKey(key))
			{
				npcLocations[key].TerritoryExcel = corrected;
			}
		}
		ManualItemCorrections.ApplyCorrections(npcLocations);
	}

	public void ParseLgbFile(LgbFile lgbFile, TerritoryType sTerritoryType, uint? npcId = null)
	{
		LayerCommon.Layer[] layers;
		layers = lgbFile.Layers;
		for (int j = 0; j < layers.Length; j++)
		{
			LayerCommon.InstanceObject[] instanceObjects;
			instanceObjects = layers[j].InstanceObjects;
			for (int k = 0; k < instanceObjects.Length; k++)
			{
				LayerCommon.InstanceObject instanceObject;
				instanceObject = instanceObjects[k];
				if (instanceObject.AssetType != LayerEntryType.EventNPC)
				{
					continue;
				}
				uint npcRowId;
				npcRowId = ((LayerCommon.ENPCInstanceObject)(object)instanceObject.Object).ParentData.ParentData.BaseId;
				if (npcRowId == 0 || (npcId.HasValue && npcRowId != npcId) || (!npcId.HasValue && npcLocations.ContainsKey(npcRowId)))
				{
					continue;
				}
				byte mapId;
				mapId = eNpcResidents.GetRow(npcRowId).Map;
				try
				{
					Map map;
					map = maps.First((Map i) => i.TerritoryType.Value == sTerritoryType && i.MapIndex == mapId);
					npcLocations.Add(npcRowId, new NpcLocation(instanceObject.Transform.Translation.X, instanceObject.Transform.Translation.Z, sTerritoryType, map.RowId));
				}
				catch (InvalidOperationException)
				{
					npcLocations.Add(npcRowId, new NpcLocation(instanceObject.Transform.Translation.X, instanceObject.Transform.Translation.Z, sTerritoryType));
				}
			}
		}
	}
}
