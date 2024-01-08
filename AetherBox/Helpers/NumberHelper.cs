using System;
using System.Numerics;
using System.Runtime.CompilerServices;

#nullable enable
namespace AetherBox.Helpers
{
    public static class NumberHelper
    {
        public static int RoundOff(this int i, int sliderIncrement)
        {
            var num = Convert.ToDouble(sliderIncrement);
            return (int)Math.Round((double)i / num) * sliderIncrement;
        }

        public static float RoundOff(this float i, float sliderIncrement)
        {
            return (float)Math.Round((double)i / (double)sliderIncrement) * sliderIncrement;
        }

        public static
#nullable disable
        string FormatTimeSpan(DateTime time)
        {
            var timeSpan = DateTime.UtcNow - time;
            if (timeSpan.Days > 0)
            {
                var interpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 1);
                interpolatedStringHandler.AppendFormatted<int>(timeSpan.Days);
                interpolatedStringHandler.AppendLiteral(" days ago");
                return interpolatedStringHandler.ToStringAndClear();
            }
            if (timeSpan.Hours > 0)
            {
                var interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                interpolatedStringHandler.AppendFormatted<int>(timeSpan.Hours);
                interpolatedStringHandler.AppendLiteral(" hours ago");
                return interpolatedStringHandler.ToStringAndClear();
            }
            if (timeSpan.Minutes > 0)
            {
                var interpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
                interpolatedStringHandler.AppendFormatted<int>(timeSpan.Minutes);
                interpolatedStringHandler.AppendLiteral(" minutes ago");
                return interpolatedStringHandler.ToStringAndClear();
            }
            if (timeSpan.Seconds <= 10)
                return "now";
            var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(12, 1);
            interpolatedStringHandler1.AppendFormatted<int>(timeSpan.Seconds);
            interpolatedStringHandler1.AppendLiteral(" seconds ago");
            return interpolatedStringHandler1.ToStringAndClear();
        }

        public struct Angle
        {
            public const float RadToDeg = 57.2957764f;
            public const float DegToRad = 0.0174532924f;
            public float Rad;

            public Angle(float radians = 0.0f) => this.Rad = radians;

            public float Deg => this.Rad * 57.2957764f;

            public static NumberHelper.Angle FromDirection(Vector2 dir)
            {
                return NumberHelper.Angle.FromDirection(dir.X, dir.Y);
            }

            public static NumberHelper.Angle FromDirection(float x, float z)
            {
                return new NumberHelper.Angle(MathF.Atan2(x, z));
            }

            public readonly Vector2 ToDirection() => new Vector2(this.Sin(), this.Cos());

            public static NumberHelper.Angle operator +(NumberHelper.Angle a, NumberHelper.Angle b)
            {
                return new NumberHelper.Angle(a.Rad + b.Rad);
            }

            public static NumberHelper.Angle operator -(NumberHelper.Angle a, NumberHelper.Angle b)
            {
                return new NumberHelper.Angle(a.Rad - b.Rad);
            }

            public static NumberHelper.Angle operator -(NumberHelper.Angle a)
            {
                return new NumberHelper.Angle(-a.Rad);
            }

            public static NumberHelper.Angle operator *(NumberHelper.Angle a, float b)
            {
                return new NumberHelper.Angle(a.Rad * b);
            }

            public static NumberHelper.Angle operator *(float a, NumberHelper.Angle b)
            {
                return new NumberHelper.Angle(a * b.Rad);
            }

            public static NumberHelper.Angle operator /(NumberHelper.Angle a, float b)
            {
                return new NumberHelper.Angle(a.Rad / b);
            }

            public readonly NumberHelper.Angle Abs() => new NumberHelper.Angle(Math.Abs(this.Rad));

            public readonly float Sin() => MathF.Sin(this.Rad);

            public readonly float Cos() => MathF.Cos(this.Rad);

            public readonly float Tan() => MathF.Tan(this.Rad);

            public static NumberHelper.Angle Asin(float x) => new NumberHelper.Angle(MathF.Asin(x));

            public static NumberHelper.Angle Acos(float x) => new NumberHelper.Angle(MathF.Acos(x));

            public readonly NumberHelper.Angle Normalized()
            {
                var rad = this.Rad;
                while ((double)rad < -3.1415927410125732)
                    rad += 6.28318548f;
                while ((double)rad > 3.1415927410125732)
                    rad -= 6.28318548f;
                return new NumberHelper.Angle(rad);
            }

            public readonly bool AlmostEqual(NumberHelper.Angle other, float epsRad)
            {
                var num = Math.Abs(this.Rad - other.Rad);
                return (double)num <= (double)epsRad || (double)num >= 6.2831854820251465 - (double)epsRad;
            }

            public static bool operator ==(NumberHelper.Angle l, NumberHelper.Angle r)
            {
                return (double)l.Rad == (double)r.Rad;
            }

            public static bool operator !=(NumberHelper.Angle l, NumberHelper.Angle r)
            {
                return (double)l.Rad != (double)r.Rad;
            }

            public override readonly bool Equals(
#nullable enable
            object? obj) => obj is NumberHelper.Angle angle && this == angle;

            public override readonly int GetHashCode() => this.Rad.GetHashCode();

            public override
#nullable disable
            string ToString() => this.Deg.ToString("f0");
        }
    }
}
