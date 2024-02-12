using System;
using System.Numerics;
using System.Xml.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using System.Windows.Forms;
using SocketIOClient.Messages;
using Dalamud.Game.ClientState.Objects;
using static AetherBox.Helpers.BossMod.ActorCastEvent;

namespace AetherBox.Helpers;

internal static class GameObjectHelper
{
    private static string Name = "GameObjectHelper";

    internal unsafe static void UpdateGameObjects(IFramework framework)
    {
        Players = Svc.Objects.GetObjectInRadius(50);
        BattlePlayers = Players.OfType<BattleChara>().Where(chara => chara.SubKind == 1);
    }

    public static IEnumerable<GameObject> Players { get; set; }
    public static IEnumerable<BattleChara> BattlePlayers { get; set; }

    public static void SetTarget(GameObject obj)
    {
        Svc.Targets.Target = obj;
    }

    public static IEnumerable<T> GetObjectInRadius<T>(this IEnumerable<T> objects, float radius) where T : GameObject
        => objects.Where(o => o.DistanceToPlayer() <= radius);

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

    /// <summary>
    /// The distance from <paramref name="obj"/> to the player 
    /// NOTE: Takes the center point of the player
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static float DistanceToPlayerCenter(this GameObject obj)
    {
        if (obj == null) return float.MaxValue;
        var player = Player.Object;
        if (player == null) return float.MaxValue;

        var distance = Vector3.Distance(player.Position, obj.Position);
        distance -= obj.HitboxRadius;
        return distance;
    }

    /// <summary>
    /// The distance from <paramref name="obj"/> to the player (Taking the hitbox radius (circle around the player) into account)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static float DistanceToPlayer(this GameObject obj)
    {
        if (obj == null) return float.MaxValue;
        var player = Player.Object;
        if (player == null) return float.MaxValue;

        var distance = Vector3.Distance(player.Position, obj.Position) - player.HitboxRadius;
        distance -= obj.HitboxRadius;
        return distance;
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

    private static void Enable()
    {
        Svc.Framework.Update += UpdateGameObjects;
        Svc.Log.Information("Enabled: " + Name);
    }

    private static void Disable()
    {
        Svc.Framework.Update -= UpdateGameObjects;
        Svc.Log.Information("Disabled: " + Name);
    }

    public static void Init()
    {
        Enable();
    }

    public static void Dispose()
    {
        Disable();
    }

}
