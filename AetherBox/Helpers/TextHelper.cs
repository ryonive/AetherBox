// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Helpers.TextHelper
using System.Globalization;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Text;
namespace AetherBox.Helpers;
public static class TextHelper
{
	public static string ToTitleCase(this string s)
	{
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());
	}

	public static string ParseSeStringLumina(Lumina.Text.SeString? luminaString)
	{
		if (luminaString != null)
		{
			return Dalamud.Game.Text.SeStringHandling.SeString.Parse(luminaString.RawData).TextValue;
		}
		return string.Empty;
	}

	public static string GetLast(this string source, int tail_length)
	{
		if (tail_length >= source.Length)
		{
			return source;
		}
		return source.Substring(source.Length - tail_length);
	}
}
