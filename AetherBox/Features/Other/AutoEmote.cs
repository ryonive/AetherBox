using Dalamud.Plugin.Services;
using EasyCombat.UI.Helpers;
using ECommons.Automation;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using AetherBox.FeaturesSetup;
using System.Numerics;
using AetherBox.Helpers;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Dalamud.Game.ClientState.Conditions;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace AetherBox.Features.Other;
public class AutoEmote : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("test", "", 3, null)]
        public bool Test;
    }

    public override string Name => "Auto Emote";

    public override string Description => "Attempts to auto emote, set target";

    public override FeatureType FeatureType => FeatureType.Disabled;

    private Dalamud.Game.ClientState.Objects.Types.IGameObject? mouseOverTarget;
    private uint? emoteObjectID;

    public Configs? Config { get; private set; }

    protected override DrawConfigDelegate DrawConfigTree => delegate (ref bool hasChanged)
    {
        if (ImGui.Checkbox("test", ref Config.Test))
        {
            hasChanged = true;
        }
        Vector3 targetPos;
        Dalamud.Game.ClientState.Objects.Types.IGameObject lastMaster;
        Dalamud.Game.ClientState.Objects.Types.IGameObject target;
        string str;

        lastMaster = Svc.Targets.PreviousTarget;
        target = Svc.Targets.PreviousTarget;

        if (Svc.Targets.Target != null)
        {
            targetPos = Svc.Targets.Target.Position;
            str = "Target";
        }
        else if (lastMaster != null)
        {
            targetPos = lastMaster.Position;
            str = "Last Target";
        }
        else
        {
            targetPos = Vector3.Zero; // Set to null or any other appropriate value
            str = "null";
        }
        ImGui.TextColored(AetherColor.BrightGhostType, $"Current Master: {((mouseOverTarget != null) ? mouseOverTarget.Name : ((SeString)"null"))}");
        if (ImGui.Button("Set"))
        {
            SetMouseTarget();
        }
        ImGuiHelper.HelpMarker("Sets Master target to your current target");
        ImGui.SameLine();

        if (ImGui.Button("Clear"))
        {
            ClearMouseTarget();
        }
        ImGuiHelper.HelpMarker("Clears the current Master target");



        //var hotbarSlotType = Enum.GetName(typeof(HotbarSlotType), commandType) ?? commandType.ToString();
        // if (ImGui.BeginCombo("Type", hotbarSlotType))
        //{
        //  for (int i = 1; i <= 32; i++)
        // {
        //    if (!ImGui.Selectable($"{Enum.GetName(typeof(HotbarSlotType), i) ?? i.ToString()}##{i}", commandType == i)) continue;
        //    commandType = i;
        //  }
        // ImGui.EndCombo();
        // }

        //DrawHotbarIDInput((HotbarSlotType)commandType);
    };
    private static int commandType = 1;
    private static uint commandID = 0;
    private void SetMouseTarget()
    {
        try
        {
            mouseOverTarget = Svc.Targets?.Target;
            emoteObjectID = Svc.Targets?.Target?.EntityId;
            PrintModuleMessage($"Emote target is set to {mouseOverTarget?.Name}");
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"{ex}");
        }
    }

    private void ClearMouseTarget()
    {
        try
        {
            mouseOverTarget = null;
            emoteObjectID = null;
            Svc.Log.Debug($"Clearing current emote target");
            PrintModuleMessage($"Cleared current emote target");
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"{ex}");
        }
    }

    protected static ConditionFlag CurrentConditionFlag { get; set; }
    private static bool didEmote { get; set; } = false;
    private unsafe void DoEmote(IFramework framework)
    {

        if (!Config.Test)
        {
            TaskManager.Abort();
        }
        if (Config.Test)
        {
            TaskManager.Enqueue(delegate
            {
                if (mouseOverTarget != null && !didEmote)
                {
                    Chat.Instance.SendMessage("/dote");
                    didEmote = true;
                    TaskManager.InsertDelay(new Random().Next(7500, 7500));
                    didEmote = false;
                }
            });
        }
    }

    public static void DrawHotbarIDInput(HotbarSlotType slotType)
    {
        switch ((HotbarSlotType)commandType)
        {
            case HotbarSlotType.Emote:
                ImGuiHelper.ExcelSheetCombo($"ID##{commandType}", ref commandID, new ImGuiHelper.ExcelSheetComboOptions<Emote> { FormatRow = r => $"[#{r.RowId}] {r.Name}" });
                break;
            default:
                var ___ = (int)commandID;
                if (ImGui.InputInt("ID", ref ___))
                    commandID = (ushort)___;
                break;
        }
    }

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += DoEmote;
        Svc.Log.Information($"[{Name}] subscribed to event:  [DoEmote]'");
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= DoEmote;
        base.Disable();
    }
}
