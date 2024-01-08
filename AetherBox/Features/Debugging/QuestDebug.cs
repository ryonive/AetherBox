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
namespace AetherBox.Features.Debugging
{
    public class QuestDebug : DebugHelper
    {
        private readonly 
    #nullable disable
    FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        private QuestManager qm;
        private unsafe QuestManager* _qm = QuestManager.Instance();
        private int selectedQuestID;
        private string questName = "";
        private readonly ExcelSheet<Quest> questSheet;
        private static readonly 
    #nullable enable
    Dictionary<uint, 
    #nullable disable
    Quest>
    #nullable enable
    ? QuestSheet;
        private readonly 
    #nullable disable
    List<SeString> questNames = Svc.Data.GetExcelSheet<Quest>(Svc.ClientState.ClientLanguage).Select<Quest, SeString>((Func<Quest, SeString>) (x => x.Name)).ToList<SeString>();

        public override string Name => nameof(QuestDebug).Replace("Debug", "") + " Debugging";

        public override unsafe void Draw()
        {
            ImGui.Text(this.Name ?? "");
            ImGui.Separator();
            AtkUnitBase* AddonPtr;
            if (ImGui.Button("Very Easy") && GenericHelpers.TryGetAddonByName<AtkUnitBase>("DifficultySelectYesNo", out AddonPtr))
                Callback.Fire(AddonPtr, true, (object)0, (object)2);
            ImGui.InputText("###QuestNameInput", ref this.questName, 500U);
            DefaultInterpolatedStringHandler interpolatedStringHandler;
            if (this.questName != "")
            {
                Quest quest = this.TrySearchQuest(this.questName);
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 1);
                interpolatedStringHandler.AppendLiteral("QuestID: ");
                interpolatedStringHandler.AppendFormatted<int>((int)quest.RowId);
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            }
            ImGui.InputInt("###QuestIDInput", ref this.selectedQuestID, 500);
            if (this.selectedQuestID != 0)
            {
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
                interpolatedStringHandler.AppendLiteral("Is Quest Accepted?: ");
                interpolatedStringHandler.AppendFormatted<bool>(this.qm.IsQuestAccepted((ushort)this.selectedQuestID));
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
                interpolatedStringHandler.AppendLiteral("Is Quest Complete?: ");
                interpolatedStringHandler.AppendFormatted<bool>(QuestManager.IsQuestComplete((ushort)this.selectedQuestID));
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
                interpolatedStringHandler.AppendLiteral("Current Quest Sequence: ");
                interpolatedStringHandler.AppendFormatted<byte>(QuestManager.GetQuestSequence((ushort)this.selectedQuestID));
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            }
            ImGui.Separator();
            ImGuiEx.TextUnderlined("Accepted Quests");
            Span<QuestWork> normalQuestsSpan = this._qm->NormalQuestsSpan;
            for (int index = 0; index < normalQuestsSpan.Length; ++index)
            {
                QuestWork questWork = normalQuestsSpan[index];
                if (questWork.QuestId != (ushort)0)
                {
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 4);
                    interpolatedStringHandler.AppendFormatted<ushort>(questWork.QuestId);
                    interpolatedStringHandler.AppendLiteral(": ");
                    interpolatedStringHandler.AppendFormatted(QuestDebug.NameOfQuest(questWork.QuestId));
                    interpolatedStringHandler.AppendLiteral("\n   seq: ");
                    interpolatedStringHandler.AppendFormatted<byte>(questWork.Sequence);
                    interpolatedStringHandler.AppendLiteral(" flag: ");
                    interpolatedStringHandler.AppendFormatted<byte>(questWork.Flags);
                    ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                }
            }
        }

        public static string NameOfQuest(ushort id)
        {
            if (id > (ushort)0)
            {
                int digits = id.ToString().Length;
                if (QuestDebug.QuestSheet.Any<KeyValuePair<uint, Quest>>((Func<KeyValuePair<uint, Quest>, bool>)(x => (int)Convert.ToInt16(x.Value.Id.RawString.GetLast(digits)) == (int)id)))
                    return QuestDebug.QuestSheet.First<KeyValuePair<uint, Quest>>((Func<KeyValuePair<uint, Quest>, bool>)(x => (int)Convert.ToInt16(x.Value.Id.RawString.GetLast(digits)) == (int)id)).Value.Name.RawString.Replace("\uE0BE", "").Trim();
            }
            return "";
        }

        private Quest TrySearchQuest(string input)
        {
            List<(SeString, int)> list = this.questNames.Select<SeString, (SeString, int)>((Func<SeString, int, (SeString, int)>) ((n, i) => (n, i))).Where<(SeString, int)>((Func<(SeString, int), bool>) (t => !string.IsNullOrEmpty((string) t.n) && QuestDebug.IsMatch(input, (string) t.n))).ToList<(SeString, int)>();
            if (list.Count > 1)
                list = list.OrderByDescending<(SeString, int), object>((Func<(SeString, int), object>)(t => QuestDebug.MatchingScore((string)t.n, input))).ToList<(SeString, int)>();
            return list.Count <= 0 ? (Quest)null : this.questSheet.GetRow((uint)list.First<(SeString, int)>().Item2);
        }

        private static bool IsMatch(string x, string y)
        {
            return Regex.IsMatch(x, "\\b" + Regex.Escape(y) + "\\b");
        }

        private static object MatchingScore(string item, string line)
        {
            int num = 0;
            if (line.Contains(item))
                num += item.Length;
            return (object)num;
        }

        static QuestDebug()
        {
            IDataManager data = Svc.Data;
            Dictionary<uint, Quest> dictionary;
            if (data == null)
            {
                dictionary = (Dictionary<uint, Quest>)null;
            }
            else
            {
                ExcelSheet<Quest> excelSheet = data.GetExcelSheet<Quest>();
                dictionary = excelSheet != null ? excelSheet.Where<Quest>((Func<Quest, bool>)(x => x.Id.RawString.Length > 0)).ToDictionary<Quest, uint, Quest>((Func<Quest, uint>)(i => i.RowId), (Func<Quest, Quest>)(i => i)) : (Dictionary<uint, Quest>)null;
            }
            QuestDebug.QuestSheet = dictionary;
        }
    }
}
