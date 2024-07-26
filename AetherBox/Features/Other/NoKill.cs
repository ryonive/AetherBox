using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AetherBox.Features;
using AetherBox.Features.Other;
using AetherBox.FeaturesSetup;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Features.Other;

public class NoKill : Feature, IDisposable
{
	public class Configs : FeatureConfig
	{
		[FeatureConfigOption("Skip Authentication Errors")]
		public bool SkipAuthError = true;

		[FeatureConfigOption("Queue Mode: Use for lobby errors in queues")]
		public bool QueueMode;

		[FeatureConfigOption("Safer Mode: Filters invalid messages that may crash the client")]
		public bool SaferMode;

		[FeatureConfigOption("Try to Close Error Automatically")]
		public bool CloseAutomatically;

		[FeatureConfigOption("Try to Login After")]
		public bool AttemptLogin = true;
	}

	private delegate long StartHandlerDelegate(long a1, long a2);

	private delegate long LoginHandlerDelegate(long a1, long a2);

	private delegate char LobbyErrorHandlerDelegate(long a1, long a2, long a3);

	private delegate void DecodeSeStringHandlerDelegate(long a1, long a2, long a3, long a4);

	internal nint StartHandler;

	internal nint LoginHandler;

	internal nint LobbyErrorHandler;

	private Hook<StartHandlerDelegate> startHandlerHook;

	private Hook<LoginHandlerDelegate> loginHandlerHook;

	private Hook<LobbyErrorHandlerDelegate> lobbyErrorHandlerHook;

	public override string Name => "Prevent Lobby Error Crashes";

	public override string Description => "Prevents the game from closing itself when it gets a lobby error";

	public override FeatureType FeatureType => FeatureType.Other;

	public Configs Config { get; private set; }

	public override bool UseAutoConfig => true;

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		if (lobbyErrorHandlerHook == null)
		{
			lobbyErrorHandlerHook = Svc.Hook.HookFromSignature<LobbyErrorHandlerDelegate>("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0", LobbyErrorHandlerDetour);
		}
		try
		{
			StartHandler = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B2 01 49 8B CC");
		}
		catch (Exception)
		{
			StartHandler = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B2 01 49 8B CD");
		}
		LoginHandler = Svc.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 40 32 FF");
		lobbyErrorHandlerHook.Enable();
		if (Config.AttemptLogin)
		{
			startHandlerHook = Svc.Hook.HookFromAddress<StartHandlerDelegate>(StartHandler, StartHandlerDetour);
			loginHandlerHook = Svc.Hook.HookFromAddress<LoginHandlerDelegate>(LoginHandler, LoginHandlerDetour);
			startHandlerHook.Enable();
			loginHandlerHook.Enable();
		}
		Svc.Framework.Update += CheckDialogue;
		base.Enable();
	}

	public override void Disable()
	{
		SaveConfig(Config);
		lobbyErrorHandlerHook?.Disable();
        lobbyErrorHandlerHook?.Dispose();
        if (Config.AttemptLogin)
		{
			startHandlerHook?.Disable();
            startHandlerHook?.Dispose();
            loginHandlerHook?.Disable();
            loginHandlerHook?.Dispose();
        }
		Svc.Framework.Update -= CheckDialogue;
		base.Disable();
	}

    private long StartHandlerDetour(long a1, long a2)
	{
		Marshal.ReadInt16(new IntPtr(a1 + 88));
		int a1_456;
		a1_456 = Marshal.ReadInt32(new IntPtr(a1 + 456));
		Svc.Log.Debug($"Start a1_456:{a1_456}");
		if (a1_456 != 0 && Config.QueueMode)
		{
			Marshal.WriteInt32(new IntPtr(a1 + 456), 0);
			Svc.Log.Debug($"a1_456: {a1_456} => 0");
		}
		return startHandlerHook.Original(a1, a2);
	}

	private long LoginHandlerDetour(long a1, long a2)
	{
		byte a1_2165;
		a1_2165 = Marshal.ReadByte(new IntPtr(a1 + 2165));
		Svc.Log.Debug($"Login a1_2165:{a1_2165}");
		if (a1_2165 != 0 && Config.QueueMode)
		{
			Marshal.WriteByte(new IntPtr(a1 + 2165), 0);
			Svc.Log.Debug($"a1_2165: {a1_2165} => 0");
		}
		return loginHandlerHook.Original(a1, a2);
	}

	private char LobbyErrorHandlerDetour(long a1, long a2, long a3)
	{
		nint p3;
		p3 = new IntPtr(a3);
		byte t1;
		t1 = Marshal.ReadByte(p3);
		int num;
		num = (((t1 & 0xF) > 0) ? Marshal.ReadInt32(p3 + 8) : 0);
		ushort v4_16;
		v4_16 = (ushort)num;
		Svc.Log.Debug($"LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
		if (num != 0)
		{
			if (v4_16 == 13100 && Config.SkipAuthError)
			{
				Svc.Log.Debug("Skip Auth Error");
			}
			else
			{
				Marshal.WriteInt64(p3 + 8, 16000L);
				v4_16 = (ushort)(((t1 & 0xF) > 0) ? ((uint)Marshal.ReadInt32(p3 + 8)) : 0u);
			}
		}
		Svc.Log.Debug($"After LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
		return lobbyErrorHandlerHook.Original(a1, a2, a3);
	}

	private unsafe void CheckDialogue(IFramework framework)
	{
		if (Config.CloseAutomatically && Svc.GameGui.GetAddonByName("Dialogue") != IntPtr.Zero && !Svc.Condition.Any())
		{
			AtkUnitBase* addon;
			addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Dialogue");
			if (addon->IsVisible())
			{
				WindowsKeypress.SendKeypress(Keys.NumPad0);
			}
		}
	}
}
