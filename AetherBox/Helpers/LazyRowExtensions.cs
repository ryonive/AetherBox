// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Helpers.LazyRowExtensions
using Dalamud;
using ECommons.DalamudServices;
using Lumina.Excel;
namespace AetherBox.Helpers;
public static class LazyRowExtensions
{
	public static LazyRow<T> GetDifferentLanguage<T>(this LazyRow<T> row, ClientLanguage language) where T : ExcelRow
	{
		return new LazyRow<T>(Svc.Data.GameData, row.Row, language.ToLumina());
	}
}
