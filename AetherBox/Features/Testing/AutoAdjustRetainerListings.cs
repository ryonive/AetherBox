using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AetherBox.FeaturesSetup;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Features.Testing;
public class AutoAdjustRetainerListings : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Price Reduction", IntMin = 0, IntMax = 600, EditorSize = 300)]
        public int PriceReduction = 1;

        [FeatureConfigOption("Lowest Acceptable Price", IntMin = 0, IntMax = 600, EditorSize = 300)]
        public int LowestAcceptablePrice = 100;

        [FeatureConfigOption("Separate NQ And HQ")]
        public bool SeparateNQAndHQ;

        [FeatureConfigOption("Max Price Reduction", IntMin = 0, IntMax = 600, EditorSize = 300)]
        public int MaxPriceReduction;
    }

    private static int CurrentItemPrice;

    private static int CurrentMarketLowestPrice;

    private static uint CurrentItemSearchItemID;

    private static bool IsCurrentItemHQ;

    private unsafe static RetainerManager.Retainer* CurrentRetainer;

    private readonly Dictionary<string, string>? resourceData;

    private readonly Dictionary<string, string>? fbResourceData;

    public override string Name => "Auto Adjust Retainer Listings";

    public override string Description => "Adjusts your retainers' items upon opening listings. Interrupt with Shift.";

    public override FeatureType FeatureType => FeatureType.UI;

    public override bool isDebug => false;

    public bool Initialized { get; set; }

    private VirtualKey ConflictKey { get; set; } = VirtualKey.SHIFT;


    public override bool UseAutoConfig => true;

    public Configs Config { get; private set; }

    public override void Enable()
    {
        base.Enable();
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.AddonLifeCycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", OnRetainerSellList);
        Svc.AddonLifeCycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", OnRetainerSell);
        Svc.AddonLifeCycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", OnRetainerSell);
        Svc.Framework.Update += OnUpdate;
        Initialized = true;
    }

    public override void Disable()
    {
        base.Disable();
        SaveConfig(Config);
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifeCycle.UnregisterListener(OnRetainerSellList);
        Svc.AddonLifeCycle.UnregisterListener(OnRetainerSell);
        TaskManager?.Abort();
        Initialized = false;
    }

    private void OnUpdate(IFramework framework)
    {
        if (TaskManager.IsBusy && Svc.KeyState[ConflictKey])
        {
            TaskManager.Abort();
            Chat.Instance.SendMessage("/e ConflictKey used on AutoAdjustRetainerListings <se.6>");
            Svc.PluginInterface.UiBuilder.AddNotification("ConflictKey used on AutoAdjustRetainerListings", "AetherBox", NotificationType.Success);
        }
    }

    private void OnRetainerSell(AddonEvent eventType, AddonArgs addonInfo)
    {
        switch (eventType)
        {
            case AddonEvent.PostSetup:
                if (!TaskManager.IsBusy)
                {
                    TaskManager.Enqueue((Func<bool?>)ClickComparePrice, null);
                    TaskManager.AbortOnTimeout = false;
                    TaskManager.DelayNext(500);
                    TaskManager.Enqueue((Func<bool?>)GetLowestPrice, null);
                    TaskManager.AbortOnTimeout = true;
                    TaskManager.DelayNext(100);
                    TaskManager.Enqueue((Func<bool?>)FillLowestPrice, null);
                }
                break;
            case AddonEvent.PreFinalize:
                if (TaskManager.NumQueuedTasks <= 1)
                {
                    TaskManager.Abort();
                }
                break;
        }
    }

    private unsafe void OnRetainerSellList(AddonEvent type, AddonArgs args)
    {
        RetainerManager.Retainer* activeRetainer = RetainerManager.Instance()->GetActiveRetainer();
        if (CurrentRetainer != null && CurrentRetainer == activeRetainer)
        {
            return;
        }
        CurrentRetainer = activeRetainer;
        GetSellListItems(out var itemCount);
        if (itemCount != 0)
        {
            for (int i = 0; i < itemCount; i++)
            {
                EnqueueSingleItem(i);
                CurrentMarketLowestPrice = 0;
            }
        }
    }

    private void EnqueueSingleItem(int index)
    {
        TaskManager.Enqueue(() => ClickSellingItem(index));
        TaskManager.DelayNext(100);
        TaskManager.Enqueue((Func<bool?>)ClickAdjustPrice, null);
        TaskManager.DelayNext(100);
        TaskManager.Enqueue((Func<bool?>)ClickComparePrice, null);
        TaskManager.DelayNext(500);
        TaskManager.AbortOnTimeout = false;
        TaskManager.Enqueue((Func<bool?>)GetLowestPrice, null);
        TaskManager.AbortOnTimeout = true;
        TaskManager.DelayNext(100);
        TaskManager.Enqueue((Func<bool?>)FillLowestPrice, null);
        TaskManager.DelayNext(800);
    }

    private unsafe static void GetSellListItems(out uint availableItems)
    {
        availableItems = 0u;
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) || !GenericHelpers.IsAddonReady(addon))
        {
            return;
        }
        for (int i = 0; i < 20; i++)
        {
            if (InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerMarket)->GetInventorySlot(i)->ItemID != 0)
            {
                availableItems++;
            }
        }
    }

    private unsafe static bool? ClickSellingItem(int index)
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            Callback.Fire(addon, true, 0, index, 1);
            return true;
        }
        return false;
    }

    private unsafe static bool? ClickAdjustPrice()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            Callback.Fire(addon, true, 0, 0, 0, 0, 0);
            return true;
        }
        return false;
    }

    private unsafe static bool? ClickComparePrice()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSell", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            CurrentItemPrice = addon->AtkValues[5].Int;
            IsCurrentItemHQ = Marshal.PtrToStringUTF8((nint)addon->AtkValues[1].String).Contains('\ue03c');
            Callback.Fire(addon, true, 4);
            return true;
        }
        return false;
    }

    private unsafe bool? GetLowestPrice()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ItemSearchResult", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            CurrentItemSearchItemID = AgentItemSearch.Instance()->ResultItemID;
            string searchResult = addon->GetTextNodeById(29u)->NodeText.ExtractText();
            if (string.IsNullOrEmpty(searchResult))
            {
                return false;
            }
            if (int.Parse(AutoRetainerPriceAdjustRegex().Replace(searchResult, "")) == 0)
            {
                CurrentMarketLowestPrice = 0;
                addon->Close(fireCallback: true);
                return true;
            }
            if (Config.SeparateNQAndHQ && IsCurrentItemHQ)
            {
                bool foundHQItem = false;
                for (int i = 1; i <= 12; i++)
                {
                    if (foundHQItem)
                    {
                        break;
                    }
                    AtkResNode**  listing = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList;
                    if (listing[13]->GetAsAtkImageNode()->AtkResNode.IsVisible)
                    {
                        string priceText2 = listing[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                        if (int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText2, ""), out CurrentMarketLowestPrice))
                        {
                            foundHQItem = true;
                        }
                    }
                }
                if (!foundHQItem)
                {
                    string priceText = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                    if (!int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""), out CurrentMarketLowestPrice))
                    {
                        return false;
                    }
                }
            }
            else
            {
                string priceText3 = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                if (!int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText3, ""), out CurrentMarketLowestPrice))
                {
                    return false;
                }
            }
            addon->Close(fireCallback: true);
            return true;
        }
        return false;
    }

    private unsafe bool? FillLowestPrice()
    {
        if (GenericHelpers.TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            AtkUnitBase* ui = &addon->AtkUnitBase;
            AtkComponentNumericInput* priceComponent = addon->AskingPrice;
            if (CurrentMarketLowestPrice - Config.PriceReduction < Config.LowestAcceptablePrice)
            {
                SeString message = GetSeString("Item is listed lower than minimum price, skipping", SeString.CreateItemLink(CurrentItemSearchItemID, IsCurrentItemHQ ? ItemPayload.ItemKind.Hq : ItemPayload.ItemKind.Normal), CurrentMarketLowestPrice, CurrentItemPrice, Config.LowestAcceptablePrice); ;
                Svc.Chat.Print(message);
                Callback.Fire((AtkUnitBase*)addon, true, 1);
                ui->Close(fireCallback: true);
                return true;
            }
            if (Config.MaxPriceReduction != 0 && CurrentItemPrice - CurrentMarketLowestPrice > Config.LowestAcceptablePrice)
            {
                SeString message2 = GetSeString("Item has exceeded maximum acceptable reduction, skipping", SeString.CreateItemLink(CurrentItemSearchItemID, IsCurrentItemHQ ? ItemPayload.ItemKind.Hq : ItemPayload.ItemKind.Normal), CurrentMarketLowestPrice, CurrentItemPrice, Config.MaxPriceReduction);
                Svc.Chat.Print(message2);
                Callback.Fire((AtkUnitBase*)addon, true, 1);
                ui->Close(fireCallback: true);
                return true;
            }
            priceComponent->SetValue(CurrentMarketLowestPrice - Config.PriceReduction);
            Callback.Fire((AtkUnitBase*)addon, true, 0);
            ui->Close(fireCallback: true);
            return true;
        }
        return false;
    }

    public SeString GetSeString(string key, params object[] args)
    {
        string format = (resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.GetValueOrDefault(key));
        SeStringBuilder ssb = new SeStringBuilder();

        int lastIndex = 0;

        ssb.AddUiForeground("[AetherBox]", 34);
        int num;
        foreach (Match match in SeStringRegex().Matches(format))
        {
            num = lastIndex;
            ssb.AddUiForeground(format.Substring(num, match.Index - num), 2);
            lastIndex = match.Index + match.Length;
            if (int.TryParse(match.Groups[1].Value, out var argIndex) && argIndex >= 0 && argIndex < args.Length)
            {
                if (args[argIndex] is SeString seString)
                {
                    ssb.Append(seString);
                }
                else
                {
                    ssb.AddUiForeground(args[argIndex].ToString(), 2);
                }
            }
        }
        string text = format;
        num = lastIndex;
        ssb.AddUiForeground(text.Substring(num, text.Length - num), 2);
        return ssb.Build();
    }

    private static Regex SeStringRegex()
    {
        return new Regex("[\\\"].+?[\\\"]|[^ ]+");
    }

    private static Regex AutoRetainerPriceAdjustRegex()
    {
        return new Regex("[^0-9]");
    }
}
