// AetherBox, Version=69.3.0.0, Culture=neutral, PublicKeyToken=null
// AetherBox.Features.Debugging.HousingZoneExtensions
using System;
using AetherBox.Features.Debugging;
namespace AetherBox.Features.Debugging;
public static class HousingZoneExtensions
{
	public static string ToName(this HousingDebug.HousingZone z)
	{
		return z switch
		{
			HousingDebug.HousingZone.Unknown => "Unknown", 
			HousingDebug.HousingZone.Mist => "Mist", 
			HousingDebug.HousingZone.Goblet => "The Goblet", 
			HousingDebug.HousingZone.LavenderBeds => "Lavender Beds", 
			HousingDebug.HousingZone.Shirogane => "Shirogane", 
			HousingDebug.HousingZone.Firmament => "Firmament", 
			_ => throw new ArgumentOutOfRangeException("z", z, null), 
		};
	}
}
