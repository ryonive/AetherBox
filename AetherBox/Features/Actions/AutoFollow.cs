// Add a command for Auto Follow Distance (Example: /afd 10) would set the distance to keep to 10yalms

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AetherBox.Features.Debugging;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using EasyCombat.UI.Helpers;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using DGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using Player = AetherBox.Helpers.Player;

namespace AetherBox.Features.Actions;

public class AutoFollow : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Distance to Keep", "", 1, null)]
        public int distanceToKeep = 3;

        [FeatureConfigOption("Don't follow if further than this (yalms)", "", 2, null, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "Set to 0 to disable")]
        public int disableIfFurtherThan;

        [FeatureConfigOption("Function only in duty", "", 3, null)]
        public bool OnlyInDuty;

        [FeatureConfigOption("Change master on chat message", "", 4, null, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "If a party chat message contains \"autofollow\", the current master will be switched to them.")]
        public bool changeMasterOnChat;

        [FeatureConfigOption("Selected Chat Type", "", 5, null)]
        public XivChatType SelectedChatType;

        [FeatureConfigOption("Mount & Fly (Experimental)", "", 6, null)]
        public bool MountAndFly;

        [FeatureConfigOption("Abort if moving (Experimental)", "", 7, null)]
        public bool AbortIfMoving ;

        [FeatureConfigOption("Auto Jump (Highly Experimental)", "", 8, null)]
        public bool AutoJump;

        public string AutoFollowName = string.Empty;
    }

    private readonly List<string> registeredCommands = new List<string>();

    private readonly OverrideMovement movement = new OverrideMovement();

    private DGameObject? master;
    private uint? masterObjectID;

    public override string Name => "Auto Follow";

    public override string Description => "True Auto Follow. Trigger with /autofollow while targeting someone.\nUse it with no target to wipe the current master.";

    public override FeatureType FeatureType => FeatureType.Actions;

    public Configs? Config { get; private set; }

    protected override DrawConfigDelegate DrawConfigTree => delegate (ref bool hasChanged)
    {
        float tableWidth = ImGui.GetContentRegionAvail().X -10;
        float rowHeight = 30.0f; // Adjust this value as needed for the height of each row
        Vector2 tableSize = new Vector2(tableWidth, rowHeight * 2); // 2 rows
        if (ImGui.BeginTable("AutoFollow header options", 2, ImGuiTableFlags.SizingStretchProp, tableSize))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Checkbox("Duty Only", ref Config.OnlyInDuty))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("When enabled, Auto Follow will only work while you're in a duty.");

            ImGui.TableNextColumn();

            if (ImGui.Checkbox("Change master on chat", ref Config.changeMasterOnChat))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("If a party chat message contains \"autofollow\"\nthe current master will be switched to them.");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Checkbox("Mount & Fly", ref Config.MountAndFly))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("Let Auto Follow use mount");

            ImGui.TableNextColumn();
            // Define your chatTypeOptions array with the chat type names
            string[] chatTypeOptions = Constants.NormalChatTypes.Select(chatType => chatType.ToString()).ToArray();
            int selectedChatTypeIndex = Array.IndexOf(chatTypeOptions, Config.SelectedChatType.ToString());
            // ImGui.PushItemWidth(120);
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2 - 15);
            if (ImGui.Combo("Chat Type", ref selectedChatTypeIndex, chatTypeOptions, chatTypeOptions.Length))
            {
                // User has selected a chat type
                if (selectedChatTypeIndex >= 0 && selectedChatTypeIndex < Constants.NormalChatTypes.Length)
                {
                    Config.SelectedChatType = Constants.NormalChatTypes[selectedChatTypeIndex];
                    hasChanged = true;
                }
            }
            ImGuiHelper.HelpMarker("Select the channel that should be listend to for the \"autofollow\" command!\nNOTE: \"CrossParty\" functions the same as regular party chat!");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Checkbox("Abort if Moving", ref Config.AbortIfMoving))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("Abort mounting if character is moving");

            ImGui.TableNextColumn();
            if (ImGui.Checkbox("Auto Jump", ref Config.AutoJump))
            {
                hasChanged = true;
                if (Config.AutoJump)
                {
                    Notify.Success($"\"Config.AutoJump\" enabled!");
                }
                else
                {
                    Notify.Warning($"\"Config.AutoJump\" disabled!");
                }
            }
            ImGuiHelper.HelpMarker("Attempts to jump whenever the master target jumps.");

            ImGui.EndTable();
        }
        ImGuiHelper.SeperatorWithSpacing();

        if (ImGui.BeginTable("AutoFollow Slider options", 2, ImGuiTableFlags.SizingStretchProp, tableSize))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGuiHelper.TextCentered("Distance to keep (yalms)");
            ImGuiHelper.HelpMarker("Distance threshold for auto follow to start following.\n(NOTE: starts following when distance to master target is greater then the value.)");

            ImGui.TableNextColumn();
            ImGuiHelper.TextCentered("Disable if further than (yalms)");
            ImGuiHelper.HelpMarker("Distance threshold for auto follow to stop following.\n(NOTE: Stops following until master target is in range again.)");


            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 20);
            if (ImGui.SliderInt("", ref Config.distanceToKeep, 0, 30))
            {
                hasChanged = true;
            }
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 20);
            if (ImGui.SliderInt("", ref Config.disableIfFurtherThan, 0, 50))
            {
                hasChanged = true;
            }
            ImGui.EndTable();
        }
        ImGuiHelper.SeperatorWithSpacing();

        ImGui.Spacing();
        ImGui.TextColored(AetherColor.BrightGhostType, $"Current Master: {((master != null) ? master.Name : ((SeString)"null"))}");
        if (Svc.ClientState.LocalPlayer == null)
        {
            ImGui.Text("Your Position: x: null, y: null, z: null");
        }
        else
        {
            ImGui.Text($"Your Position: x: {Svc.ClientState.LocalPlayer.Position.X}, y: {Svc.ClientState.LocalPlayer.Position.Y}, z: {Svc.ClientState.LocalPlayer.Position.Z}");
        }
        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
        defaultInterpolatedStringHandler.AppendLiteral("Master Position: x: ");
        defaultInterpolatedStringHandler.AppendFormatted((master != null) ? ((object)master.Position.X) : "null");
        defaultInterpolatedStringHandler.AppendLiteral(", y: ");
        defaultInterpolatedStringHandler.AppendFormatted((master != null) ? ((object)master.Position.Y) : "null");
        defaultInterpolatedStringHandler.AppendLiteral(", z: ");
        defaultInterpolatedStringHandler.AppendFormatted((master != null) ? ((object)master.Position.Z) : "null");
        ImGui.Text(defaultInterpolatedStringHandler.ToStringAndClear());
        defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
        defaultInterpolatedStringHandler.AppendLiteral("Distance to Master: ");
        defaultInterpolatedStringHandler.AppendFormatted((master != null && Svc.ClientState.LocalPlayer != null) ? ((object)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position)) : "null");
        ImGui.Text(defaultInterpolatedStringHandler.ToStringAndClear());
        if (ImGui.Button("Set"))
        {
            SetMaster();
        }
        ImGuiHelper.HelpMarker("Sets Master target to your current target");
        ImGui.SameLine();

        if (ImGui.Button("Clear"))
        {
            ClearMaster();
        }
        ImGuiHelper.HelpMarker("Clears the current Master target");


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

        ImGui.SameLine();
        if (ImGui.Button($"Set to " + str))
        {
            try
            {
                lastMaster = Svc.Targets.PreviousTarget;
                var masterObjectID = Svc.Targets?.Target?.EntityId;
                if (master == null || lastMaster == null)
                {
                    PrintModuleMessage($"Master is null!");
                }
                else if (master != null)
                {
                    master = Svc.Targets?.Target;
                    masterObjectID = Svc.Targets?.Target?.EntityId;
                    PrintModuleMessage($"Master is set to {master?.Name}");
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Debug($"{ex}");
            }
        }
        ImGui.Text($"{str} Position: {targetPos:f3}");
    };

    public string Command { get; set; } = "/autofollow";


    protected void OnCommand(List<string> args)
    {
        try
        {
            if (Svc.Targets.Target != null)
            {
                SetMaster();
            }
            else
            {
                ClearMaster();
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    protected virtual void OnCommandInternal(string _, string args)
    {
        args = args.ToLower();
        OnCommand(args.Split(' ').ToList());
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
            Svc.Commands.AddHandler(Command, new CommandInfo(OnCommandInternal)
            {
                HelpMessage = "",
                ShowInHelp = false
            });
            registeredCommands.Add(Command);
        }
        Svc.Framework.Update += Follow;
        Svc.Chat.ChatMessage += OnChatMessage;
        Svc.Log.Information($"[{Name}] subscribed to event: [Follow] - [OnChatMessage]'");
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        foreach (string c in registeredCommands)
        {
            Svc.Commands.RemoveHandler(c);
        }
        registeredCommands.Clear();
        Svc.Framework.Update -= Follow;
        Svc.Chat.ChatMessage -= OnChatMessage;
        Svc.Log.Information($"[{Name}] unsubscribed from event: [Follow] - [OnChatMessage]'");
        base.Disable();
    }

    private void SetMaster()
    {
        try
        {
            master = Svc.Targets?.Target;
            masterObjectID = Svc.Targets?.Target?.EntityId;
            PrintModuleMessage($"Master is set to {master?.Name}");
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"{ex}");
        }
    }

    private void ClearMaster()
    {
        try
        {
            master = null;
            masterObjectID = null;
            Svc.Log.Debug($"Clearing current master");
            PrintModuleMessage($"Cleared current master");
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"{ex}");
        }
    }

    private unsafe void Follow(IFramework framework)
    {
        if (!Player.Available) return;

        master = Svc.Objects.FirstOrDefault(x => x.EntityId == masterObjectID || !Config.AutoFollowName.IsNullOrEmpty() && x.Name.TextValue.Equals(Config.AutoFollowName, StringComparison.InvariantCultureIgnoreCase));

        if (master == null) { movement.Enabled = false; return; }
        if (Config.disableIfFurtherThan > 0 && !Player.Object.IsNear(master, Config.disableIfFurtherThan)) { movement.Enabled = false; return; }
        if (Config.OnlyInDuty && !Player.InDuty) { movement.Enabled = false; return; }
        if (Svc.Condition[ConditionFlag.InFlight]) { TaskManager.Abort(); }

        if (Svc.ClientState.LocalPlayer != null)
        {
            if (Config == null) { return; }

            if (Config.disableIfFurtherThan > 0 && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) > (float)Config.disableIfFurtherThan)
            {
                movement.Enabled = false;
                return;
            }
        }

        if (Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == 0)
        {
            movement.Enabled = false;
            return;
        }
        var player = Svc.ClientState.LocalPlayer;
        if (Svc.ClientState.LocalPlayer != null && player.IsDead)
        {
            movement.Enabled = false;
            return;
        }


        if (Svc.Condition[ConditionFlag.InFlight])
        {
            TaskManager.Abort();
        }
        if (master.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
        {
            if (Config.MountAndFly && ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)master.Address)->IsMounted() && CanMount())
            {
                movement.Enabled = false;
                TaskManager.InsertDelay(300);
                TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9u, 3758096384uL, 0u, 0u, 0u, null));
                return;
            }
            if (Config.MountAndFly && ((Structs.Character*)master.Address)->IsFlying != 0 && !Svc.Condition[ConditionFlag.InFlight] && Svc.Condition[ConditionFlag.Mounted])
            {
                movement.Enabled = false;
                TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2u, 3758096384uL, 0u, 0u, 0u, null));
                TaskManager.InsertDelay(50);
                TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2u, 3758096384uL, 0u, 0u, 0u, null));
                return;
            }
            if (!(master.Character()->IsMounted() && Svc.Condition[ConditionFlag.Mounted]) && TerritorySupportsMounting())
            {
                movement.Enabled = false;
                master.BattleChara()->GetStatusManager()->RemoveStatus(10);
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
                return;
            }
        }
        if (Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) <= (float)Config.distanceToKeep)
        {
            movement.Enabled = false;
            return;
        }

        // Check if the master target is jumping
        if (master != null && Config.AutoJump)
        {
            if (master.Position.Y > Svc.ClientState.LocalPlayer?.Position.Y + 0.5 && IsMoving() && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) <= (float)Config.distanceToKeep)
            {
                TestClass.GeneralActionJump();
                return;
            }

        }

        movement.Enabled = true;
        movement.DesiredPosition = master.Position;
    }

    /// <summary>
    /// Determines whether the character or entity can be mounted.
    /// </summary>
    /// <remarks>
    /// <br>This method returns true if all of the following conditions are met:</br>
    /// <br>1. There is a local player (Svc.ClientState.LocalPlayer is not null).</br>
    /// <br>2. The character is not already mounted (ConditionFlag.Mounted is false).</br>
    /// <br>3. The character is not in the process of mounting (ConditionFlag.Mounting is false).</br>
    /// <br>4. The character is not in combat (ConditionFlag.InCombat is false).</br>
    /// <br>5. The current territory allows mounting, as indicated by TerritoryType configuration (TerritoryType.Mount is true).</br>
    ///
    /// <br>If all these conditions are met, the method returns true, indicating that mounting is allowed. Otherwise, it returns false, indicating that mounting is not allowed at that moment.</br>
    /// </remarks>
    /// <returns>True if the character can be mounted under the specified conditions; otherwise, false.</returns>
    private bool CanMount()
    {
        if (Svc.ClientState.LocalPlayer is null) return false;
        if (Svc.Condition[ConditionFlag.Mounted]) return false;
        if (Svc.Condition[ConditionFlag.Mounting]) return false;
        if (Svc.Condition[ConditionFlag.InCombat]) return false;
        if (!Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == Svc.ClientState.TerritoryType).Mount) return false;
        if (Config.AbortIfMoving && IsMoving()) return false;
        if (!Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.Mounting] && !Svc.Condition[ConditionFlag.InCombat])
        {
            return !Svc.Condition[ConditionFlag.Casting];
        }
        return false;
    }

    private static bool TerritorySupportsMounting() => Excel.GetRow<TerritoryType>(Player.Territory)?.Unknown32 != 0;

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type != XivChatType.Party) return;
        var player = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
        if (message.TextValue.ToLowerInvariant().Contains("autofollow"))
        {
            if (int.TryParse(message.TextValue.Split("autofollow")[1], out var distance))
                Config.distanceToKeep = distance;
            else if (message.TextValue.ToLowerInvariant().Contains("autofollow off"))
                ClearMaster();
            else
            {
                foreach (var actor in Svc.Objects)
                {
                    if (actor == null) continue;
                    if (actor.Name.TextValue.Equals(player?.PlayerName))
                    {
                        Svc.Targets.Target = actor;
                        SetMaster();
                    }
                }
            }
        }
    }

    private unsafe void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {

        if (!Config.changeMasterOnChat)
        {
            return;
        }

        // Check if the received chat type matches the selected chat type in Config
        if (type != Config.SelectedChatType)
        {
            return;
        }

        PlayerPayload? player;
        player = sender?.Payloads.SingleOrDefault((Payload x) => x is PlayerPayload) as PlayerPayload;

        // Convert the message to lowercase for case-insensitive comparison
        string lowerMessage = message.TextValue.ToLowerInvariant();

        if (lowerMessage.Contains("autofollow", StringComparison.CurrentCultureIgnoreCase))
        {
            foreach (Dalamud.Game.ClientState.Objects.Types.IGameObject actor in Svc.Objects)
            {
                if (actor != null)
                {
                    Svc.Log.Info($"{actor.Name.TextValue} == {player?.PlayerName} {actor.Name.TextValue.ToLowerInvariant().Equals(player?.PlayerName)}");

                    if (actor.Name.TextValue.Equals(player?.PlayerName) && ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->GetIsTargetable())
                    {
                        Svc.Targets.Target = actor;
                        SetMaster();
                    }
                }
            }
        }

        if (lowerMessage.Contains("autofollowoff", StringComparison.CurrentCultureIgnoreCase))
        {
            foreach (Dalamud.Game.ClientState.Objects.Types.IGameObject actor in Svc.Objects)
            {
                if (actor != null)
                {
                    Svc.Log.Info($"{actor.Name.TextValue} == {player?.PlayerName} {actor.Name.TextValue.ToLowerInvariant().Equals(player?.PlayerName)}");

                    if (actor.Name.TextValue.Equals(player?.PlayerName) && ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->GetIsTargetable())
                    {
                        Svc.Targets.Target = actor;
                        ClearMaster();
                    }
                }
            }
        }


        // Check if the message contains "autofollow off" to clear the master
        if (lowerMessage.Contains("autofollowoff", StringComparison.CurrentCultureIgnoreCase))
        {
            ClearMaster();
        }
    }

}