using AetherBox.Debugging;
using AetherBox.Helpers;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

#nullable enable
namespace AetherBox.Features.Debugging;

public class QuestDebug : DebugHelper
{
    private readonly FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());

    private QuestManager qm;

    private unsafe QuestManager* _qm = QuestManager.Instance();

    private int selectedQuestID;

    private string questName = "";

    private readonly ExcelSheet<Quest> questSheet;

    private static readonly Dictionary<uint, Quest>? QuestSheet = Svc.Data?.GetExcelSheet<Quest>()?.Where((Quest x) => x.Id.RawString.Length > 0).ToDictionary((Quest i) => i.RowId, (Quest i) => i);

    private readonly List<SeString> questNames = (from x in Svc.Data.GetExcelSheet<Quest>(Svc.ClientState.ClientLanguage)
                                                  select x.Name).ToList();

    public override string Name => "QuestDebug".Replace("Debug", "") + " Debugging";

    public unsafe override void Draw()
    {
        ImGui.Text(Name ?? "");
        ImGui.Separator();
        if (ImGui.Button("Very Easy") && GenericHelpers.TryGetAddonByName<AtkUnitBase>("DifficultySelectYesNo", out var addon))
        {
            Callback.Fire(addon, true, 0, 2);
        }
        ImGui.InputText("###QuestNameInput", ref questName, 500u);
        if (questName != "")
        {
            Quest quest2 = TrySearchQuest(questName);
            ImGui.Text($"QuestID: {quest2.RowId}");
        }
        ImGui.InputInt("###QuestIDInput", ref selectedQuestID, 500);
        if (selectedQuestID != 0)
        {
            ImGui.Text($"Is Quest Accepted?: {qm.IsQuestAccepted((ushort)selectedQuestID)}");
            ImGui.Text($"Is Quest Complete?: {QuestManager.IsQuestComplete((ushort)selectedQuestID)}");
            ImGui.Text($"Current Quest Sequence: {QuestManager.GetQuestSequence((ushort)selectedQuestID)}");
        }
        ImGui.Separator();
        ImGuiEx.TextUnderlined("Accepted Quests");
        Span<QuestWork> normalQuestsSpan = _qm->NormalQuestsSpan;
        for (int i = 0; i < normalQuestsSpan.Length; i++)
        {
            QuestWork quest = normalQuestsSpan[i];
            if (quest.QuestId != 0)
            {
                ImGui.Text($"{quest.QuestId}: {NameOfQuest(quest.QuestId)}\n   seq: {quest.Sequence} flag: {quest.Flags}");
            }
        }
    }

    public static string NameOfQuest(ushort id)
    {
        if (id > 0)
        {
            int digits = id.ToString().Length;
            if (QuestSheet.Any((KeyValuePair<uint, Quest> x) => Convert.ToInt16(x.Value.Id.RawString.GetLast(digits)) == id))
            {
                return QuestSheet.First((KeyValuePair<uint, Quest> x) => Convert.ToInt16(x.Value.Id.RawString.GetLast(digits)) == id).Value.Name.RawString.Replace("\ue0be", "").Trim();
            }
        }
        return "";
    }

    private Quest TrySearchQuest(string input)
    {
        List<(SeString, int)> matchingRows = (from t in questNames.Select((SeString n, int i) => (n: n, i: i))
                                              where !string.IsNullOrEmpty(t.n) && IsMatch(input, t.n)
                                              select t).ToList();
        if (matchingRows.Count > 1)
        {
            matchingRows = matchingRows.OrderByDescending<(SeString, int), object>(((SeString n, int i) t) => MatchingScore(t.n, input)).ToList();
        }
        if (matchingRows.Count <= 0)
        {
            return null;
        }
        return questSheet.GetRow((uint)matchingRows.First().Item2);
    }

    private static bool IsMatch(string x, string y)
    {
        return Regex.IsMatch(x, "\\b" + Regex.Escape(y) + "\\b");
    }

    private static object MatchingScore(string item, string line)
    {
        int score = 0;
        if (line.Contains(item))
        {
            score += item.Length;
        }
        return score;
    }
}
