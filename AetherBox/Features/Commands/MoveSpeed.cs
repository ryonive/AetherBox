using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.Features.Debugging;
using AetherBox.FeaturesSetup;
using ECommons.DalamudServices;

namespace AetherBox.Features.Commands;

public class MoveSpeed : CommandFeature
{
    internal static float offset = 6f;

    public override string Name => "Modify Movement Speed";

    public override string Command { get; set; } = "/movespeed";


    public override string[] Alias => new string[2] { "/move", "/speed" };

    public override string Description => "";

    public override List<string> Parameters => new List<string> { "[<speed>]" };

    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Commands;

    protected override void OnCommand(List<string> args)
    {
        try
        {
            if (args.Count == 0)
            {
                PositionDebug.SetSpeed(offset);
                return;
            }
            float speed;
            speed = float.Parse(args[0]);
            PositionDebug.SetSpeed(speed * offset);
            Svc.Log.Info($"Setting move speed to {speed}");
        }
        catch
        {
        }
    }
}
