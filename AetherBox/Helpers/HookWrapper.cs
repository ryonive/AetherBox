using System;
using Dalamud.Hooking;

namespace AetherBox.Helpers
{
    public class HookWrapper<T> : IHookWrapper, IDisposable where T : Delegate
    {
        private Hook<T> wrappedHook;
        private bool disposed;

        public HookWrapper(Hook<T> hook) => wrappedHook = hook;

        public void Enable()
        {
            if (disposed)
                return;
            wrappedHook?.Enable();
        }

        public void Disable()
        {
            if (disposed)
                return;
            wrappedHook?.Disable();
        }

        public void Dispose()
        {
            Disable();
            disposed = true;
            wrappedHook?.Dispose();
        }

        public IntPtr Address => wrappedHook.Address;

        public T Original => wrappedHook.Original;

        public bool IsEnabled => wrappedHook.IsEnabled;

        public bool IsDisposed => wrappedHook.IsDisposed;
    }
    public interface IHookWrapper : IDisposable
    {
        void Enable();

        void Disable();

        bool IsEnabled { get; }

        bool IsDisposed { get; }
    }
}
