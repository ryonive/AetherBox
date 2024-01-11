using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AetherBox.Helpers;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Helpers;

public static class UiHelper
{
    public record PartInfo(ushort U, ushort V, ushort Width, ushort Height);

    public unsafe static AtkImageNode* MakeImageNode(uint id, PartInfo partInfo)
    {
        if (!TryMakeImageNode(id, ~(NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorBottom | NodeFlags.AnchorRight | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Clip | NodeFlags.Fill | NodeFlags.HasCollision | NodeFlags.RespondToMouse | NodeFlags.Focusable | NodeFlags.Droppable | NodeFlags.IsTopNode | NodeFlags.EmitsEvents | NodeFlags.UseDepthBasedPriority | NodeFlags.UnkFlag2), 0u, 0, 0, out var imageNode))
        {
            Svc.Log.Error("Failed to alloc memory for AtkImageNode.");
            return null;
        }
        if (!TryMakePartsList(0u, out var partsList))
        {
            Svc.Log.Error("Failed to alloc memory for AtkUldPartsList.");
            FreeImageNode(imageNode);
            return null;
        }
        if (!TryMakePart(partInfo.U, partInfo.V, partInfo.Width, partInfo.Height, out var part))
        {
            Svc.Log.Error("Failed to alloc memory for AtkUldPart.");
            FreePartsList(partsList);
            FreeImageNode(imageNode);
            return null;
        }
        if (!TryMakeAsset(0u, out var asset))
        {
            Svc.Log.Error("Failed to alloc memory for AtkUldAsset.");
            FreePart(part);
            FreePartsList(partsList);
            FreeImageNode(imageNode);
        }
        AddAsset(part, asset);
        AddPart(partsList, part);
        AddPartsList(imageNode, partsList);
        return imageNode;
    }

    public unsafe static bool IsAddonReady(AtkUnitBase* addon)
    {
        if (addon == null)
        {
            return false;
        }
        if (addon->RootNode == null)
        {
            return false;
        }
        if (addon->RootNode->ChildNode == null)
        {
            return false;
        }
        return true;
    }

    public unsafe static AtkTextNode* MakeTextNode(uint id)
    {
        if (!TryMakeTextNode(id, out var textNode))
        {
            return null;
        }
        return textNode;
    }

    public unsafe static void LinkNodeAtEnd(AtkResNode* imageNode, AtkUnitBase* parent)
    {
        AtkResNode* node;
        node = parent->RootNode->ChildNode;
        while (node->PrevSiblingNode != null)
        {
            node = node->PrevSiblingNode;
        }
        node->PrevSiblingNode = imageNode;
        imageNode->NextSiblingNode = node;
        imageNode->ParentNode = node->ParentNode;
        parent->UldManager.UpdateDrawNodeList();
    }

    public unsafe static void LinkNodeAtEnd<T>(T* atkNode, AtkResNode* parentNode, AtkUnitBase* addon) where T : unmanaged
    {
        AtkResNode* endNode;
        endNode = parentNode->ChildNode;
        if (endNode == null)
        {
            parentNode->ChildNode = (AtkResNode*)atkNode;
            ((AtkResNode*)atkNode)->ParentNode = parentNode;
            ((AtkResNode*)atkNode)->PrevSiblingNode = null;
            ((AtkResNode*)atkNode)->NextSiblingNode = null;
        }
        else
        {
            while (endNode->PrevSiblingNode != null)
            {
                endNode = endNode->PrevSiblingNode;
            }
            ((AtkResNode*)atkNode)->ParentNode = parentNode;
            ((AtkResNode*)atkNode)->NextSiblingNode = endNode;
            ((AtkResNode*)atkNode)->PrevSiblingNode = null;
            endNode->PrevSiblingNode = (AtkResNode*)atkNode;
        }
        addon->UldManager.UpdateDrawNodeList();
    }

    public unsafe static void LinkNodeAfterTargetNode(AtkResNode* node, AtkComponentNode* parent, AtkResNode* targetNode)
    {
        AtkResNode* prev;
        prev = targetNode->PrevSiblingNode;
        node->ParentNode = targetNode->ParentNode;
        targetNode->PrevSiblingNode = node;
        prev->NextSiblingNode = node;
        node->PrevSiblingNode = prev;
        node->NextSiblingNode = targetNode;
        parent->Component->UldManager.UpdateDrawNodeList();
    }

    public unsafe static void LinkNodeAfterTargetNode<T>(T* atkNode, AtkUnitBase* parent, AtkResNode* targetNode) where T : unmanaged
    {
        AtkResNode* prev;
        prev = targetNode->PrevSiblingNode;
        ((AtkResNode*)atkNode)->ParentNode = targetNode->ParentNode;
        targetNode->PrevSiblingNode = (AtkResNode*)atkNode;
        prev->NextSiblingNode = (AtkResNode*)atkNode;
        ((AtkResNode*)atkNode)->PrevSiblingNode = prev;
        ((AtkResNode*)atkNode)->NextSiblingNode = targetNode;
        parent->UldManager.UpdateDrawNodeList();
    }

    public unsafe static void UnlinkNode<T>(T* atkNode, AtkComponentNode* componentNode) where T : unmanaged
    {
        if (atkNode != null)
        {
            if (((AtkResNode*)atkNode)->ParentNode->ChildNode == atkNode)
            {
                ((AtkResNode*)atkNode)->ParentNode->ChildNode = ((AtkResNode*)atkNode)->NextSiblingNode;
            }
            if (((AtkResNode*)atkNode)->NextSiblingNode != null && ((AtkResNode*)atkNode)->NextSiblingNode->PrevSiblingNode == atkNode)
            {
                ((AtkResNode*)atkNode)->NextSiblingNode->PrevSiblingNode = ((AtkResNode*)atkNode)->PrevSiblingNode;
            }
            if (((AtkResNode*)atkNode)->PrevSiblingNode != null && ((AtkResNode*)atkNode)->PrevSiblingNode->NextSiblingNode == atkNode)
            {
                ((AtkResNode*)atkNode)->PrevSiblingNode->NextSiblingNode = ((AtkResNode*)atkNode)->NextSiblingNode;
            }
            componentNode->Component->UldManager.UpdateDrawNodeList();
        }
    }

    public unsafe static void UnlinkNode<T>(T* atkNode, AtkUnitBase* unitBase) where T : unmanaged
    {
        if (atkNode != null)
        {
            if (((AtkResNode*)atkNode)->ParentNode->ChildNode == atkNode)
            {
                ((AtkResNode*)atkNode)->ParentNode->ChildNode = ((AtkResNode*)atkNode)->NextSiblingNode;
            }
            if (((AtkResNode*)atkNode)->NextSiblingNode != null && ((AtkResNode*)atkNode)->NextSiblingNode->PrevSiblingNode == atkNode)
            {
                ((AtkResNode*)atkNode)->NextSiblingNode->PrevSiblingNode = ((AtkResNode*)atkNode)->PrevSiblingNode;
            }
            if (((AtkResNode*)atkNode)->PrevSiblingNode != null && ((AtkResNode*)atkNode)->PrevSiblingNode->NextSiblingNode == atkNode)
            {
                ((AtkResNode*)atkNode)->PrevSiblingNode->NextSiblingNode = ((AtkResNode*)atkNode)->NextSiblingNode;
            }
            unitBase->UldManager.UpdateDrawNodeList();
        }
    }

    public unsafe static void UnlinkAndFreeImageNode(AtkImageNode* node, AtkUnitBase* parent)
    {
        if (node->AtkResNode.PrevSiblingNode != null)
        {
            node->AtkResNode.PrevSiblingNode->NextSiblingNode = node->AtkResNode.NextSiblingNode;
        }
        if (node->AtkResNode.NextSiblingNode != null)
        {
            node->AtkResNode.NextSiblingNode->PrevSiblingNode = node->AtkResNode.PrevSiblingNode;
        }
        parent->UldManager.UpdateDrawNodeList();
        FreePartsList(node->PartsList);
        FreeImageNode(node);
    }

    public unsafe static void UnlinkAndFreeTextNode(AtkTextNode* node, AtkUnitBase* parent)
    {
        if (node->AtkResNode.PrevSiblingNode != null)
        {
            node->AtkResNode.PrevSiblingNode->NextSiblingNode = node->AtkResNode.NextSiblingNode;
        }
        if (node->AtkResNode.NextSiblingNode != null)
        {
            node->AtkResNode.NextSiblingNode->PrevSiblingNode = node->AtkResNode.PrevSiblingNode;
        }
        parent->UldManager.UpdateDrawNodeList();
        FreeTextNode(node);
    }

    public unsafe static bool TryMakeTextNode(uint id, [NotNullWhen(true)] out AtkTextNode* textNode)
    {
        textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
        if (textNode != null)
        {
            textNode->AtkResNode.Type = NodeType.Text;
            textNode->AtkResNode.NodeID = id;
            return true;
        }
        return false;
    }

    public unsafe static bool TryMakeImageNode(uint id, NodeFlags resNodeFlags, uint resNodeDrawFlags, byte wrapMode, byte imageNodeFlags, [NotNullWhen(true)] out AtkImageNode* imageNode)
    {
        imageNode = IMemorySpace.GetUISpace()->Create<AtkImageNode>();
        if (imageNode != null)
        {
            imageNode->AtkResNode.Type = NodeType.Image;
            imageNode->AtkResNode.NodeID = id;
            imageNode->AtkResNode.NodeFlags = resNodeFlags;
            imageNode->AtkResNode.DrawFlags = resNodeDrawFlags;
            imageNode->WrapMode = wrapMode;
            imageNode->Flags = imageNodeFlags;
            return true;
        }
        return false;
    }

    public unsafe static bool TryMakePartsList(uint id, [NotNullWhen(true)] out AtkUldPartsList* partsList)
    {
        partsList = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPartsList), 8uL);
        if (partsList != null)
        {
            partsList->Id = id;
            partsList->PartCount = 0u;
            partsList->Parts = null;
            return true;
        }
        return false;
    }

    public unsafe static bool TryMakePart(ushort u, ushort v, ushort width, ushort height, [NotNullWhen(true)] out AtkUldPart* part)
    {
        part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart), 8uL);
        if (part != null)
        {
            part->U = u;
            part->V = v;
            part->Width = width;
            part->Height = height;
            return true;
        }
        return false;
    }

    public unsafe static bool TryMakeAsset(uint id, [NotNullWhen(true)] out AtkUldAsset* asset)
    {
        asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8uL);
        if (asset != null)
        {
            asset->Id = id;
            asset->AtkTexture.Ctor();
            return true;
        }
        return false;
    }

    public unsafe static void AddPartsList(AtkImageNode* imageNode, AtkUldPartsList* partsList)
    {
        imageNode->PartsList = partsList;
    }

    public unsafe static void AddPartsList(AtkCounterNode* counterNode, AtkUldPartsList* partsList)
    {
        counterNode->PartsList = partsList;
    }

    public unsafe static void AddPart(AtkUldPartsList* partsList, AtkUldPart* part)
    {
        AtkUldPart* oldPartArray;
        oldPartArray = partsList->Parts;
        uint newSize;
        newSize = partsList->PartCount + 1;
        AtkUldPart* newArray;
        newArray = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)(sizeof(AtkUldPart) * newSize), 8uL);
        if (oldPartArray != null)
        {
            foreach (int index in Enumerable.Range(0, (int)partsList->PartCount))
            {
                Buffer.MemoryCopy(oldPartArray + index, newArray + index, sizeof(AtkUldPart), sizeof(AtkUldPart));
            }
            IMemorySpace.Free(oldPartArray, (ulong)(sizeof(AtkUldPart) * partsList->PartCount));
        }
        Buffer.MemoryCopy(part, newArray + (newSize - 1), sizeof(AtkUldPart), sizeof(AtkUldPart));
        partsList->Parts = newArray;
        partsList->PartCount = newSize;
    }

    public unsafe static void AddAsset(AtkUldPart* part, AtkUldAsset* asset)
    {
        part->UldAsset = asset;
    }

    public unsafe static void FreeImageNode(AtkImageNode* node)
    {
        node->AtkResNode.Destroy(free: false);
        IMemorySpace.Free(node, (ulong)sizeof(AtkImageNode));
    }

    public unsafe static void FreeTextNode(AtkTextNode* node)
    {
        node->AtkResNode.Destroy(free: false);
        IMemorySpace.Free(node, (ulong)sizeof(AtkTextNode));
    }

    public unsafe static void FreePartsList(AtkUldPartsList* partsList)
    {
        foreach (int index in Enumerable.Range(0, (int)partsList->PartCount))
        {
            AtkUldPart* num;
            num = partsList->Parts + index;
            FreeAsset(num->UldAsset);
            FreePart(num);
        }
        IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
    }

    public unsafe static void FreePart(AtkUldPart* part)
    {
        IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
    }

    public unsafe static void FreeAsset(AtkUldAsset* asset)
    {
        IMemorySpace.Free(asset, (ulong)sizeof(AtkUldAsset));
    }
}
