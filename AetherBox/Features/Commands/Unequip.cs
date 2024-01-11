// AetherBox, Version=69.3.0.0, Culture=neutral, PublicKeyToken=null
// AetherBox.Features.Commands.Unequip
using System;
using System.Collections.Generic;
using System.Linq;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
namespace AetherBox.Features.Commands;
public class Unequip : CommandFeature
{
	private enum EquippedSlots
	{
		ArmoryMainHand = 0,
		ArmoryOffHand = 1,
		ArmoryHead = 2,
		ArmoryBody = 3,
		ArmoryHands = 4,
		ArmoryWaist = 5,
		ArmoryLegs = 6,
		ArmoryFeet = 7,
		ArmoryEar = 8,
		ArmoryNeck = 9,
		ArmoryWrist = 10,
		ArmoryRingsL = 11,
		ArmoryRingsR = 11,
		ArmorySoulCrystal = 13
	}

	public override string Name => "Unequip";

	public override string Command { get; set; } = "/unequip";


	public override string[] Alias => new string[1] { "/ada" };

	public override string Description => "Call any action directly.";

	public override List<string> Parameters => new List<string> { "[<head/body/arms/legs/feet/earring/necklace/bracelet/Lring/Rring>]", "[<destination>]" };

	public override bool isDebug => true;

	public override FeatureType FeatureType => FeatureType.Disabled;

	protected unsafe override void OnCommand(List<string> args)
	{
		try
		{
			InventoryContainer* c;
			c = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
			for (int i = 0; i < c->Size; i++)
			{
				Svc.Log.Info($"{c->Items[i].ItemID} : {c->Items[i].Slot}");
			}
			Svc.Data.GetExcelSheet<ItemUICategory>(Svc.ClientState.ClientLanguage).First((ItemUICategory x) => x.Name.RawString.Contains(args[0], StringComparison.CurrentCultureIgnoreCase));
			_ = args[1] == "i";
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private unsafe static InventoryContainer* GetFreeInventoryContainer()
	{
		InventoryManager* intPtr;
		intPtr = InventoryManager.Instance();
		InventoryContainer* inv1;
		inv1 = intPtr->GetInventoryContainer(InventoryType.Inventory1);
		InventoryContainer* inv2;
		inv2 = intPtr->GetInventoryContainer(InventoryType.Inventory2);
		InventoryContainer* inv3;
		inv3 = intPtr->GetInventoryContainer(InventoryType.Inventory3);
		InventoryContainer* inv4;
		inv4 = intPtr->GetInventoryContainer(InventoryType.Inventory4);
		InventoryContainer*[] array;
		array = new InventoryContainer*[4] { inv1, inv2, inv3, inv4 };
		foreach (InventoryContainer* c in array)
		{
			for (int i = 0; i < c->Size; i++)
			{
				if (c->Items[i].ItemID == 0)
				{
					return c;
				}
			}
		}
		return null;
	}
}
