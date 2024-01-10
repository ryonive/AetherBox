// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Features.UI.AutoNumerics
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AetherBox;
using AetherBox.Features;
using AetherBox.Features.UI;
using AetherBox.FeaturesSetup;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
namespace AetherBox.Features.UI;
public class AutoNumerics : Feature
{
	public class Configs : FeatureConfig
	{
		public bool WorkOnTrading;

		public int TradeMinOrMax = -1;

		public bool TradeExcludeSplit;

		public bool TradeConfirm;

		public bool WorkOnFCChest;

		public int FCChestMinOrMax = 1;

		public bool FCExcludeSplit = true;

		public bool FCChestConfirm;

		public bool WorkOnRetainers;

		public int RetainersMinOrMax = -1;

		public bool RetainerExcludeSplit;

		public bool RetainersConfirm;

		public bool WorkOnInventory;

		public int InventoryMinOrMax = -1;

		public bool InventoryExcludeSplit;

		public bool InventoryConfirm;

		public bool WorkOnMail;

		public int MailMinOrMax = -1;

		public bool MailExcludeSplit;

		public bool MailConfirm;

		public bool WorkOnTransmute;

		public int TransmuteMinOrMax;

		public bool TransmuteExcludeSplit = true;

		public bool TransmuteConfirm = true;

		public bool WorkOnVentures;

		public int VentureMinOrMax = 1;

		public bool VentureExcludeSplit = true;

		public bool VentureConfirm;
	}

	private readonly string splitText = (from x in Svc.Data.GetExcelSheet<Addon>()
		where x.RowId == 533
		select x).First().Text.RawString;

	private bool hasDisabled;

	public override string Name => "Auto-Fill Numeric Dialogs";

	public override string Description => "Automatically fills any numeric input dialog boxes. Works on a whitelist system. Hold shift when opening a numeric dialog to disable.";

	public override FeatureType FeatureType => FeatureType.UI;

	public Configs Config { get; private set; }

	protected override DrawConfigDelegate DrawConfigTree => delegate
	{
		DrawConfigsForAddon("Trading", ref Config.WorkOnTrading, ref Config.TradeMinOrMax, ref Config.TradeExcludeSplit, ref Config.TradeConfirm);
		DrawConfigsForAddon("FC Chests", ref Config.WorkOnFCChest, ref Config.FCChestMinOrMax, ref Config.FCExcludeSplit, ref Config.FCChestConfirm);
		DrawConfigsForAddon("Retainers", ref Config.WorkOnRetainers, ref Config.RetainersMinOrMax, ref Config.RetainerExcludeSplit, ref Config.RetainersConfirm);
		DrawConfigsForAddon("Mail", ref Config.WorkOnMail, ref Config.MailMinOrMax, ref Config.MailExcludeSplit, ref Config.MailConfirm);
		DrawConfigsForAddon("Materia Transmutation", ref Config.WorkOnTransmute, ref Config.TransmuteMinOrMax, ref Config.TransmuteExcludeSplit, ref Config.TransmuteConfirm);
		DrawConfigsForAddon("Venture Purchase", ref Config.WorkOnVentures, ref Config.VentureMinOrMax, ref Config.VentureExcludeSplit, ref Config.VentureConfirm);
	};

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		Common.OnAddonSetup += FillRegularNumeric;
		Common.OnAddonSetup += FillVentureNumeric;
		base.Enable();
	}

	private unsafe void FillRegularNumeric(SetupAddonArgs obj)
	{
		if (obj.AddonName != "InputNumeric" || ImGui.GetIO().KeyShift)
		{
			return;
		}
		try
		{
			int minValue = obj.Addon->AtkValues[2].Int;
			int maxValue = obj.Addon->AtkValues[3].Int;
			obj.Addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
			_ = obj.Addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
			if (Config.WorkOnTrading && Svc.Condition[ConditionFlag.TradeOpen])
			{
				TryFill(obj.Addon, minValue, maxValue, Config.TradeMinOrMax, Config.TradeExcludeSplit, Config.TradeConfirm);
			}
			if (Config.WorkOnFCChest && InFcChest())
			{
				TryFill(obj.Addon, minValue, maxValue, Config.FCChestMinOrMax, Config.FCExcludeSplit, Config.FCChestConfirm);
			}
			if (Config.WorkOnRetainers && Svc.Condition[ConditionFlag.OccupiedSummoningBell] && !InFcChest())
			{
				TryFill(obj.Addon, minValue, maxValue, Config.RetainersMinOrMax, Config.RetainerExcludeSplit, Config.RetainersConfirm);
			}
			if (Config.WorkOnMail && InMail())
			{
				TryFill(obj.Addon, minValue, maxValue, Config.MailMinOrMax, Config.MailExcludeSplit, Config.MailConfirm);
			}
			if (Config.WorkOnTransmute && InTransmute())
			{
				TryFill(obj.Addon, minValue, maxValue, Config.TransmuteMinOrMax, Config.TransmuteExcludeSplit, Config.TransmuteConfirm);
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private unsafe void TryFill(AtkUnitBase* numeric, int minValue, int maxValue, int minOrMax, bool excludeSplit, bool autoConfirm)
	{
		AtkTextNode* numericTextNode = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
		AtkResNode* numericResNode = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];
		if (excludeSplit && IsSplitAddon())
		{
			return;
		}
		if (minOrMax == 0)
		{
			TaskManager.Enqueue(delegate
			{
				numericTextNode->SetText(ConvertToByte(minValue));
			});
			if (autoConfirm)
			{
				TaskManager.Enqueue(delegate
				{
					Callback.Fire(numeric, true, minValue);
				});
			}
		}
		if (minOrMax == 1)
		{
			TaskManager.Enqueue(delegate
			{
				numericTextNode->SetText(ConvertToByte(maxValue));
			});
			if (autoConfirm)
			{
				TaskManager.Enqueue(delegate
				{
					Callback.Fire(numeric, true, maxValue);
				});
			}
		}
		if (minOrMax != -1)
		{
			return;
		}
		string currentAmt = numericTextNode->NodeText.ToString();
		if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
		{
			TaskManager.Enqueue(delegate
			{
				Callback.Fire(numeric, true, int.Parse(currentAmt));
			});
		}
	}

	private unsafe void FillBankNumeric(SetupAddonArgs obj)
	{
		if (obj.AddonName != "Bank" || ImGui.GetIO().KeyShift || !Config.WorkOnFCChest)
		{
			return;
		}
		try
		{
			int bMinValue = obj.Addon->AtkValues[5].Int;
			int bMaxValue = obj.Addon->AtkValues[6].Int;
			AtkTextNode* bNumericTextNode = obj.Addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
			if (Config.FCExcludeSplit && IsSplitAddon())
			{
				return;
			}
			if (Config.FCChestMinOrMax == 0)
			{
				TaskManager.Enqueue(delegate
				{
					bNumericTextNode->SetText(ConvertToByte(bMinValue));
				});
				if (Config.FCChestConfirm)
				{
					TaskManager.Enqueue(delegate
					{
						Callback.Fire(obj.Addon, true, 3, (uint)bMinValue);
					});
				}
			}
			if (Config.FCChestMinOrMax == 1)
			{
				TaskManager.Enqueue(delegate
				{
					bNumericTextNode->SetText(ConvertToByte(bMaxValue));
				});
				if (Config.FCChestConfirm)
				{
					TaskManager.Enqueue(delegate
					{
						Callback.Fire(obj.Addon, true, 3, (uint)bMaxValue);
					});
				}
			}
			if (Config.FCChestMinOrMax == -1 && int.TryParse(bNumericTextNode->NodeText.ToString(), out var num) && num > 0 && obj.Addon->AtkValues[4].Int > 0)
			{
				TaskManager.Enqueue(delegate
				{
					Callback.Fire(obj.Addon, true, 0);
				});
			}
		}
		catch
		{
		}
	}

	private unsafe void FillVentureNumeric(SetupAddonArgs obj)
	{
		if (obj.AddonName != "ShopExchangeCurrencyDialog" || ImGui.GetIO().KeyShift || !Config.WorkOnVentures)
		{
			return;
		}
		try
		{
			int minValue = 1;
			uint maxAvailable = obj.Addon->AtkValues[5].UInt - obj.Addon->AtkValues[4].UInt;
			uint maxAfford = obj.Addon->AtkValues[1].UInt / obj.Addon->AtkValues[2].UInt;
			uint maxValue = ((maxAvailable > maxAfford) ? maxAfford : maxAvailable);
			AtkTextNode* numericTextNode = obj.Addon->UldManager.NodeList[8]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
			if (Config.VentureMinOrMax == 0)
			{
				TaskManager.Enqueue(delegate
				{
					numericTextNode->SetText(ConvertToByte(minValue));
				});
				if (Config.VentureConfirm)
				{
					TaskManager.Enqueue(delegate
					{
						Callback.Fire(obj.Addon, true, 0, minValue);
					});
				}
			}
			if (Config.VentureMinOrMax != 1)
			{
				return;
			}
			TaskManager.Enqueue(delegate
			{
				numericTextNode->SetText(ConvertToByte((int)maxValue));
			});
			if (Config.VentureConfirm)
			{
				TaskManager.Enqueue(delegate
				{
					Callback.Fire(obj.Addon, true, 0, maxValue);
				});
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private unsafe bool IsSplitAddon()
	{
		AtkUnitBase* numeric = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InputNumeric");
		return numeric->UldManager.NodeList[5]->GetAsAtkTextNode()->NodeText.ToString() == splitText;
	}

	private unsafe bool InFcChest()
	{
		AtkUnitBase* fcChest = (AtkUnitBase*)Svc.GameGui.GetAddonByName("FreeCompanyChest");
		if (fcChest != null)
		{
			return fcChest->IsVisible;
		}
		return false;
	}

	private unsafe bool InFcBank()
	{
		AtkUnitBase* fcBank = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Bank");
		if (fcBank != null)
		{
			return fcBank->IsVisible;
		}
		return false;
	}

	private unsafe bool InMail()
	{
		AtkUnitBase* mail = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterList");
		if (mail != null)
		{
			return mail->IsVisible;
		}
		return false;
	}

	private unsafe bool InTransmute()
	{
		AtkUnitBase* trans = (AtkUnitBase*)Svc.GameGui.GetAddonByName("TradeMultiple");
		if (trans != null)
		{
			return trans->IsVisible;
		}
		return false;
	}

    private unsafe byte* ConvertToByte(int x)
    {
        string str = x.ToString();
        byte[] bytes = Encoding.Default.GetBytes(str);

        byte* unmanagedBytes = (byte*)Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, (IntPtr)unmanagedBytes, bytes.Length);

        return unmanagedBytes;
    }


    public override void Disable()
	{
		SaveConfig(Config);
		Common.OnAddonSetup -= FillRegularNumeric;
		Common.OnAddonSetup -= FillVentureNumeric;
		base.Disable();
	}

	private static void DrawConfigsForAddon(string addonName, ref bool workOnAddon, ref int minOrMax, ref bool excludeSplit, ref bool autoConfirm)
	{
		ImGui.Checkbox("Work on " + addonName, ref workOnAddon);
		if (!workOnAddon)
		{
			return;
		}
		ImGui.PushID(addonName);
		ImGui.Indent();
		if (ImGui.RadioButton("Auto fill highest amount possible", minOrMax == 1))
		{
			minOrMax = 1;
		}
		if (ImGui.RadioButton("Auto fill lowest amount possible", minOrMax == 0))
		{
			minOrMax = 0;
		}
		if (addonName != "Venture Purchase")
		{
			if (ImGui.RadioButton("Auto OK on manually entered amounts", minOrMax == -1))
			{
				minOrMax = -1;
			}
			ImGui.Checkbox("Exclude Split Dialog", ref excludeSplit);
		}
		if (minOrMax != -1)
		{
			ImGui.Checkbox("Auto Confirm", ref autoConfirm);
		}
		ImGui.Unindent();
		ImGui.PopID();
	}
}
