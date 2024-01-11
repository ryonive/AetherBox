using AetherBox.Helpers;
using Dalamud.Logging;
using ECommons.Reflection;

namespace AetherBox.Helpers;

internal static class YesAlready
{
	internal static bool Reenable;

	internal static void DisableIfNeeded()
	{
		if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, suppressErrors: false, ignoreCache: true))
		{
			PluginLog.Information("Disabling Yes Already");
			pl.GetStaticFoP("YesAlready.Service", "Configuration").SetFoP("Enabled", false);
			Reenable = true;
		}
	}

	internal static void EnableIfNeeded()
	{
		if (Reenable && DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, suppressErrors: false, ignoreCache: true))
		{
			PluginLog.Information("Enabling Yes Already");
			pl.GetStaticFoP("YesAlready.Service", "Configuration").SetFoP("Enabled", true);
			Reenable = false;
		}
	}

	internal static bool IsEnabled()
	{
		if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, suppressErrors: false, ignoreCache: true))
		{
			return pl.GetStaticFoP("YesAlready.Service", "Configuration").GetFoP<bool>("Enabled");
		}
		return false;
	}

	internal static bool? WaitForYesAlreadyDisabledTask()
	{
		return !IsEnabled();
	}

	internal static void Tick()
	{
		if (FeatureHelper.IsBusy)
		{
			if (IsEnabled())
			{
				DisableIfNeeded();
			}
		}
		else if (Reenable)
		{
			EnableIfNeeded();
		}
	}
}
