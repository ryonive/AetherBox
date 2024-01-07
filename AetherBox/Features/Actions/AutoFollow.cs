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
    private readonly  List<string> registeredCommands = new List<string>();
    private readonly OverrideMovement movement = new OverrideMovement();
    private Dalamud.Game.ClientState.Objects.Types.GameObject? master;
    private uint? masterObjectID;

    public override string Name => "Auto Follow";

    public override string Description
    {
        get
        {
            return "True Auto Follow. Trigger with /autofollow while targeting someone. Use it with no target to wipe the current master.";
        }
    }

    public override FeatureType FeatureType => FeatureType.Actions;

    public AutoFollow.Configs Config { get; private set; }

    protected override BaseFeature.DrawConfigDelegate DrawConfigTree
    {
        get
        {
            return (BaseFeature.DrawConfigDelegate)((ref bool hasChanged) =>
            {
                if (ImGui.Checkbox("Function Only in Duty", ref this.Config.OnlyInDuty))
                    hasChanged = true;
                if (ImGui.Checkbox("Change master on chat message", ref this.Config.changeMasterOnChat))
                    hasChanged = true;
                ImGuiComponents.HelpMarker("If a party chat message contains \"autofollow\", the current master will be switched to them.");
                ImGui.PushItemWidth(300f);
                if (ImGui.SliderInt("Distance to Keep (yalms)", ref this.Config.distanceToKeep, 0, 30))
                    hasChanged = true;
                ImGui.PushItemWidth(300f);
                if (ImGui.SliderInt("Disable if Further Than (yalms)", ref this.Config.disableIfFurtherThan, 0, 300))
                    hasChanged = true;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 1);
                interpolatedStringHandler.AppendLiteral("Current Master: ");
                interpolatedStringHandler.AppendFormatted<SeString>(this.master != null ? this.master.Name : (SeString)"null");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                if (Svc.ClientState.LocalPlayer == null)
                {
                    ImGui.Text("Your Position: x: null, y: null, z: null");
                }
                else
                {
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 3);
                    interpolatedStringHandler.AppendLiteral("Your Position: x: ");
                    interpolatedStringHandler.AppendFormatted<float>(Svc.ClientState.LocalPlayer.Position.X);
                    interpolatedStringHandler.AppendLiteral(", y: ");
                    interpolatedStringHandler.AppendFormatted<float>(Svc.ClientState.LocalPlayer.Position.Y);
                    interpolatedStringHandler.AppendLiteral(", z: ");
                    interpolatedStringHandler.AppendFormatted<float>(Svc.ClientState.LocalPlayer.Position.Z);
                    ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                }
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
                interpolatedStringHandler.AppendLiteral("Master Position: x: ");
                interpolatedStringHandler.AppendFormatted(this.master != null ? (object)this.master.Position.X : (object)"null");
                interpolatedStringHandler.AppendLiteral(", y: ");
                interpolatedStringHandler.AppendFormatted(this.master != null ? (object)this.master.Position.Y : (object)"null");
                interpolatedStringHandler.AppendLiteral(", z: ");
                interpolatedStringHandler.AppendFormatted(this.master != null ? (object)this.master.Position.Z : (object)"null");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
                interpolatedStringHandler.AppendLiteral("Distance to Master: ");
                interpolatedStringHandler.AppendFormatted(!(this.master != null) || !(Svc.ClientState.LocalPlayer != null) ? (object)"null" : (object)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, this.master.Position));
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                if (ImGui.Button("Set"))
                    this.SetMaster();
                ImGui.SameLine();
                if (!ImGui.Button("Clear"))
                    return;
                this.ClearMaster();
            });
        }
    }

    public string Command { get; set; } = "/autofollow";

    protected void OnCommand(List<string> args)
    {
        try
        {
            if (Svc.Targets.Target != null)
                this.SetMaster();
            else
                this.ClearMaster();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    protected virtual void OnCommandInternal(string _, string args)
    {
        args = args.ToLower();
        this.OnCommand(((IEnumerable<string>)args.Split(' ')).ToList<string>());
    }

    public override void Enable()
    {
        this.Config = this.LoadConfig<AutoFollow.Configs>() ?? new AutoFollow.Configs();
        if (Svc.Commands.Commands.ContainsKey(this.Command))
        {
            Svc.Log.Error("Command '" + this.Command + "' is already registered.");
        }
        else
        {
            Svc.Commands.AddHandler(this.Command, new CommandInfo(new CommandInfo.HandlerDelegate(this.OnCommandInternal))
            {
                HelpMessage = "",
                ShowInHelp = false
            });
            this.registeredCommands.Add(this.Command);
        }
        Svc.Framework.Update += new IFramework.OnUpdateDelegate(this.Follow);
        Svc.Chat.ChatMessage += new IChatGui.OnMessageDelegate(this.OnChatMessage);
        base.Enable();
    }

    public override void Disable()
    {
        this.SaveConfig<AutoFollow.Configs>(this.Config);
        foreach (string registeredCommand in this.registeredCommands)
            Svc.Commands.RemoveHandler(registeredCommand);
        this.registeredCommands.Clear();
        Svc.Framework.Update -= new IFramework.OnUpdateDelegate(this.Follow);
        Svc.Chat.ChatMessage -= new IChatGui.OnMessageDelegate(this.OnChatMessage);
        base.Disable();
    }

    private void SetMaster()
    {
        try
        {
            this.master = Svc.Targets.Target;
            this.masterObjectID = new uint?(Svc.Targets.Target.ObjectId);
        }
        catch(Exception ex)
        {
            Svc.Log.Warning($"{ex}");
        }
    }

    private void ClearMaster()
    {
        this.master = null;
        this.masterObjectID = new uint?();
    }

    private unsafe void Follow(IFramework framework)
    {
        this.master = Svc.Objects.FirstOrDefault(x =>
        {
            int objectId = (int) x.ObjectId;
            uint? masterObjectId = this.masterObjectID;
            int valueOrDefault = (int) masterObjectId.GetValueOrDefault();
            return objectId == valueOrDefault & masterObjectId.HasValue;
        });
        if (this.master == (Dalamud.Game.ClientState.Objects.Types.GameObject)null)
            this.movement.Enabled = false;
        else if ((double)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, this.master.Position) <= (double)this.Config.distanceToKeep)
            this.movement.Enabled = false;
        else if (this.Config.disableIfFurtherThan > 0 && (double)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, this.master.Position) > (double)this.Config.disableIfFurtherThan)
            this.movement.Enabled = false;
        else if (this.Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == (ushort)0)
        {
            this.movement.Enabled = false;
        }
        else
        {
            this.movement.Enabled = true;
            this.movement.DesiredPosition = this.master.Position;
        }
    }

    private unsafe void OnChatMessage(
      XivChatType type,
      uint senderId,
      ref SeString sender,
      ref SeString message,
      ref bool isHandled)
    {
        if (type != XivChatType.Party)
            return;
        PlayerPayload playerPayload = sender.Payloads.SingleOrDefault<Payload>((Func<Payload, bool>) (x => x is PlayerPayload)) as PlayerPayload;
        if (!message.TextValue.ToLowerInvariant().Contains("autofollow", StringComparison.CurrentCultureIgnoreCase))
            return;
        foreach (Dalamud.Game.ClientState.Objects.Types.GameObject gameObject in (IEnumerable<Dalamud.Game.ClientState.Objects.Types.GameObject>)Svc.Objects)
        {
            if (gameObject != null)
            {
                IPluginLog log = Svc.Log;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 3);
                interpolatedStringHandler.AppendFormatted(gameObject.Name.TextValue);
                interpolatedStringHandler.AppendLiteral(" == ");
                interpolatedStringHandler.AppendFormatted(playerPayload.PlayerName);
                interpolatedStringHandler.AppendLiteral(" ");
                interpolatedStringHandler.AppendFormatted<bool>(gameObject.Name.TextValue.ToLowerInvariant().Equals(playerPayload.PlayerName));
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                object[] objArray = Array.Empty<object>();
                log.Info(stringAndClear, objArray);
                if (gameObject.Name.TextValue.Equals(playerPayload.PlayerName) && ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)gameObject.Address)->GetIsTargetable())
                {
                    Svc.Targets.Target = gameObject;
                    this.SetMaster();
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
