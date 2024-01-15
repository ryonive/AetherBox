using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using AetherBox.Helpers.Faloop.Model;

namespace AetherBox.Helpers.Faloop.Model;

public static class MockData
{
    private static readonly MobReportData.Spawn Spawn = new MobReportData.Spawn
    {
        ZoneId = 399u,
        ZonePoiIds = new List<int> { 643 },
        Timestamp = DateTime.Parse("2022-12-09T12:06:33.031Z"),
        Window = 1
    };

    public static readonly MobReportData SpawnMobReport = new MobReportData
    {
        Action = "spawn",
        MobId = 4376u,
        WorldId = 52u,
        ZoneInstance = 0,
        Data = JsonObject.Create(JsonSerializer.SerializeToElement(Spawn, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }))
    };

    private static readonly MobReportData.Death Death = new MobReportData.Death
    {
        Num = 1,
        StartedAt = DateTime.Parse("2022-12-09T12:09:38.718Z"),
        PrevStartedAt = DateTime.Parse("2022-12-05T02:28:12.931Z")
    };

    public static readonly MobReportData DeathMobReport = new MobReportData
    {
        Action = "death",
        MobId = 4376u,
        WorldId = 52u,
        ZoneInstance = 0,
        Data = JsonObject.Create(JsonSerializer.SerializeToElement(Death, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }))
    };
}
