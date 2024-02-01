using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using AetherBox.Features;
using AetherBox.Features.ChatFeature;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.Helpers.Faloop;
using AetherBox.Helpers.Faloop.Model;
using AetherBox.Helpers.Faloop.Model.Embed;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ECommons.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using SocketIOClient;

namespace AetherBox.Features.ChatFeature;

internal class FaloopToChat : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Username")]
        public string FaloopUsername = string.Empty;

        [FeatureConfigOption("Password")]
        public string FaloopPassword = string.Empty;

        public int Channel = Enum.GetValues<XivChatType>().ToList().IndexOf(XivChatType.Echo);

        public int Jurisdiction;

        public Dictionary<MajorPatch, bool> MajorPatches = new Dictionary<MajorPatch, bool>
        {
            {
                MajorPatch.ARealmReborn,
                true
            },
            {
                MajorPatch.Heavensward,
                true
            },
            {
                MajorPatch.Stormblood,
                true
            },
            {
                MajorPatch.Shadowbringer,
                true
            },
            {
                MajorPatch.Endwalker,
                true
            }
        };

        public bool EnableSpawnReport;

        public bool EnableSpawnTimestamp;

        public bool EnableDeathReport;

        public bool EnableDeathTimestamp;

        public bool DisableInDuty;

        public bool SkipOrphanReport;
    }

    public class SpawnHistory
    {
        public uint MobId;

        public uint WorldId;

        public DateTime At;
    }

    private readonly FaloopSession session = new FaloopSession();

    private readonly FaloopSocketIOClient socket = new FaloopSocketIOClient();

    private readonly string[] jurisdictions = Enum.GetNames<Jurisdiction>();

    private readonly string[] channels = Enum.GetNames<XivChatType>();

    private readonly string[] majorPatches = Enum.GetNames<MajorPatch>();

    public List<SpawnHistory> SpawnHistories = new List<SpawnHistory>();

    public override string Name => "Echo Faloop";

    public override string Description => "Prints out faloop marks in chat. Requires sign in.";

    public override FeatureType FeatureType => FeatureType.ChatFeature;

    public Configs MainConfig { get; private set; }

    public Configs RankS { get; private set; }

    public Configs RankA { get; private set; }

    public Configs RankB { get; private set; }

    public Configs Fate { get; private set; }

    protected override DrawConfigDelegate DrawConfigTree => delegate
    {
        ImGui.InputText("Faloop Username", ref MainConfig.FaloopUsername, 32u);
        ImGui.InputText("Faloop Password", ref MainConfig.FaloopPassword, 128u);
        if (ImGui.Button("Save & Connect"))
        {
            Connect();
        }
        ImGui.SameLine();
        if (ImGui.Button("Emit mock payload"))
        {
            EmitMockData();
        }
        ImGui.SameLine();
        if (ImGui.Button("Kill Connection"))
        {
            socket.Dispose();
        }
        DrawPerRankConfig("Rank S", RankS);
        DrawPerRankConfig("Rank A", RankA);
        DrawPerRankConfig("Rank B", RankB);
        DrawPerRankConfig("Fate", Fate);
    };

    private void DrawPerRankConfig(string label, Configs rankConfig)
    {
        if (ImGui.CollapsingHeader(label))
        {
            ImGui.Indent();
            ImGui.Combo("Channel##" + label, ref rankConfig.Channel, channels, channels.Length);
            ImGui.Combo("Jurisdiction##" + label, ref rankConfig.Jurisdiction, jurisdictions, jurisdictions.Length);
            ImGui.Text("Expansions");
            ImGui.Indent();
            MajorPatch[] values;
            values = Enum.GetValues<MajorPatch>();
            foreach (MajorPatch patchVersion in values)
            {
                bool exists;
                ref bool value = ref CollectionsMarshal.GetValueRefOrAddDefault(rankConfig.MajorPatches, patchVersion, out exists);
                ImGui.Checkbox(Enum.GetName(patchVersion), ref value);
            }
            ImGui.Unindent();
            ImGui.NewLine();
            ImGui.Checkbox("Report Spawns##" + label, ref rankConfig.EnableSpawnReport);
            if (rankConfig.EnableSpawnReport)
            {
                ImGui.Indent();
                ImGui.Checkbox("Display Spawn Timestamp##" + label, ref rankConfig.EnableSpawnTimestamp);
                ImGui.Unindent();
            }
            ImGui.Checkbox("Report Deaths##" + label, ref rankConfig.EnableDeathReport);
            if (rankConfig.EnableDeathReport)
            {
                ImGui.Indent();
                ImGui.Checkbox("Display Death Timestamp##" + label, ref rankConfig.EnableDeathTimestamp);
                ImGui.Unindent();
            }
            ImGui.Checkbox("Disable Reporting While in Duty##" + label, ref rankConfig.DisableInDuty);
            ImGui.Checkbox("Skip Orphan Report##" + label, ref rankConfig.SkipOrphanReport);
            ImGui.Unindent();
        }
    }

    public override void Enable()
    {
        MainConfig = LoadConfig<Configs>("FaloopToChatMainConfig") ?? new Configs();
        RankS = LoadConfig<Configs>("FaloopToChatRankS") ?? new Configs();
        RankA = LoadConfig<Configs>("FaloopToChatRankA") ?? new Configs();
        RankB = LoadConfig<Configs>("FaloopToChatRankB") ?? new Configs();
        Fate = LoadConfig<Configs>("FaloopToChatFate") ?? new Configs();
        socket.OnConnected += OnConnected;
        socket.OnDisconnected += OnDisconnected;
        socket.OnError += OnError;
        socket.OnMobReport += OnMobReport;
        socket.OnAny += OnAny;
        socket.OnReconnected += OnReconnected;
        socket.OnReconnectError += OnReconnectError;
        socket.OnReconnectAttempt += OnReconnectAttempt;
        socket.OnReconnectFailed += OnReconnectFailed;
        socket.OnPing += OnPing;
        socket.OnPong += OnPong;
        Connect();
        CleanSpawnHistories();
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(MainConfig, "FaloopToChatMainConfig");
        SaveConfig(RankS, "FaloopToChatRankS");
        SaveConfig(RankA, "FaloopToChatRankA");
        SaveConfig(RankB, "FaloopToChatRankB");
        SaveConfig(Fate, "FaloopToChatFate");
        socket.Dispose();
        base.Disable();
    }

    private static TextPayload GetRankIcon(string rank)
    {
        return rank switch
        {
            "S" => new TextPayload(SeIconChar.BoxedLetterS.ToIconString()),
            "A" => new TextPayload(SeIconChar.BoxedLetterA.ToIconString()),
            "B" => new TextPayload(SeIconChar.BoxedLetterB.ToIconString()),
            "F" => new TextPayload(SeIconChar.BoxedLetterF.ToIconString()),
            _ => throw new ArgumentException("Unknown rank: " + rank),
        };
    }

    private Configs GetRankConfig(string rank)
    {
        return rank switch
        {
            "S" => RankS,
            "A" => RankA,
            "B" => RankB,
            "F" => Fate,
            _ => null,
        };
    }

    private void OnConnected()
    {
        PrintModuleMessage("Connected");
    }

    private void OnDisconnected(string cause)
    {
        PrintModuleMessage("Disconnected.");
        PluginLog.Warning("Disconnected. Reason: " + cause);
    }

    private static void OnError(string error)
    {
        PluginLog.Error("Disconnected = " + error);
    }

    private void OnMobReport(MobReportData data)
    {
        BNpcName mob;
        mob = Svc.Data.GetExcelSheet<BNpcName>()?.GetRow(data.MobId);
        if (mob == null)
        {
            PluginLog.Debug("OnMobReport: mob == null");
            return;
        }
        MobData mobData;
        mobData = session.EmbedData.Mobs.FirstOrDefault((MobData x) => x.Id == data.MobId);
        if (mobData == null)
        {
            PluginLog.Debug("OnMobReport: mobData == null");
            return;
        }
        World world;
        world = Svc.Data.GetExcelSheet<World>()?.GetRow(data.WorldId);
        WorldDCGroupType dataCenter;
        dataCenter = world?.DataCenter?.Value;
        if (world == null || dataCenter == null)
        {
            PluginLog.Debug("OnMobReport: world == null || dataCenter == null");
            return;
        }
        World currentWorld;
        currentWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.GameData;
        WorldDCGroupType currentDataCenter;
        currentDataCenter = currentWorld?.DataCenter?.Value;
        if (currentWorld == null || currentDataCenter == null)
        {
            PluginLog.Debug("OnMobReport: currentWorld == null || currentDataCenter == null");
            return;
        }
        Configs config;
        config = GetRankConfig(mobData.Rank);
        if (config == null)
        {
            PluginLog.Debug("OnMobReport: config == null");
            return;
        }
        if (!config.MajorPatches.TryGetValue(mobData.Version, out var value) || !value)
        {
            PluginLog.Debug("OnMobReport: majorPatches");
            return;
        }
        if (config.DisableInDuty && Svc.Condition[ConditionFlag.BoundByDuty])
        {
            PluginLog.Debug("OnMobReport: in duty");
            return;
        }
        switch ((Jurisdiction)config.Jurisdiction)
        {
            case Jurisdiction.Region:
                if (dataCenter.Region == currentDataCenter.Region)
                {
                    break;
                }
                goto default;
            case Jurisdiction.DataCenter:
                if (dataCenter.RowId == currentDataCenter.RowId)
                {
                    break;
                }
                goto default;
            case Jurisdiction.World:
                if (world.RowId == currentWorld.RowId)
                {
                    break;
                }
                goto default;
            default:
                PluginLog.Verbose("OnMobReport: unmatched");
                return;
            case Jurisdiction.All:
                break;
        }
        string action;
        action = data.Action;
        if (!(action == "spawn"))
        {
            if (action == "death" && config.EnableDeathReport)
            {
                OnDeathMobReport(data, mob, world, config.Channel, mobData.Rank, config.SkipOrphanReport);
                PluginLog.Verbose($"{"OnMobReport"}: {new Action<MobReportData, BNpcName, World, int, string, bool>(OnDeathMobReport)}");
            }
        }
        else if (config.EnableSpawnReport)
        {
            OnSpawnMobReport(data, mob, world, config.Channel, mobData.Rank);
            PluginLog.Verbose($"{"OnMobReport"}: {new Action<MobReportData, BNpcName, World, int, string>(OnSpawnMobReport)}");
        }
    }

    private void OnSpawnMobReport(MobReportData data, BNpcName mob, World world, int channel, string rank)
    {
        MobReportData.Spawn spawn;
        spawn = data.Data.Deserialize<MobReportData.Spawn>();
        if (spawn == null)
        {
            PluginLog.Debug("OnSpawnMobReport: spawn == null");
            return;
        }
        SpawnHistories.Add(new SpawnHistory
        {
            MobId = data.MobId,
            WorldId = data.WorldId,
            At = spawn.Timestamp
        });
        List<Payload> payloads;
        payloads = new List<Payload>
        {
            new TextPayload(SeIconChar.BoxedPlus.ToIconString() ?? ""),
            GetRankIcon(rank),
            new TextPayload(" " + mob.Singular.RawString + " ")
        };
        SeString mapLink;
        mapLink = CoordinatesHelper.CreateMapLink(spawn.ZoneId, spawn.ZonePoiIds.First(), data.ZoneInstance, session);
        if (mapLink != null)
        {
            payloads.AddRange(mapLink.Payloads);
        }
        payloads.AddRange(new Payload[2]
        {
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload((GetRankConfig(rank).EnableSpawnTimestamp ? $"{world.Name} {NumberHelper.FormatTimeSpan(spawn.Timestamp)}" : ((string)world.Name)) ?? "")
        });
        Svc.Chat.Print(new XivChatEntry
        {
            Name = (spawn.Reporters?.FirstOrDefault()?.Name ?? "Faloop"),
            Message = new SeString(payloads),
            Type = Enum.GetValues<XivChatType>()[channel]
        });
    }

    private void OnDeathMobReport(MobReportData data, BNpcName mob, World world, int channel, string rank, bool skipOrphanReport)
    {
        MobReportData.Death death;
        death = data.Data.Deserialize<MobReportData.Death>();
        if (death == null)
        {
            PluginLog.Debug("OnDeathMobReport: death == null");
            return;
        }
        if (skipOrphanReport && SpawnHistories.RemoveAll((SpawnHistory x) => x.MobId == data.MobId && x.WorldId == data.WorldId) == 0)
        {
            PluginLog.Debug("OnDeathMobReport: skipOrphanReport");
            return;
        }
        Svc.Chat.Print(new XivChatEntry
        {
            Name = "Faloop",
            Message = new SeString(new List<Payload>
            {
                new TextPayload(SeIconChar.Cross.ToIconString() ?? ""),
                GetRankIcon(rank),
                new TextPayload(" " + mob.Singular.RawString),
                new IconPayload(BitmapFontIcon.CrossWorld),
                new TextPayload((GetRankConfig(rank).EnableDeathTimestamp ? $"{world.Name} {NumberHelper.FormatTimeSpan(death.StartedAt)}" : ((string)world.Name)) ?? "")
            }),
            Type = Enum.GetValues<XivChatType>()[channel]
        });
    }

    private static void OnAny(string name, SocketIOResponse response)
    {
        PluginLog.Debug($"{"OnAny"} Event {name} = {response}");
    }

    private static void OnReconnected(int count)
    {
        PluginLog.Debug($"Reconnected {count}");
    }

    private static void OnReconnectError(Exception exception)
    {
        PluginLog.Error($"Reconnect error {exception}");
    }

    private static void OnReconnectAttempt(int count)
    {
        PluginLog.Debug($"Reconnect attempt {count}");
    }

    private static void OnReconnectFailed()
    {
        PluginLog.Debug("Reconnect failed");
    }

    private static void OnPing()
    {
        PluginLog.Debug("Ping");
    }

    private static void OnPong(TimeSpan span)
    {
        PluginLog.Debug($"Pong: {span}");
    }

    public void Connect()
    {
        if (string.IsNullOrWhiteSpace(MainConfig.FaloopUsername) || string.IsNullOrWhiteSpace(MainConfig.FaloopPassword))
        {
            PrintModuleMessage("Login information invalid.");
            return;
        }
        Task.Run(async delegate
        {
            _ = 1;
            try
            {
                if (await session.LoginAsync(MainConfig.FaloopUsername, MainConfig.FaloopPassword))
                {
                    await socket.Connect(session);
                }
            }
            catch (Exception exception)
            {
                PluginLog.Error($"Connection Failed {exception}");
            }
        });
    }

    public void EmitMockData()
    {
        Task.Run(async delegate
        {
            try
            {
                OnMobReport(MockData.SpawnMobReport);
                await Task.Delay(3000);
                OnMobReport(MockData.DeathMobReport);
            }
            catch (Exception exception)
            {
                PluginLog.Error("EmitMockData" + $" {exception}");
            }
        });
    }

    private void CleanSpawnHistories()
    {
        SpawnHistories.RemoveAll((SpawnHistory x) => DateTime.UtcNow - x.At > TimeSpan.FromHours(1.0));
    }
}
