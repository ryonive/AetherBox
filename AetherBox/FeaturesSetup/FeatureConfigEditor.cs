using ImGuiNET;
using System.Numerics;

#nullable disable
namespace AetherBox.FeaturesSetup
{
    public static class FeatureConfigEditor
    {
        public static bool ColorEditor(string name, ref object configOption)
        {
            switch (configOption)
            {
                case Vector4 vector4:
                    Vector4 col1 = vector4;
                    if (ImGui.ColorEdit4(name, ref col1))
                    {
                        configOption = (object)col1;
                        return true;
                    }
                    break;
                case Vector3 vector3:
                    Vector3 col2 = vector3;
                    if (ImGui.ColorEdit3(name, ref col2))
                    {
                        configOption = (object)col2;
                        return true;
                    }
                    break;
            }
            return false;
        }

        public static bool SimpleColorEditor(string name, ref object configOption)
        {
            if (!(configOption is Vector4 col) || !ImGui.ColorEdit4(name, ref col, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview))
                return false;
            configOption = (object)col;
            return true;
        }
    }
}
