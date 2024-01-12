using System;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Helpers;

public static class Misc
{
	public static ExcelSheet<Lumina.Excel.GeneratedSheets.Action> Action;

	public static ExcelSheet<AozAction> AozAction;

	public static ExcelSheet<AozActionTransient> AozActionTransient;

	public static uint AozToNormal(uint id)
	{
		if (id == 0)
		{
			return 0u;
		}
		return AozAction.GetRow(id).Action.Row;
	}

	public static uint NormalToAoz(uint id)
	{
		foreach (AozAction aozAction in AozAction)
		{
			if (aozAction.Action.Row == id)
			{
				return aozAction.RowId;
			}
		}
		throw new Exception("https://tenor.com/view/8032213");
	}
}
