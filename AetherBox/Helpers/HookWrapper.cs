using System;
using Dalamud.Hooking;
using ECommons.DalamudServices;

namespace AetherBox.Helpers
{
    // This class is a wrapper for Dalamud's Hook class, providing additional functionality.
    public class HookWrapper<T> : IHookWrapper, IDisposable where T : Delegate
    {
        private readonly Hook<T> wrappedHook;
        private bool disposed;

        public nint Address => wrappedHook.Address;
        public T Original => wrappedHook.Original;
        public bool IsEnabled => wrappedHook.IsEnabled;
        public bool IsDisposed => wrappedHook.IsDisposed;

        // Constructor to initialize the HookWrapper with a Hook instance.
        public HookWrapper(Hook<T> hook)
        {
            wrappedHook = hook ?? throw new ArgumentNullException(nameof(hook));
        }

        // Enable the hook.
        public void Enable()
        {
            if (!disposed)
            {
                wrappedHook?.Enable();
                Debug("Hook enabled.");
            }
        }

        // Disable the hook.
        public void Disable()
        {
            if (!disposed)
            {
                wrappedHook?.Disable();
                Debug("Hook disabled.");
            }
        }

        // Dispose of the HookWrapper, disabling the hook and cleaning up resources.
        public void Dispose()
        {
            Disable();
            disposed = true;
            wrappedHook?.Dispose();
            Debug("HookWrapper disposed.");
        }

        // Log debug messages.
        private void Debug(string message)
        {
            Svc.Log.Debug(message);
            Svc.Chat.Print(message);
        }

        // Log error messages.
        private void Error(string message)
        {
            Svc.Log.Error(message);
            Svc.Chat.PrintError(message);
        }
    }
}
