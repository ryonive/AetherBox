using System;
using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.Commands;

public class DirectActionCall : CommandFeature
{
    public override string Name => "Direct Action Call";

    public override string Command { get; set; } = "/directaction";


    public override string[] Alias => new string[1] { "/ada" };

    public override string Description => "Call any action directly.";

    public override List<string> Parameters => new List<string> { "[<ActionType>]", "[<ID>]" };

    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Commands;

    protected unsafe override void OnCommand(List<string> args)
    {
        try
        {
            ActionType actionType;
            actionType = ParseActionType(args[0]);
            uint actionID;
            actionID = uint.Parse(args[1]);
            PluginLog.Log("Executing " + Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(Svc.ClientState.ClientLanguage).GetRow(actionID).Name.RawString);
            ActionManager.Instance()->UseActionLocation(actionType, actionID, 3758096384uL, null);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    private static ActionType ParseActionType(string input)
    {
        if (Enum.TryParse<ActionType>(input, ignoreCase: true, out var result))
        {
            return result;
        }
        if (byte.TryParse(input, out var intValue) && Enum.IsDefined(typeof(ActionType), intValue))
        {
            return (ActionType)intValue;
        }
        throw new ArgumentException("Invalid ActionType", "input");
    }
}
