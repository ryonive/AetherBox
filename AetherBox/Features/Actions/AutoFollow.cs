using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Components;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

#nullable enable
namespace AetherBox.Features.Actions;

public class AutoFollow : Feature
{
#nullable disable
    private readonly  List<string> registeredCommands = [];
    private readonly OverrideMovement movement = new();
#nullable enable
    private Dalamud.Game.ClientState.Objects.Types.GameObject? master;
    private uint? masterObjectID;
#nullable disable

    public override string Name => "Auto Follow";

    public override string Description
    {
        get
        {
            return "True Auto Follow. Trigger with /autofollow while targeting someone. Use it with no target to wipe the current master.";
        }
    }

    public override FeatureType FeatureType => FeatureType.Actions;

    public Configs Config { get; private set; }

    protected override DrawConfigDelegate DrawConfigTree
    {
        get
        {
            return (ref bool hasChanged) =>
            {
                if (ImGui.Checkbox("Function Only in Duty", ref Config.OnlyInDuty))
                    hasChanged = true;
                if (ImGui.Checkbox("Change master on chat message", ref Config.changeMasterOnChat))
                    hasChanged = true;
                ImGuiComponents.HelpMarker("If a party chat message contains \"autofollow\", the current master will be switched to them.");
                ImGui.PushItemWidth(300f);
                if (ImGui.SliderInt("Distance to Keep (yalms)", ref Config.distanceToKeep, 0, 30))
                    hasChanged = true;
                ImGui.PushItemWidth(300f);
                if (ImGui.SliderInt("Disable if Further Than (yalms)", ref Config.disableIfFurtherThan, 0, 300))
                    hasChanged = true;
                var interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 1);
                interpolatedStringHandler.AppendLiteral("Current Master: ");
                interpolatedStringHandler.AppendFormatted(master != null ? master.Name : (SeString)"null");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                if (Svc.ClientState.LocalPlayer == null)
                {
                    ImGui.Text("Your Position: x: null, y: null, z: null");
                }
                else
                {
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 3);
                    interpolatedStringHandler.AppendLiteral("Your Position: x: ");
                    interpolatedStringHandler.AppendFormatted(Svc.ClientState.LocalPlayer.Position.X);
                    interpolatedStringHandler.AppendLiteral(", y: ");
                    interpolatedStringHandler.AppendFormatted(Svc.ClientState.LocalPlayer.Position.Y);
                    interpolatedStringHandler.AppendLiteral(", z: ");
                    interpolatedStringHandler.AppendFormatted(Svc.ClientState.LocalPlayer.Position.Z);
                    ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                }
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
                interpolatedStringHandler.AppendLiteral("Master Position: x: ");
                interpolatedStringHandler.AppendFormatted(master != null ? master.Position.X : (object)"null");
                interpolatedStringHandler.AppendLiteral(", y: ");
                interpolatedStringHandler.AppendFormatted(master != null ? master.Position.Y : (object)"null");
                interpolatedStringHandler.AppendLiteral(", z: ");
                interpolatedStringHandler.AppendFormatted(master != null ? master.Position.Z : (object)"null");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
                interpolatedStringHandler.AppendLiteral("Distance to Master: ");
                interpolatedStringHandler.AppendFormatted((master == null) || (Svc.ClientState.LocalPlayer == null) ? "null" : (object)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position));
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                if (ImGui.Button("Set"))
                    SetMaster();
                ImGui.SameLine();
                if (!ImGui.Button("Clear"))
                    return;
                ClearMaster();
            };
        }
    }

    public string Command { get; set; } = "/autofollow";

    protected void OnCommand(List<string> args)
    {
        try
        {
            if (Svc.Targets.Target != null)
                SetMaster();
            else
                ClearMaster();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    protected virtual void OnCommandInternal(string _, string args)
    {
        args = args.ToLower();
        OnCommand([.. args.Split(' ')]);
    }

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        if (Svc.Commands.Commands.ContainsKey(Command))
        {
            Svc.Log.Error("Command '" + Command + "' is already registered.");
        }
        else
        {
            Svc.Commands.AddHandler(Command, new CommandInfo(new CommandInfo.HandlerDelegate(OnCommandInternal))
            {
                HelpMessage = "",
                ShowInHelp = false
            });
            registeredCommands.Add(Command);
        }
        Svc.Framework.Update += new IFramework.OnUpdateDelegate(Follow);
        Svc.Chat.ChatMessage += new IChatGui.OnMessageDelegate(OnChatMessage);
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        foreach (var registeredCommand in registeredCommands)
            Svc.Commands.RemoveHandler(registeredCommand);
        registeredCommands.Clear();
        Svc.Framework.Update -= new IFramework.OnUpdateDelegate(Follow);
        Svc.Chat.ChatMessage -= new IChatGui.OnMessageDelegate(OnChatMessage);
        base.Disable();
    }

    private void SetMaster()
    {
        try
        {
            master = Svc.Targets.Target;
            masterObjectID = new uint?(Svc.Targets.Target.ObjectId);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"{ex}");
        }
    }

    private void ClearMaster()
    {
        master = null;
        masterObjectID = new uint?();
    }

    private unsafe void Follow(IFramework framework)
    {
        master = Svc.Objects.FirstOrDefault(x =>
        {
            var objectId = (int) x.ObjectId;
            var masterObjectId = masterObjectID;
            var valueOrDefault = (int) masterObjectId.GetValueOrDefault();
            return objectId == valueOrDefault & masterObjectId.HasValue;
        });
        if (master == null)
            movement.Enabled = false;
        else if ((double)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) <= Config.distanceToKeep)
            movement.Enabled = false;
        else if (Config.disableIfFurtherThan > 0 && (double)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) > Config.disableIfFurtherThan)
            movement.Enabled = false;
        else if (Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == 0)
        {
            movement.Enabled = false;
        }
        else
        {
            movement.Enabled = true;
            movement.DesiredPosition = master.Position;
        }
    }

    private unsafe void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type != XivChatType.Party)
            return;
        var playerPayload = sender.Payloads.SingleOrDefault( x => x is PlayerPayload) as PlayerPayload;
        if (!message.TextValue.ToLowerInvariant().Contains("autofollow", StringComparison.CurrentCultureIgnoreCase))
            return;
        foreach (var gameObject in (IEnumerable<Dalamud.Game.ClientState.Objects.Types.GameObject>)Svc.Objects)
        {
            if (gameObject != null)
            {
                var log = Svc.Log;
                var interpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 3);
                interpolatedStringHandler.AppendFormatted(gameObject.Name.TextValue);
                interpolatedStringHandler.AppendLiteral(" == ");
                interpolatedStringHandler.AppendFormatted(playerPayload.PlayerName);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted(gameObject.Name.TextValue.ToLowerInvariant().Equals(playerPayload.PlayerName));
                var stringAndClear = interpolatedStringHandler.ToStringAndClear();
                var objArray = Array.Empty<object>();
                log.Info(stringAndClear, objArray);
                if (gameObject.Name.TextValue.Equals(playerPayload.PlayerName) && ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)gameObject.Address)->GetIsTargetable())
                {
                    Svc.Targets.Target = gameObject;
                    SetMaster();
                }
            }
        }
    }

    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Distance to Keep", "", 1, null, IntMin = 0, IntMax = 30, EditorSize = 300)]
        public int distanceToKeep = 3;
        [FeatureConfigOption("Don't follow if further than this (yalms)", "", 2, null, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "Set to 0 to disable")]
        public int disableIfFurtherThan;
        [FeatureConfigOption("Function only in duty", "", 3, null, IntMin = 0, IntMax = 30, EditorSize = 300)]
        public bool OnlyInDuty;
        [FeatureConfigOption("Change master on chat message", "", 3, null, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "If a party chat message contains \"autofollow\", the current master will be switched to them.")]
        public bool changeMasterOnChat;
    }
}
