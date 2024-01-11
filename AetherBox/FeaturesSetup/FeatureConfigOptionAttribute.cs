using System;
using System.Reflection;
using AetherBox.FeaturesSetup;

namespace AetherBox.FeaturesSetup;

[AttributeUsage(AttributeTargets.All)]
public class FeatureConfigOptionAttribute : Attribute
{
    public delegate bool ConfigOptionEditor(string name, ref object configOption);

    public enum NumberEditType
    {
        Slider = 0,
        Drag = 1
    }

    public int IntIncrements = 1;

    public float FloatIncrements = 0.1f;

    public string Format = "%.1f";

    public string Name { get; }

    public string HelpText { get; set; } = "";


    public string LocalizeKey { get; }

    public int Priority { get; }

    public int EditorSize { get; set; } = -1;


    public bool SameLine { get; set; }

    public bool ConditionalDisplay { get; set; }

    public int IntMin { get; set; } = int.MinValue;


    public int IntMax { get; set; } = int.MaxValue;


    public NumberEditType IntType { get; set; }

    public float FloatMin { get; set; } = float.MinValue;


    public float FloatMax { get; set; } = 2.1474836E+09f;


    public NumberEditType FloatType { get; set; }

    public bool EnforcedLimit { get; set; } = true;


    public MethodInfo Editor { get; set; }

    public uint SelectedValue { get; set; }

    public FeatureConfigOptionAttribute(string name)
    {
        Name = name;
    }

    public FeatureConfigOptionAttribute(string name, string editorType, int priority = 0, string localizeKey = null)
    {
        Name = name;
        Priority = priority;
        LocalizeKey = localizeKey ?? name;
        Editor = typeof(FeatureConfigEditor).GetMethod(editorType + "Editor", BindingFlags.Static | BindingFlags.Public);
    }

    public FeatureConfigOptionAttribute(string name, uint selectedValue = 0u)
    {
        Name = name;
        SelectedValue = selectedValue;
    }
}
