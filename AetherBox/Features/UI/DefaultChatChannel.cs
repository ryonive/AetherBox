// AetherBox, Version=69.3.0.0, Culture=neutral, PublicKeyToken=null
// AetherBox.Features.UI.DefaultChatChannel
using System;
using System.Collections.Generic;
using AetherBox.Features;
using AetherBox.Features.UI;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
namespace AetherBox.Features.UI;
public class DefaultChatChannel : Feature
{
	public class Configs : FeatureConfig
	{
		public int SelectedChannel = 3;

		public bool OnLogin = true;

		public bool OnZoneChange;
	}

	public override string Name => "Default Chat Channel";

	public override string Description => "Sets the default chat channel.";

	public override FeatureType FeatureType => FeatureType.Disabled;

	public Configs Config { get; private set; }

	protected unsafe override DrawConfigDelegate DrawConfigTree => delegate(ref bool hasChanged)
	{
		try
		{
			if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ChatLog", out var AddonPtr))
			{
				List<string> list;
				list = new List<string>();
				for (int i = 0; i < AddonPtr->AtkValues[6].Int; i++)
				{
					list.Add(TextHelper.AtkValueStringToString(AddonPtr->AtkValues[8 + i].String));
				}
				using ImRaii.IEndObject endObject = ImRaii.Combo("channels", list[Config.SelectedChannel]);
				if (endObject)
				{
					foreach (string current in list)
					{
						if (ImGui.Selectable(current, list.IndexOf(current) == Config.SelectedChannel))
						{
							Config.SelectedChannel = list.IndexOf(current);
							hasChanged = true;
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
		if (ImGui.Checkbox("On Login", ref Config.OnLogin))
		{
			hasChanged = true;
		}
		if (ImGui.Checkbox("On Zone Change", ref Config.OnZoneChange))
		{
			hasChanged = true;
		}
	};

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		Svc.ClientState.Login += OnLogin;
		Svc.ClientState.TerritoryChanged += OnZoneChange;
		base.Enable();
	}

	public override void Disable()
	{
		SaveConfig(Config);
		Svc.ClientState.Login -= OnLogin;
		Svc.ClientState.TerritoryChanged -= OnZoneChange;
		base.Disable();
	}

	private unsafe void OnLogin()
	{
		if (Config.OnLogin && GenericHelpers.TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon))
		{
			Callback.Fire(addon, false, 4, Config.SelectedChannel, Config.SelectedChannel, 0);
		}
	}

	private unsafe void OnZoneChange(ushort obj)
	{
		if (Config.OnZoneChange && GenericHelpers.TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon))
		{
			Callback.Fire(addon, false, 4, Config.SelectedChannel, Config.SelectedChannel, 0);
		}
	}
}
