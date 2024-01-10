using AetherBox.Debugging;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;

namespace AetherBox.Features.Debugging
{
    public class SliderCanvas : DebugHelper
    {
        private readonly Stopwatch sw = new Stopwatch();
        private const int BarSpacing = 4;
        private const int BarCount = 64;

        public override string Name => nameof(SliderCanvas).Replace("Debug", "") + " Debugging";

        public override void Draw()
        {
            if (!this.sw.IsRunning)
                this.sw.Restart();
            Vector2 contentRegionAvail1 = ImGui.GetContentRegionAvail();
            float num1 = (double) contentRegionAvail1.X > (double) contentRegionAvail1.Y ? contentRegionAvail1.Y : contentRegionAvail1.X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (float)((double)contentRegionAvail1.X / 2.0 - (double)num1 / 2.0));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, uint.MaxValue);
            if (ImGui.BeginChild("sliderLand", new Vector2(num1) * 0.8f, true))
            {
                ImDrawListPtr foregroundDrawList = ImGui.GetForegroundDrawList();
                Vector2 contentRegionAvail2 = ImGui.GetContentRegionAvail();
                float radius = (float) ((double) contentRegionAvail2.X / 64.0 - 4.0);
                Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
                foregroundDrawList.AddRectFilled(cursorScreenPos - new Vector2(50f), cursorScreenPos + contentRegionAvail2 + new Vector2(100f), uint.MaxValue);
                for (int index = 0; index < 64; ++index)
                    foregroundDrawList.AddRectFilled(cursorScreenPos + new Vector2((float)(index * 4) + (float)index * radius, 0.0f), cursorScreenPos + new Vector2((float)(index * 4) + (float)(index + 1) * radius, contentRegionAvail2.Y), 861230421U);
                float totalSeconds = (float) this.sw.Elapsed.TotalSeconds;
                for (int i = 0; i < 64; ++i)
                {
                    float num2 = Math.Clamp(SliderCanvas.GetSliderValue(totalSeconds, (float) i / 64f, i), 0.0f, 1f);
                    foregroundDrawList.AddRectFilled(cursorScreenPos + new Vector2((float)(i * 4) + (float)i * radius, (float)(0.0 + (1.0 - (double)num2) * (double)contentRegionAvail2.Y)), cursorScreenPos + new Vector2((float)(i * 4) + (float)(i + 1) * radius, contentRegionAvail2.Y), 4293809408U);
                    foregroundDrawList.AddCircleFilled(cursorScreenPos + new Vector2((float)((double)radius / 2.0 + (double)(i * 4) + (double)i * (double)radius), (float)(0.0 + (1.0 - (double)num2) * (double)contentRegionAvail2.Y)), radius, 4293809408U);
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        public static float GetSliderValue(float t, float x, int i)
        {
            return (float)Doom.GetSliderValue(t, x, i);
        }
    }
}
