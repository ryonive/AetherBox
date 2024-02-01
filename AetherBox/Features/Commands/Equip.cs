using System;
using System.Collections.Generic;
using System.Linq;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.Commands;

public class Equip : CommandFeature
{
    private static int EquipAttemptLoops;

    public override string Name => "Equip";

    public override string Command { get; set; } = "/equip";


    public override string Description => "Equip an item via id";

    public override List<string> Parameters => new List<string> { "" };

    public override FeatureType FeatureType => FeatureType.Commands;

    protected override void OnCommand(List<string> args)
    {
        try
        {
            if (uint.TryParse(args[0], out var itemID))
            {
                EquipItem(itemID);
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    private static uint GetItemIDFromString(string arg)
    {
        return Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).FirstOrDefault((Item x) => x.Name == arg).RowId;
    }

    private unsafe static void EquipItem(uint itemId)
    {
        (InventoryType, int)? pos;
        pos = FindItemInInventory(itemId, new List<InventoryType>
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
                InventoryType.ArmoryMainHand,
                InventoryType.ArmoryHands
            });
        if (!pos.HasValue)
        {
            DuoLog.Error($"Failed to find item {Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name} (ID: {itemId}) in inventory");
            return;
        }
        InventoryType item;
        item = pos.Value.Item1;
        bool flag;
        flag = ((item == InventoryType.ArmoryHands || item == InventoryType.ArmoryMainHand) ? true : false);
        AgentId agentId;
        agentId = (flag ? AgentId.ArmouryBoard : AgentId.Inventory);
        uint addonId;
        addonId = AgentModule.Instance()->GetAgentByInternalId(agentId)->GetAddonID();
        AgentInventoryContext* ctx;
        ctx = AgentInventoryContext.Instance();
        ctx->OpenForItemSlot(pos.Value.Item1, pos.Value.Item2, addonId);
        AtkUnitBase* contextMenu;
        contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
        if (contextMenu == null)
        {
            return;
        }
        for (int i = 0; i < contextMenu->AtkValuesCount; i++)
        {
            if (ctx->EventIdSpan[i] == 25)
            {
                Svc.Log.Debug($"Equipping item #{itemId} from {pos.Value.Item1} @ {pos.Value.Item2}, index {i}");
                Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0);
            }
        }
        Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
        EquipAttemptLoops++;
        if (EquipAttemptLoops >= 5)
        {
            DuoLog.Error("Equip option not found after 5 attempts. Aborting.");
        }
    }

    private unsafe static (InventoryType inv, int slot)? FindItemInInventory(uint itemId, IEnumerable<InventoryType> inventories)
    {
        foreach (InventoryType inv in inventories)
        {
            InventoryContainer* cont;
            cont = InventoryManager.Instance()->GetInventoryContainer(inv);
            for (int i = 0; i < cont->Size; i++)
            {
                if (cont->GetInventorySlot(i)->ItemID == itemId)
                {
                    return (inv, i);
                }
            }
        }
        return null;
    }
}
