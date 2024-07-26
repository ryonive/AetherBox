using System;
using System.Runtime.InteropServices;
using ClickLib.Bases;
using ClickLib.Enums;
using ClickLib.Structures;
using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using static ECommons.Automation.UIInput.ClickHelper;

namespace AetherBox.Helpers;

public static unsafe class ClickHelperExtensions
{
    public static void ClickAddonButton(this AtkComponentButton target, AtkComponentBase* addon, uint which, ECommons.Automation.UIInput.EventType type = ECommons.Automation.UIInput.EventType.CHANGE, ECommons.Automation.UIInput.EventData? eventData = null)
        => ClickHelper.ClickAddonComponent(addon, target.AtkComponentBase.OwnerNode, which, type, eventData);

    public static void ClickRadioButton(this AtkComponentRadioButton target, AtkComponentBase* addon, uint which, ECommons.Automation.UIInput.EventType type = ECommons.Automation.UIInput.EventType.CHANGE)
        => ClickHelper.ClickAddonComponent(addon, target.OwnerNode, which, type);

    public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon, AtkEvent* eventData)
    {
        ClickHelper.Listener.Invoke((nint)addon, eventData->Type, eventData->Param, eventData);
    }

    public static void ClickAddonButton(this AtkCollisionNode target, AtkUnitBase* addon, AtkEvent* eventData)
    {
        ClickHelper.Listener.Invoke((nint)addon, eventData->Type, eventData->Param, eventData);
    }

    public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon)
    {
        var btnRes = target.AtkComponentBase.OwnerNode->AtkResNode;
        var evt = btnRes.AtkEventManager.Event;

        addon->ReceiveEvent(evt->Type, (int)evt->Param, btnRes.AtkEventManager.Event);
    }

    public static void ClickAddonButton(this AtkCollisionNode target, AtkUnitBase* addon)
    {
        var btnRes = target.AtkResNode;
        var evt = btnRes.AtkEventManager.Event;

        while (evt->Type != AtkEventType.MouseClick)
            evt = evt->NextEvent;

        addon->ReceiveEvent(evt->Type, (int)evt->Param, btnRes.AtkEventManager.Event);
    }


    public static void ClickRadioButton(this AtkComponentRadioButton target, AtkUnitBase* addon)
    {
        var btnRes = target.OwnerNode->AtkResNode;
        var evt = btnRes.AtkEventManager.Event;

        Svc.Log.Debug($"{evt->Type} {evt->Param}");
        addon->ReceiveEvent(evt->Type, (int)evt->Param, btnRes.AtkEventManager.Event);
    }
}