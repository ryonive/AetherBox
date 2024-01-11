using System.Collections.Generic;
using System.Linq;
using Dalamud;
using ECommons.DalamudServices;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Helpers
{
    internal static class TaskLookup
    {
        public static List<uint> GetInstanceListFromID(uint id)
        {
            WeeklyBingoOrderData bingoOrderData;
            bingoOrderData = Svc.Data.GetExcelSheet<WeeklyBingoOrderData>().GetRow(id);
            if (bingoOrderData == null)
            {
                return new List<uint>();
            }
            switch (bingoOrderData.Type)
            {
                case 0u:
                    return (from c in Svc.Data.GetExcelSheet<ContentFinderCondition>()
                            where c.Content == bingoOrderData.Data
                            select c into row
                            orderby row.SortKey
                            select row into c
                            select c.TerritoryType.Row).ToList();
                case 1u:
                    return (from m in Svc.Data.GetExcelSheet<ContentFinderCondition>()
                            where m.ContentType.Row == 2
                            where m.ClassJobLevelRequired == bingoOrderData.Data
                            select m into row
                            orderby row.SortKey
                            select row into m
                            select m.TerritoryType.Row).ToList();
                case 2u:
                    return (from m in Svc.Data.GetExcelSheet<ContentFinderCondition>()
                            where m.ContentType.Row == 2
                            where m.ClassJobLevelRequired >= bingoOrderData.Data - ((bingoOrderData.Data > 50) ? 9 : 49) && m.ClassJobLevelRequired <= bingoOrderData.Data - 1
                            select m into row
                            orderby row.SortKey
                            select row into m
                            select m.TerritoryType.Row).ToList();
                case 3u:
                    return bingoOrderData.Unknown5 switch
                    {
                        1 => new List<uint>(),
                        2 => new List<uint>(),
                        3 => (from m in Svc.Data.GetExcelSheet<ContentFinderCondition>()
                              where m.ContentType.Row == 21
                              select m into row
                              orderby row.SortKey
                              select row into m
                              select m.TerritoryType.Row).ToList(),
                        _ => new List<uint>(),
                    };
                case 4u:
                {
                    int raidIndex;
                    raidIndex = (int)((bingoOrderData.Data - 11) * 2);
                    switch (bingoOrderData.Data)
                    {
                        case 2u:
                            return new List<uint> { 241u, 242u, 243u, 244u, 245u };
                        case 3u:
                            return new List<uint> { 355u, 356u, 357u, 358u };
                        case 4u:
                            return new List<uint> { 193u, 194u, 195u, 196u };
                        case 5u:
                            return new List<uint> { 442u, 443u, 444u, 445u };
                        case 6u:
                            return new List<uint> { 520u, 521u, 522u, 523u };
                        case 7u:
                            return new List<uint> { 580u, 581u, 582u, 583u };
                        case 8u:
                            return new List<uint> { 691u, 692u, 693u, 694u };
                        case 9u:
                            return new List<uint> { 748u, 749u, 750u, 751u };
                        case 10u:
                            return new List<uint> { 798u, 799u, 800u, 801u };
                        default:
                            return (from row in Svc.Data.GetExcelSheet<ContentFinderCondition>(ClientLanguage.English)
                                    where row.ContentType.Row == 5
                                    where row.ContentMemberType.Row == 3
                                    where !row.Name.RawString.Contains("Savage")
                                    where row.ItemLevelRequired >= 425
                                    orderby row.SortKey
                                    select row.TerritoryType.Row).ToArray()[raidIndex..(raidIndex + 2)].ToList();
                        case 0u:
                        case 1u:
                            return new List<uint>();
                    }
                }
                default:
                    Svc.Log.Information($"[WondrousTails] Unrecognized ID: {id}");
                    return new List<uint>();
            }
        }
    }
}