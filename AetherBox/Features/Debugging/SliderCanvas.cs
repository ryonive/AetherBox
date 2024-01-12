using System;
using System.Diagnostics;
using System.Numerics;
using AetherBox.Debugging;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class SliderCanvas : DebugHelper
{
	private readonly Stopwatch sw = new Stopwatch();

	private const int BarSpacing = 4;

	private const int BarCount = 64;

	public override string Name => "SliderCanvas".Replace("Debug", "") + " Debugging";

	public override void Draw()
	{
		if (!sw.IsRunning)
		{
			sw.Restart();
		}
		Vector2 tSpace;
		tSpace = ImGui.GetContentRegionAvail();
		float size;
		size = ((tSpace.X > tSpace.Y) ? tSpace.Y : tSpace.X);
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (tSpace.X / 2f - size / 2f));
		ImGui.PushStyleColor(ImGuiCol.WindowBg, uint.MaxValue);
		if (ImGui.BeginChild("sliderLand", new Vector2(size) * 0.8f, border: true))
		{
			ImDrawListPtr dl;
			dl = ImGui.GetForegroundDrawList();
			Vector2 space;
			space = ImGui.GetContentRegionAvail();
			float barSize;
			barSize = space.X / 64f - 4f;
			Vector2 p0;
			p0 = ImGui.GetCursorScreenPos();
			dl.AddRectFilled(p0 - new Vector2(50f), p0 + space + new Vector2(100f), uint.MaxValue);
			for (int j = 0; j < 64; j++)
			{
				dl.AddRectFilled(p0 + new Vector2((float)(j * 4) + (float)j * barSize, 0f), p0 + new Vector2((float)(j * 4) + (float)(j + 1) * barSize, space.Y), 861230421u);
			}
			float t;
			t = (float)sw.Elapsed.TotalSeconds;
			for (int i = 0; i < 64; i++)
			{
				float v;
				v = Math.Clamp(GetSliderValue(t, (float)i / 64f, i), 0f, 1f);
				dl.AddRectFilled(p0 + new Vector2((float)(i * 4) + (float)i * barSize, 0f + (1f - v) * space.Y), p0 + new Vector2((float)(i * 4) + (float)(i + 1) * barSize, space.Y), 4293809408u);
				dl.AddCircleFilled(p0 + new Vector2(barSize / 2f + (float)(i * 4) + (float)i * barSize, 0f + (1f - v) * space.Y), barSize, 4293809408u);
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
