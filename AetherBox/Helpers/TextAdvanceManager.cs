// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Helpers.TextAdvanceManager
using System.Collections.Generic;
using AetherBox;
using AetherBox.Helpers;
using ECommons.DalamudServices;
namespace AetherBox.Helpers;
internal static class TextAdvanceManager
{
	private static bool WasChanged;

	private static bool IsBusy => FeatureHelper.IsBusy;

	internal static void Tick()
	{
		if (WasChanged && !IsBusy)
		{
			WasChanged = false;
			UnlockTA();
		}
		if (IsBusy)
		{
			WasChanged = true;
			LockTA();
		}
	}

	internal static void LockTA()
	{
		if (Svc.PluginInterface.TryGetData<HashSet<string>>("TextAdvance.StopRequests", out HashSet<string> data))
		{
			data.Add(global::AetherBox.AetherBox.Name);
		}
	}

	internal static void UnlockTA()
	{
		if (Svc.PluginInterface.TryGetData<HashSet<string>>("TextAdvance.StopRequests", out HashSet<string> data))
		{
			data.Remove(global::AetherBox.AetherBox.Name);
		}
	}
}
