using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherBox.Features;
using AetherBox.Features.Commands;
using AetherBox.FeaturesSetup;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Features.Commands;

public class NoDozeSnap : CommandFeature
{
    private unsafe delegate byte ShouldSnap(Character* a1, SnapPosition* a2);

    [StructLayout(LayoutKind.Explicit, Size = 56)]
    public struct SnapPosition
    {
        [FieldOffset(0)]
        public Vector3 PositionA;

        [FieldOffset(16)]
        public float RotationA;

        [FieldOffset(32)]
        public Vector3 PositionB;

        [FieldOffset(48)]
        public float RotationB;
    }

    [Signature("E8 ?? ?? ?? ?? 4C 8B 74 24 ?? 48 8B CE E8")]
    private unsafe readonly delegate* unmanaged<nint, ushort, nint, byte, byte, void> useEmote = null;

    public override string Name => "No Doze Snap";

    public override string Command { get; set; } = "/dozehere";


    public override string Description => "Dozers without Borders";

    public override FeatureType FeatureType => FeatureType.Disabled;

    [Signature("E8 ?? ?? ?? ?? 84 C0 74 44 4C 8D 6D C7", DetourName = "ShouldSnapDetour")]
    private Hook<ShouldSnap>? ShouldSnapHook { get; init; }

    private unsafe static byte ShouldSnapDetour(Character* a1, SnapPosition* a2)
    {
        return 0;
    }

    public override void Enable()
    {
        ShouldSnapHook?.Enable();
        base.Enable();
    }

    public override void Dispose()
    {
        ShouldSnapHook?.Dispose();
        base.Dispose();
    }

    protected unsafe override void OnCommand(List<string> args)
    {
        AgentInterface* agent;
        agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Emote);
        useEmote(new IntPtr(agent), 88, IntPtr.Zero, 0, 0);
    }
}
