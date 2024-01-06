using AetherBox.FeaturesSetup;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using ECommons.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

#nullable disable
namespace AetherBox.Features;

public abstract class CommandFeature : Feature
{
    private readonly List<string> registeredCommands = new List<string>();

    public abstract string Command { get; set; }

    public virtual string[] Alias => Array.Empty<string>();

    public virtual string HelpMessage
    {
        get
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
            interpolatedStringHandler.AppendLiteral("[");
            interpolatedStringHandler.AppendFormatted(AetherBox.Name);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted(this.Name);
            interpolatedStringHandler.AppendLiteral("]");
            return interpolatedStringHandler.ToStringAndClear();
        }
    }

    public virtual bool ShowInHelp => false;

    public virtual List<string> Parameters => new List<string>();

    public override FeatureType FeatureType => FeatureType.Commands;

    protected abstract void OnCommand(List<string> args);

    protected virtual void OnCommandInternal(string _, string args)
    {
        args = args.ToLower();
        this.OnCommand(((IEnumerable<string>)args.Split(' ')).ToList<string>());
    }

    public override void Enable()
    {
        if (Svc.Commands.Commands.ContainsKey(this.Command))
        {
            PluginLog.Error("Command '" + this.Command + "' is already registered.");
        }
        else
        {
            Svc.Commands.AddHandler(this.Command, new CommandInfo(new CommandInfo.HandlerDelegate(this.OnCommandInternal))
            {
                HelpMessage = this.HelpMessage,
                ShowInHelp = this.ShowInHelp
            });
            this.registeredCommands.Add(this.Command);
        }
        foreach (string alia in this.Alias)
        {
            if (!Svc.Commands.Commands.ContainsKey(alia))
            {
                Svc.Commands.AddHandler(alia, new CommandInfo(new CommandInfo.HandlerDelegate(this.OnCommandInternal))
                {
                    HelpMessage = this.HelpMessage,
                    ShowInHelp = false
                });
                this.registeredCommands.Add(alia);
            }
        }
    }

    public override void Disable()
    {
        foreach (string registeredCommand in this.registeredCommands)
            Svc.Commands.RemoveHandler(registeredCommand);
        this.registeredCommands.Clear();
        base.Disable();
    }

    public static List<string> GetArgumentList(string args)
    {
        return ArgumentRegex().Matches(args).Select<Match, string>(m =>
        {
            if (!m.Value.StartsWith('"') || !m.Value.EndsWith('"'))
                return m.Value;
            string str = m.Value;
            return str.Substring(1, str.Length - 2); // Fixed substring indices.
        }).ToList<string>();
    }

    private static Regex ArgumentRegex()
    {
        return new Regex("[\\\"].+?[\\\"]|[^ ]+");
    }
}


