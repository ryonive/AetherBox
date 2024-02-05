// AetherBox, Version=69.5.0.1, Culture=neutral, PublicKeyToken=null
// AetherBox.IPC.QoLBarIPC
using System;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
namespace AetherBox.IPC;
internal class QoLBarIPC
{
	internal static string Name = "QoLBar";

	public static ICallGateSubscriber<object> qolBarInitializedSubscriber;

	public static ICallGateSubscriber<object> qolBarDisposedSubscriber;

	public static ICallGateSubscriber<string> qolBarGetVersionSubscriber;

	public static ICallGateSubscriber<int> qolBarGetIPCVersionSubscriber;

	public static ICallGateSubscriber<string[]> qolBarGetConditionSetsProvider;

	public static ICallGateSubscriber<int, bool> qolBarCheckConditionSetProvider;

	public static ICallGateSubscriber<int, int, object> qolBarMovedConditionSetProvider;

	public static ICallGateSubscriber<int, object> qolBarRemovedConditionSetProvider;

	public static bool QoLBarEnabled { get; private set; } = false;


	public static int QoLBarIPCVersion
	{
		get
		{
			try
			{
				return qolBarGetIPCVersionSubscriber.InvokeFunc();
			}
			catch
			{
				return 0;
			}
		}
	}

	public static string QoLBarVersion
	{
		get
		{
			try
			{
				return qolBarGetVersionSubscriber.InvokeFunc();
			}
			catch
			{
				return "0.0.0.0";
			}
		}
	}

	public static string[] QoLBarConditionSets
	{
		get
		{
			try
			{
				return qolBarGetConditionSetsProvider.InvokeFunc();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}
	}

	internal static void Init()
	{
		qolBarInitializedSubscriber = Svc.PluginInterface.GetIpcSubscriber<object>("QoLBar.Initialized");
		qolBarDisposedSubscriber = Svc.PluginInterface.GetIpcSubscriber<object>("QoLBar.Disposed");
		qolBarGetIPCVersionSubscriber = Svc.PluginInterface.GetIpcSubscriber<int>("QoLBar.GetIPCVersion");
		qolBarGetVersionSubscriber = Svc.PluginInterface.GetIpcSubscriber<string>("QoLBar.GetVersion");
		qolBarGetConditionSetsProvider = Svc.PluginInterface.GetIpcSubscriber<string[]>("QoLBar.GetConditionSets");
		qolBarCheckConditionSetProvider = Svc.PluginInterface.GetIpcSubscriber<int, bool>("QoLBar.CheckConditionSet");
		qolBarMovedConditionSetProvider = Svc.PluginInterface.GetIpcSubscriber<int, int, object>("QoLBar.MovedConditionSet");
		qolBarRemovedConditionSetProvider = Svc.PluginInterface.GetIpcSubscriber<int, object>("QoLBar.RemovedConditionSet");
		qolBarInitializedSubscriber.Subscribe(EnableQoLBarIPC);
		qolBarDisposedSubscriber.Subscribe(DisableQoLBarIPC);
		qolBarMovedConditionSetProvider.Subscribe(OnMovedConditionSet);
		qolBarRemovedConditionSetProvider.Subscribe(OnRemovedConditionSet);
		EnableQoLBarIPC();
	}

	public static void EnableQoLBarIPC()
	{
		if (QoLBarIPCVersion == 1)
		{
			QoLBarEnabled = true;
		}
	}

	public static void DisableQoLBarIPC()
	{
		if (QoLBarEnabled)
		{
			QoLBarEnabled = false;
		}
	}

	public static bool CheckConditionSet(int i)
	{
		try
		{
			return qolBarCheckConditionSetProvider.InvokeFunc(i);
		}
		catch
		{
			return false;
		}
	}

	private static void OnMovedConditionSet(int from, int to)
	{
	}

	private static void OnRemovedConditionSet(int removed)
	{
	}

	public static void Dispose()
	{
		qolBarInitializedSubscriber?.Unsubscribe(EnableQoLBarIPC);
		qolBarDisposedSubscriber?.Unsubscribe(DisableQoLBarIPC);
		qolBarMovedConditionSetProvider?.Unsubscribe(OnMovedConditionSet);
		qolBarRemovedConditionSetProvider?.Unsubscribe(OnRemovedConditionSet);
		QoLBarEnabled = false;
	}
}
