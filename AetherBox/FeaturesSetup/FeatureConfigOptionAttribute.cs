using System;
using System.Reflection;

#nullable disable
namespace AetherBox.FeaturesSetup
{
    [AttributeUsage(AttributeTargets.All)]
    public class FeatureConfigOptionAttribute : Attribute
    {
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

        public FeatureConfigOptionAttribute.NumberEditType IntType { get; set; }

        public float FloatMin { get; set; } = float.MinValue;

        public float FloatMax { get; set; } = (float)int.MaxValue;

        public FeatureConfigOptionAttribute.NumberEditType FloatType { get; set; }

        public bool EnforcedLimit { get; set; } = true;

        public MethodInfo Editor { get; set; }

        public uint SelectedValue { get; set; }

        public FeatureConfigOptionAttribute(string name) => this.Name = name;

        public FeatureConfigOptionAttribute(
          string name,
          string editorType,
          int priority = 0,
          string localizeKey = null)
        {
            this.Name = name;
            this.Priority = priority;
            this.LocalizeKey = localizeKey ?? name;
            this.Editor = typeof(FeatureConfigEditor).GetMethod(editorType + nameof(Editor), BindingFlags.Static | BindingFlags.Public);
        }

        public FeatureConfigOptionAttribute(string name, uint selectedValue = 0)
        {
            this.Name = name;
            this.SelectedValue = selectedValue;
        }

        public delegate bool ConfigOptionEditor(string name, ref object configOption);

        public enum NumberEditType
        {
            Slider,
            Drag,
        }
    }
}
