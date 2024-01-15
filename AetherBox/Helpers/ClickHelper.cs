using System;
using System.Runtime.InteropServices;
using ClickLib.Bases;
using ClickLib.Enums;
using ClickLib.Structures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Helpers;

internal static class ClickHelper
{
    private unsafe static ReceiveEventDelegate GetReceiveEvent(AtkEventListener* listener)
    {
        return Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(new IntPtr(listener->vfunc[2]));
    }

    private unsafe static void InvokeReceiveEvent(AtkEventListener* eventListener, EventType type, uint which, EventData eventData, InputData inputData)
    {
        GetReceiveEvent(eventListener)(eventListener, type, which, eventData.Data, inputData.Data);
    }

    private unsafe static void ClickAddonComponent(AtkComponentBase* unitbase, AtkComponentNode* target, uint which, EventType type, EventData? eventData = null, InputData? inputData = null)
    {
        if (eventData == null)
        {
            eventData = EventData.ForNormalTarget(target, unitbase);
        }
        if (inputData == null)
        {
            inputData = InputData.Empty();
        }
        InvokeReceiveEvent(&unitbase->AtkEventListener, type, which, eventData, inputData);
    }

    public unsafe static void ClickAddonButton(this AtkComponentButton target, AtkComponentBase* addon, uint which, EventType type = EventType.CHANGE)
    {
        ClickAddonComponent(addon, target.AtkComponentBase.OwnerNode, which, type);
    }

    public unsafe static void ClickRadioButton(this AtkComponentRadioButton target, AtkComponentBase* addon, uint which, EventType type = EventType.CHANGE)
    {
        ClickAddonComponent(addon, target.AtkComponentBase.OwnerNode, which, type);
    }
}
