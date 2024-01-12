using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AetherBox.Helpers;
using AetherBox.Helpers.Faloop;
using AetherBox.Helpers.Faloop.Model.Embed;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ECommons.Logging;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Helpers;

public static class CoordinatesHelper
{
	public class MapLinkMessage
	{
		public ushort ChatType;

		public string Sender;

		public string Text;

		public float X;

		public float Y;

		public float Scale;

		public uint TerritoryId;

		public string PlaceName;

		public DateTime RecordTime;

		public static MapLinkMessage Empty => new MapLinkMessage(0, string.Empty, string.Empty, 0f, 0f, 100f, 0u, string.Empty, DateTime.Now);

		public MapLinkMessage(ushort chatType, string sender, string text, float x, float y, float scale, uint territoryId, string placeName, DateTime recordTime)
		{
			ChatType = chatType;
			Sender = sender;
			Text = text;
			X = x;
			Y = y;
			Scale = scale;
			TerritoryId = territoryId;
			PlaceName = placeName;
			RecordTime = recordTime;
		}
	}

	public static ExcelSheet<Aetheryte> Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage);

	public static ExcelSheet<MapMarker> AetherytesMap = Svc.Data.GetExcelSheet<MapMarker>(Svc.ClientState.ClientLanguage);

	public static string GetNearestAetheryte(MapLinkMessage maplinkMessage)
	{
		string aetheryteName;
		aetheryteName = "";
		double distance;
		distance = 0.0;
		foreach (Aetheryte data in Aetherytes)
		{
			if (!data.IsAetheryte || data.Territory.Value == null || data.PlaceName.Value == null)
			{
				continue;
			}
			float scale;
			scale = maplinkMessage.Scale;
			if (data.Territory.Value.RowId != maplinkMessage.TerritoryId)
			{
				continue;
			}
			MapMarker mapMarker;
			mapMarker = AetherytesMap.FirstOrDefault((MapMarker m) => m.DataType == 3 && m.DataKey == data.RowId);
			if (mapMarker == null)
			{
				Svc.Log.Error($"Cannot find aetherytes position for {maplinkMessage.PlaceName}#{data.PlaceName.Value.Name}");
				continue;
			}
			float AethersX;
			AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.X, scale);
			float AethersY;
			AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Y, scale);
			double temp_distance;
			temp_distance = Math.Pow(AethersX - maplinkMessage.X, 2.0) + Math.Pow(AethersY - maplinkMessage.Y, 2.0);
			if (aetheryteName == "" || temp_distance < distance)
			{
				distance = temp_distance;
				aetheryteName = data.PlaceName.Value.Name;
			}
		}
		return aetheryteName;
	}

	public static string GetNearestAetheryte(Vector3 pos, TerritoryType map)
	{
		MapLinkPayload MapLink;
		MapLink = new MapLinkPayload(map.RowId, map.Map.Row, (int)pos.X * 1000, (int)pos.Z * 1000);
		return GetNearestAetheryte(new MapLinkMessage(0, "", "", MapLink.XCoord, MapLink.YCoord, 100f, map.RowId, "", DateTime.Now));
	}

	private static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
	{
		float num;
		num = scale / 100f;
		return ConvertRawPositionToMapCoordinate((int)((float)((double)pos - 1024.0) / num * 1000f), scale);
	}

	private static float ConvertRawPositionToMapCoordinate(int pos, float scale)
	{
		float num;
		num = scale / 100f;
		return (float)(((double)((float)pos / 1000f * num) + 1024.0) / 2048.0 * 41.0 / (double)num + 1.0);
	}

	public static void TeleportToAetheryte(MapLinkMessage maplinkMessage)
	{
		string aetheryteName;
		aetheryteName = GetNearestAetheryte(maplinkMessage);
		if (aetheryteName != "")
		{
			Svc.Log.Debug("Teleporting to " + aetheryteName);
			Svc.Commands.ProcessCommand("/tp " + aetheryteName);
			return;
		}
		Svc.Log.Error($"Cannot find nearest aetheryte of {maplinkMessage.PlaceName}({maplinkMessage.X}, {maplinkMessage.Y}).");
	}

	public static SeString? CreateMapLink(uint zoneId, int zonePoiId, int? instance, FaloopSession session)
	{
		TerritoryType zone;
		zone = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRow(zoneId);
		Map map;
		map = zone?.Map.Value;
		if (zone == null || map == null)
		{
			Svc.Log.Debug("CreateMapLink: zone == null || map == null");
			return null;
		}
		ZoneLocationData location;
		location = session.EmbedData.ZoneLocations.FirstOrDefault((ZoneLocationData x) => x.Id == zonePoiId);
		if (location == null)
		{
			Svc.Log.Debug("CreateMapLink: location == null");
			return null;
		}
		double i;
		i = 41.0 / ((double)(int)map.SizeFactor / 100.0);
		List<float> loc;
		loc = (from x in location.Location.Split(new char[1] { ',' }, 2).Select(int.Parse)
			select (double)x / 2048.0 * i + 1.0 into x
			select Math.Round(x, 1) into x
			select (float)x).ToList();
		SeString mapLink;
		mapLink = SeString.CreateMapLink(zone.RowId, zone.Map.Row, loc[0], loc[1]);
		TextPayload instanceIcon;
		instanceIcon = GetInstanceIcon(instance);
		if (instanceIcon == null)
		{
			return mapLink;
		}
		return mapLink.Append(instanceIcon);
	}

	private static TextPayload? GetInstanceIcon(int? instance)
	{
		return instance switch
		{
			1 => new TextPayload(SeIconChar.Instance1.ToIconString()), 
			2 => new TextPayload(SeIconChar.Instance2.ToIconString()), 
			3 => new TextPayload(SeIconChar.Instance3.ToIconString()), 
			_ => null, 
		};
	}
}
