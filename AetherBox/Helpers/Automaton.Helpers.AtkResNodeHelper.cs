using System;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherBox.Helpers;
using ClickLib.Enums;
using ClickLib.Structures;
using FFXIVClientStructs.FFXIV.Component.GUI;
namespace AetherBox.Helpers;
internal static class AtkResNodeHelper
{
	internal unsafe delegate nint ReceiveEventDelegate(AtkEventListener* eventListener, EventType evt, uint which, void* eventData, void* inputData);

	public unsafe static bool GetAtkUnitBase(this nint ptr, out AtkUnitBase* atkUnitBase)
	{
		if (ptr == IntPtr.Zero)
		{
			atkUnitBase = null;
			return false;
		}
		atkUnitBase = (AtkUnitBase*)ptr;
		return true;
	}

	public unsafe static Vector2 GetNodePosition(AtkResNode* node)
	{
		Vector2 pos;
		pos = new Vector2(node->X, node->Y);
		for (AtkResNode* par = node->ParentNode; par != null; par = par->ParentNode)
		{
			pos *= new Vector2(par->ScaleX, par->ScaleY);
			pos += new Vector2(par->X, par->Y);
		}
		return pos;
	}

	public unsafe static Vector2 GetNodeScale(AtkResNode* node)
	{
		if (node == null)
		{
			return new Vector2(1f, 1f);
		}
		Vector2 scale;
		scale = new Vector2(node->ScaleX, node->ScaleY);
		while (node->ParentNode != null)
		{
			node = node->ParentNode;
			scale *= new Vector2(node->ScaleX, node->ScaleY);
		}
		return scale;
	}

	public unsafe static void ClickAddonCheckBox(AtkUnitBase* window, AtkComponentCheckBox* target, uint which, EventType type = EventType.CHANGE)
	{
		ClickAddonComponent(window, target->AtkComponentButton.AtkComponentBase.OwnerNode, which, type);
	}

	public unsafe static void ClickAddonComponent(AtkUnitBase* UnitBase, AtkComponentNode* target, uint which, EventType type, EventData? eventData = null, InputData? inputData = null)
	{
		if (eventData == null)
		{
			eventData = EventData.ForNormalTarget(target, UnitBase);
		}
		if (inputData == null)
		{
			inputData = InputData.Empty();
		}
		InvokeReceiveEvent(&UnitBase->AtkEventListener, type, which, eventData, inputData);
	}

	private unsafe static void InvokeReceiveEvent(AtkEventListener* eventListener, EventType type, uint which, EventData eventData, InputData inputData)
	{
		GetReceiveEvent(eventListener)(eventListener, type, which, eventData.Data, inputData.Data);
	}

	private unsafe static ReceiveEventDelegate GetReceiveEvent(AtkEventListener* listener)
	{
		return Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(new IntPtr(listener->vfunc[2]));
	}

	private unsafe static ReceiveEventDelegate GetReceiveEvent(AtkComponentBase* listener)
	{
		return GetReceiveEvent(&listener->AtkEventListener);
	}

	private unsafe static ReceiveEventDelegate GetReceiveEvent(AtkUnitBase* listener)
	{
		return GetReceiveEvent(&listener->AtkEventListener);
	}
}
