using System;
using System.Collections.Generic;
using AetherBox.FeaturesSetup;
using AetherBox.IPC;
using ImGuiNET;
namespace AetherBox.Features.Disabled;
internal class CommandOnCondition : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Sets")]
        public List<CommandCondition> CommandConditions { get; set; } = new List<CommandCondition>();

    }

    [Serializable]
    public class CommandCondition
    {
        public int ConditionSet;

        public string Name { get; set; }

        public string Command { get; set; }

        public CommandCondition(string name = "Unnamed Set")
        {
            Name = name;
            ConditionSet = -1;
        }

        public bool CheckConditionSet()
        {
            if (ConditionSet >= 0)
            {
                if (QoLBarIPC.QoLBarEnabled)
                {
                    return QoLBarIPC.CheckConditionSet(ConditionSet);
                }
                return false;
            }
            return true;
        }
    }

    public override string Name => "Command on condition";

    public override string Description => "Execute a command when a condition is met.";

    public override FeatureType FeatureType => FeatureType.Disabled;

    public Configs Config { get; private set; }

    public override bool UseAutoConfig => false;

    protected override DrawConfigDelegate DrawConfigTree => delegate
    {
        foreach (CommandCondition current in Config.CommandConditions)
        {
            DrawPreset(current);
        }
    };

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        base.Disable();
    }

    public void DrawPreset(CommandCondition preset)
    {
        bool qolBarEnabled;
        qolBarEnabled = QoLBarIPC.QoLBarEnabled;
        string[] conditionSets;
        conditionSets = qolBarEnabled ? QoLBarIPC.QoLBarConditionSets : Array.Empty<string>();
        string display;
        display = preset.ConditionSet < 0 ? "None" : preset.ConditionSet < conditionSets.Length ? $"[{preset.ConditionSet + 1}] {conditionSets[preset.ConditionSet]}" : (preset.ConditionSet + 1).ToString();
        if (!ImGui.BeginCombo("Condition Set", display))
        {
            return;
        }
        if (ImGui.Selectable("None##ConditionSet", preset.ConditionSet < 0))
        {
            preset.ConditionSet = -1;
            SaveConfig(Config);
        }
        if (qolBarEnabled)
        {
            for (int i = 0; i < conditionSets.Length; i++)
            {
                string name;
                name = conditionSets[i];
                if (ImGui.Selectable($"[{i + 1}] {name}", i == preset.ConditionSet))
                {
                    preset.ConditionSet = i;
                    SaveConfig(Config);
                }
            }
        }
        ImGui.EndCombo();
    }
}
