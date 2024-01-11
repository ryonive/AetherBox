using System.Numerics;
using ImGuiNET;

namespace AetherBox.FeaturesSetup;

public static class FeatureConfigEditor
{
    public static bool ColorEditor(string name, ref object configOption)
    {
        object obj;
        obj = configOption;
        if (!(obj is Vector4 vector))
        {
            if (obj is Vector3 vector2)
            {
                Vector3 v3;
                v3 = vector2;
                if (ImGui.ColorEdit3(name, ref v3))
                {
                    configOption = v3;
                    return true;
                }
            }
        }
        else
        {
            Vector4 v4;
            v4 = vector;
            if (ImGui.ColorEdit4(name, ref v4))
            {
                configOption = v4;
                return true;
            }
        }
        return false;
    }

    public static bool SimpleColorEditor(string name, ref object configOption)
    {
        if (configOption is Vector4 v4 && ImGui.ColorEdit4(name, ref v4, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview))
        {
            configOption = v4;
            return true;
        }
        return false;
    }
}
