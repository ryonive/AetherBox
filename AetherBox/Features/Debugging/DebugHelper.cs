using AetherBox.Features;
using System;

#nullable disable
namespace AetherBox.Debugging
{
    public abstract class DebugHelper : IDisposable
    {
        public AetherBox Plugin;
        public FeatureProvider FeatureProvider;

        public abstract void Draw();

        public abstract string Name { get; }

        public virtual void Dispose()
        {
        }

        public string FullName => this.Name;
    }
}
