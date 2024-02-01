using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Helpers;

internal static class AtkResNodeHelper
{
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
}
