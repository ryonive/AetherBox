using System.Numerics;
using AetherBox;
using AetherBox.Debugging;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
namespace AetherBox.UI;
internal class DebugWindow : Window
{


    public DebugWindow()
        : base($"{AetherBox.Name} - Debug {AetherBox.Plugin.GetType().Assembly.GetName().Version}###{AetherBox.Name}{"DebugWindow"}")
    {
        base.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(750, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public static void Dispose()
    {
    }

    public override void Draw()
    {
        DebugManager.DrawDebugWindow();
    }
}
