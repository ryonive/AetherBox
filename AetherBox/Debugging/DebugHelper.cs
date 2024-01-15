using System;
using AetherBox.Features;

namespace AetherBox.Debugging;

public abstract class DebugHelper : IDisposable
{
    public AetherBox Plugin;

    public FeatureProvider FeatureProvider;

    public abstract string Name { get; }

    public string FullName => Name;

    public abstract void Draw();

    public virtual void Dispose()
    {
    }
}

