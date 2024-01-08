using AetherBox.Debugging;
using ImGuiNET;
using System;
using System.Linq;
using System.Reflection;

#nullable disable
namespace AetherBox.Features.Debugging
{
    public class FeatureDebug : DebugHelper
    {
        private readonly FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());

        public override string Name => nameof(FeatureDebug).Replace("Debug", "") + " Debugging";

        public override void Draw()
        {
            ImGui.Text(this.Name ?? "");
            ImGui.Separator();
            if (ImGui.Button("Load Features"))
            {
                this.provider.LoadFeatures();
                Automaton.P.FeatureProviders.Add(this.provider);
            }
            if (!ImGui.Button("Unload Features"))
                return;
            foreach (BaseFeature baseFeature in Automaton.P.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x => x != null && x.Enabled)))
                baseFeature.Disable();
            Automaton.P.FeatureProviders.Clear();
            this.provider.UnloadFeatures();
        }
    }
}
