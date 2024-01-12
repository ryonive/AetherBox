using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using AetherBox;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using ECommons.Logging;

namespace AetherBox.Features;

public abstract class CommandFeature : Feature
{
	private readonly List<string> registeredCommands = new List<string>();

	public abstract string Command { get; set; }

	public virtual string[] Alias => Array.Empty<string>();

	public virtual string HelpMessage => $"[{AetherBox.Name} {Name}]";

	public virtual bool ShowInHelp => false;

	public virtual List<string> Parameters => new List<string>();

	public override FeatureType FeatureType => FeatureType.Commands;

	protected abstract void OnCommand(List<string> args);

	protected virtual void OnCommandInternal(string _, string args)
	{
		args = args.ToLower();
		OnCommand(args.Split(' ').ToList());
	}

	public override void Enable()
	{
		if (Svc.Commands.Commands.ContainsKey(Command))
		{
			Svc.Log.Error("Command '" + Command + "' is already registered.");
		}
		else
		{
			Svc.Commands.AddHandler(Command, new CommandInfo(OnCommandInternal)
			{
				HelpMessage = HelpMessage,
				ShowInHelp = ShowInHelp
			});
			registeredCommands.Add(Command);
		}
		string[] alias;
		alias = Alias;
		foreach (string a in alias)
		{
			if (!Svc.Commands.Commands.ContainsKey(a))
			{
				Svc.Commands.AddHandler(a, new CommandInfo(OnCommandInternal)
				{
					HelpMessage = HelpMessage,
					ShowInHelp = false
				});
				registeredCommands.Add(a);
			}
		}
	}

	public override void Disable()
	{
		foreach (string c in registeredCommands)
		{
			Svc.Commands.RemoveHandler(c);
		}
		registeredCommands.Clear();
		base.Disable();
	}

	public static List<string> GetArgumentList(string args)
	{
		return ArgumentRegex().Matches(args).Select(delegate(Match m)
		{
			if (m.Value.StartsWith('"') && m.Value.EndsWith('"'))
			{
				string value;
				value = m.Value;
				return value.Substring(1, value.Length - 1 - 1);
			}
			return m.Value;
		}).ToList();
	}

    private static Regex ArgumentRegex()
    {
        return new Regex("[\\\"].+?[\\\"]|[^ ]+");
    }
}
