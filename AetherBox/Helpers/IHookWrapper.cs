using System;

namespace AetherBox.Helpers;

public interface IHookWrapper : IDisposable
{
    bool IsEnabled { get; }

    bool IsDisposed { get; }

    void Enable();

    void Disable();
}
