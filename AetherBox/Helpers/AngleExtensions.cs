using System;
using AetherBox.Helpers;

namespace AetherBox.Helpers;

public static class AngleExtensions
{
    public static NumberHelper.Angle Radians(this float radians)
    {
        return new NumberHelper.Angle(radians);
    }

    public static NumberHelper.Angle Degrees(this float degrees)
    {
        return new NumberHelper.Angle(degrees * (MathF.PI / 180f));
    }

    public static NumberHelper.Angle Degrees(this int degrees)
    {
        return new NumberHelper.Angle((float)degrees * (MathF.PI / 180f));
    }
}
