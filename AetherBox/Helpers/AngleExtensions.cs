using System;

#nullable disable
namespace AetherBox.Helpers
{
    public static class AngleExtensions
    {
        public static NumberHelper.Angle Radians(this float radians) => new NumberHelper.Angle(radians);

        public static NumberHelper.Angle Degrees(this float degrees)
        {
            return new NumberHelper.Angle(degrees * ((float)Math.PI / 180f));
        }

        public static NumberHelper.Angle Degrees(this int degrees)
        {
            return new NumberHelper.Angle((float)degrees * ((float)Math.PI / 180f));
        }
    }
}
