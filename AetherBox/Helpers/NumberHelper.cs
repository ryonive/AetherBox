using System;
using System.Numerics;
using AetherBox.Helpers;

namespace AetherBox.Helpers;

public static class NumberHelper
{
	public struct Angle
	{
		public const float RadToDeg = 180f / MathF.PI;

		public const float DegToRad = MathF.PI / 180f;

		public float Rad;

		public float Deg => Rad * (180f / MathF.PI);

		public Angle(float radians = 0f)
		{
			Rad = radians;
		}

		public static Angle FromDirection(Vector2 dir)
		{
			return FromDirection(dir.X, dir.Y);
		}

		public static Angle FromDirection(float x, float z)
		{
			return new Angle(MathF.Atan2(x, z));
		}

		public readonly Vector2 ToDirection()
		{
			return new Vector2(Sin(), Cos());
		}

		public static Angle operator +(Angle a, Angle b)
		{
			return new Angle(a.Rad + b.Rad);
		}

		public static Angle operator -(Angle a, Angle b)
		{
			return new Angle(a.Rad - b.Rad);
		}

		public static Angle operator -(Angle a)
		{
			return new Angle(0f - a.Rad);
		}

		public static Angle operator *(Angle a, float b)
		{
			return new Angle(a.Rad * b);
		}

		public static Angle operator *(float a, Angle b)
		{
			return new Angle(a * b.Rad);
		}

		public static Angle operator /(Angle a, float b)
		{
			return new Angle(a.Rad / b);
		}

		public readonly Angle Abs()
		{
			return new Angle(Math.Abs(Rad));
		}

		public readonly float Sin()
		{
			return MathF.Sin(Rad);
		}

		public readonly float Cos()
		{
			return MathF.Cos(Rad);
		}

		public readonly float Tan()
		{
			return MathF.Tan(Rad);
		}

		public static Angle Asin(float x)
		{
			return new Angle(MathF.Asin(x));
		}

		public static Angle Acos(float x)
		{
			return new Angle(MathF.Acos(x));
		}

		public readonly Angle Normalized()
		{
			float r;
			for (r = Rad; r < -MathF.PI; r += MathF.PI * 2f)
			{
			}
			while (r > MathF.PI)
			{
				r -= MathF.PI * 2f;
			}
			return new Angle(r);
		}

		public readonly bool AlmostEqual(Angle other, float epsRad)
		{
			float delta;
			delta = Math.Abs(Rad - other.Rad);
			if (!(delta <= epsRad))
			{
				return delta >= MathF.PI * 2f - epsRad;
			}
			return true;
		}

		public static bool operator ==(Angle l, Angle r)
		{
			return l.Rad == r.Rad;
		}

		public static bool operator !=(Angle l, Angle r)
		{
			return l.Rad != r.Rad;
		}

		public override readonly bool Equals(object? obj)
		{
			if (obj is Angle angle)
			{
				return this == angle;
			}
			return false;
		}

		public override readonly int GetHashCode()
		{
			return Rad.GetHashCode();
		}

		public override string ToString()
		{
			return Deg.ToString("f0");
		}
	}

	public static int RoundOff(this int i, int sliderIncrement)
	{
		double sliderAsDouble;
		sliderAsDouble = Convert.ToDouble(sliderIncrement);
		return (int)Math.Round((double)i / sliderAsDouble) * sliderIncrement;
	}

	public static float RoundOff(this float i, float sliderIncrement)
	{
		return (float)Math.Round(i / sliderIncrement) * sliderIncrement;
	}

	public static string FormatTimeSpan(DateTime time)
	{
		TimeSpan span;
		span = DateTime.UtcNow - time;
		if (span.Days > 0)
		{
			return $"{span.Days} days ago";
		}
		if (span.Hours > 0)
		{
			return $"{span.Hours} hours ago";
		}
		if (span.Minutes > 0)
		{
			return $"{span.Minutes} minutes ago";
		}
		if (span.Seconds > 10)
		{
			return $"{span.Seconds} seconds ago";
		}
		return "now";
	}
}
