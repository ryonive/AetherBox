using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AetherBox.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class HelpMessageAttribute : Attribute
{
    public string HelpMessage { get; }

    public HelpMessageAttribute(string helpMessage)
    {
        HelpMessage = helpMessage;
    }
}
