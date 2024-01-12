using System;
using AetherBox.Helpers;
using Dalamud.Hooking;

namespace AetherBox.Helpers;

public class HookWrapper<T> : IHookWrapper, IDisposable where T : Delegate
{
	private Hook<T> wrappedHook;

	private bool disposed;

	public nint Address => wrappedHook.Address;

	public T Original => wrappedHook.Original;

	public bool IsEnabled => wrappedHook.IsEnabled;

	public bool IsDisposed => wrappedHook.IsDisposed;

	public HookWrapper(Hook<T> hook)
	{
		wrappedHook = hook;
	}

	public void Enable()
	{
		if (!disposed)
		{
			wrappedHook?.Enable();
		}
	}

	public void Disable()
	{
		if (!disposed)
		{
			wrappedHook?.Disable();
		}
	}

	public void Dispose()
	{
		Disable();
		disposed = true;
		wrappedHook?.Dispose();
	}
}
