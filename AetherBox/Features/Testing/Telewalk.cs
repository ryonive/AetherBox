using System;
using System.Collections.Generic;
using System.Numerics;
using AetherBox.Features;
using AetherBox.Features.Debugging;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
namespace AetherBox.Features.Testing;
public class Telewalk : CommandFeature
{
	private bool active;

	private float displacementFactor = 0.1f;

	public override string Name => "Telewalk";

	public override string Command { get; set; } = "/telewalk";


	public override string[] Alias => new string[1] { "/tw" };

	public override string Description => "Replaces regular movement with teleporting. Works relative to your camera facing like normal movement.";

	public override List<string> Parameters => new List<string> { "<displacement factor>" };

	public override bool isDebug => true;

	public override FeatureType FeatureType => FeatureType.Commands;

	protected override void OnCommand(List<string> args)
	{
		float.TryParse(args[0], out displacementFactor);
		displacementFactor = ((displacementFactor == 0f) ? 0.1f : displacementFactor);
		if (!active)
		{
			active = true;
			Svc.Framework.Update += ModifyPOS;
			Svc.Log.Info("Enabling Telewalk");
		}
		else
		{
			active = false;
			Svc.Framework.Update -= ModifyPOS;
			Svc.Log.Info("Disabling Telewalk");
		}
	}

	private unsafe void ModifyPOS(IFramework framework)
	{
		if (!active)
		{
			return;
		}
		Structs.CameraEx* camera;
		camera = (Structs.CameraEx*)CameraManager.Instance()->GetActiveCamera();
		double xDisp;
		xDisp = 0.0 - Math.Sin(camera->DirH);
		double zDisp;
		zDisp = 0.0 - Math.Cos(camera->DirH);
		Math.Sin(camera->DirV);
		if (Svc.ClientState.LocalPlayer != null)
		{
			Vector3 curPos;
			curPos = Svc.ClientState.LocalPlayer.Position;
			if (Svc.KeyState[VirtualKey.W])
			{
				PositionDebug.SetPos(curPos + Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, 0f, (float)zDisp)));
			}
			if (Svc.KeyState[VirtualKey.A])
			{
				PositionDebug.SetPos(curPos + Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, 0f, (float)zDisp)));
			}
			if (Svc.KeyState[VirtualKey.S])
			{
				PositionDebug.SetPos(curPos + Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, 0f, (float)zDisp)));
			}
			if (Svc.KeyState[VirtualKey.D])
			{
				PositionDebug.SetPos(curPos + -Vector3.Multiply(displacementFactor, new Vector3(0f - (float)xDisp, 0f, 0f - (float)zDisp)));
			}
			if (Svc.KeyState[VirtualKey.SPACE] && !Svc.KeyState[VirtualKey.LSHIFT])
			{
				PositionDebug.SetPos(curPos + new Vector3(0f, displacementFactor, 0f));
			}
			if (Svc.KeyState[VirtualKey.SPACE] && Svc.KeyState[VirtualKey.LSHIFT])
			{
				PositionDebug.SetPos(curPos + new Vector3(0f, 0f - displacementFactor, 0f));
			}
		}
	}
}
