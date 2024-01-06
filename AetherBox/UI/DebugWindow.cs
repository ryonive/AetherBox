using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Xml.Linq;

#nullable disable
namespace AetherBox.UI;

internal class DebugWindow : Window
{
    public DebugWindow()
    {
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 4);
        interpolatedStringHandler.AppendFormatted(AetherBox.Name);
        interpolatedStringHandler.AppendLiteral(" - Debug ");
        interpolatedStringHandler.AppendFormatted<Version>(AetherBox.Plugin.GetType().Assembly.GetName().Version);
        interpolatedStringHandler.AppendLiteral("###");
        interpolatedStringHandler.AppendFormatted(AetherBox.Name);
        interpolatedStringHandler.AppendFormatted(nameof(DebugWindow));
        // ISSUE: explicit constructor call
        base.\u002Ector(interpolatedStringHandler.ToStringAndClear());
        this.SizeConstraints = new Window.WindowSizeConstraints?(new Window.WindowSizeConstraints()
        {
            MinimumSize = new Vector2(375f, 330f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        });
    }

    public static void Dispose()
    {
    }

    public override void Draw() => DebugManager.DrawDebugWindow();
}
