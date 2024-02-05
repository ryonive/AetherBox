using System;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Helpers.NPCLocations;
public class NpcLocation
{
    public TerritoryType TerritoryExcel { get; set; }

    public float MapX => ToMapCoordinate(X, (int)TerritoryExcel.Map.Value.SizeFactor, TerritoryExcel.Map.Value.OffsetX);

    public float MapY => ToMapCoordinate(Y, (int)TerritoryExcel.Map.Value.SizeFactor, TerritoryExcel.Map.Value.OffsetY);

    public float X { get; }

    public float Y { get; }

    public uint TerritoryType => TerritoryExcel.RowId;

    public uint MapId { get; }

    public NpcLocation(float x, float y, TerritoryType territoryType, uint? map = null)
    {
        X = x;
        Y = y;
        TerritoryExcel = territoryType;
        MapId = (map.HasValue ? map.Value : territoryType.Map.Row);
    }

    private static float ToMapCoordinate(float val, float scale, short offset)
    {
        float c;
        c = scale / 100f;
        val = (val + (float)offset) * c;
        return 41f / c * ((val + 1024f) / 2048f) + 1f;
    }
}
