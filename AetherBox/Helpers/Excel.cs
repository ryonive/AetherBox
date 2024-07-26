using Dalamud.Game;
using ECommons.DalamudServices;
using Lumina.Excel;

namespace AetherBox.Helpers;

public static class Excel
{
    public static ExcelSheet<T> GetSheet<T>(ClientLanguage? language = null) where T : ExcelRow
        => Svc.Data.GetExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage)!;

    public static uint GetRowCount<T>() where T : ExcelRow
        => GetSheet<T>().RowCount;

    public static T? GetRow<T>(uint rowId, uint subRowId = uint.MaxValue, ClientLanguage? language = null) where T : ExcelRow
        => GetSheet<T>(language).GetRow(rowId, subRowId);

    public static T? FindRow<T>(Func<T?, bool> predicate) where T : ExcelRow
        => GetSheet<T>().FirstOrDefault(predicate, null);

    public static IEnumerable<T> FindRows<T>(Func<T?, bool> predicate) where T : ExcelRow
        => GetSheet<T>().Where(predicate);
}
