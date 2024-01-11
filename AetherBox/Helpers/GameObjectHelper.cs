using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;

namespace AetherBox.Helpers;

internal static class GameObjectHelper
{
	public static float GetTargetDistance(GameObject target)
	{
		if ((object)target == null || (object)Svc.ClientState.LocalPlayer == null)
		{
			return 0f;
		}
		if (target.ObjectId == Svc.ClientState.LocalPlayer.ObjectId)
		{
			return 0f;
		}
		Vector2 position;
		position = new Vector2(target.Position.X, target.Position.Z);
		Vector2 selfPosition;
		selfPosition = new Vector2(Svc.ClientState.LocalPlayer.Position.X, Svc.ClientState.LocalPlayer.Position.Z);
		return Math.Max(0f, Vector2.Distance(position, selfPosition) - target.HitboxRadius - Svc.ClientState.LocalPlayer.HitboxRadius);
	}

	public static float GetHeightDifference(GameObject target)
	{
		float dist;
		dist = Svc.ClientState.LocalPlayer.Position.Y - target.Position.Y;
		if (dist < 0f)
		{
			dist *= -1f;
		}
		return dist;
	}
}
