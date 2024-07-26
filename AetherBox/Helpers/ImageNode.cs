using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Helpers;

public static class ImageNode
{
    private unsafe static IMemorySpace* UISpace => IMemorySpace.GetUISpace();

    public unsafe static AtkImageNode* MakeNode(uint nodeId, Vector2 textureCoordinates, Vector2 textureSize)
    {
        AtkImageNode* customNode;
        customNode = UISpace->Create<AtkImageNode>();
        customNode->AtkResNode.Type = NodeType.Image;
        customNode->AtkResNode.NodeId = nodeId;
        customNode->AtkResNode.NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;
        customNode->AtkResNode.DrawFlags = 0u;
        customNode->WrapMode = 1;
        customNode->Flags = 0;
        AtkUldPartsList* partsList;
        partsList = MakePartsList(0u, 1u);
        if (partsList == null)
        {
            FreeImageNode(customNode);
            return null;
        }
        AtkUldPart* part;
        part = MakePart(textureCoordinates, textureSize);
        if (part == null)
        {
            FreePartsList(partsList);
            FreeImageNode(customNode);
            return null;
        }
        partsList->Parts = part;
        AtkUldAsset* asset;
        asset = MakeAsset(0u);
        if (asset == null)
        {
            FreePart(part);
            FreePartsList(partsList);
            FreeImageNode(customNode);
            return null;
        }
        part->UldAsset = asset;
        customNode->PartsList = partsList;
        return customNode;
    }

    private unsafe static AtkUldPartsList* MakePartsList(uint id, uint partCount)
    {
        AtkUldPartsList* partsList;
        partsList = (AtkUldPartsList*)UISpace->Malloc((ulong)sizeof(AtkUldPartsList), 8uL);
        if (partsList != null)
        {
            partsList->Id = id;
            partsList->PartCount = partCount;
            return partsList;
        }
        return null;
    }

    private unsafe static AtkUldPart* MakePart(Vector2 textureCoordinates, Vector2 size)
    {
        AtkUldPart* part;
        part = (AtkUldPart*)UISpace->Malloc((ulong)sizeof(AtkUldPart), 8uL);
        if (part != null)
        {
            part->U = (ushort)textureCoordinates.X;
            part->V = (ushort)textureCoordinates.Y;
            part->Width = (ushort)size.X;
            part->Height = (ushort)size.Y;
            return part;
        }
        return null;
    }

    private unsafe static AtkUldAsset* MakeAsset(uint id)
    {
        AtkUldAsset* asset;
        asset = (AtkUldAsset*)UISpace->Malloc((ulong)sizeof(AtkUldAsset), 8uL);
        if (asset != null)
        {
            asset->Id = id;
            asset->AtkTexture.Ctor();
            return asset;
        }
        return null;
    }

    private unsafe static void FreePartsList(AtkUldPartsList* partsList)
    {
        if (partsList != null)
        {
            IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
        }
    }

    private unsafe static void FreePart(AtkUldPart* part)
    {
        if (part != null)
        {
            IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
        }
    }

    private unsafe static void FreeAsset(AtkUldAsset* asset)
    {
        if (asset != null)
        {
            asset->AtkTexture.Destroy(free: true);
            IMemorySpace.Free(asset, (ulong)sizeof(AtkUldAsset));
        }
    }

    public unsafe static void FreeImageNode(AtkImageNode* imageNode)
    {
        if (imageNode == null)
        {
            return;
        }
        AtkUldPartsList* partsList;
        partsList = imageNode->PartsList;
        if (partsList != null)
        {
            AtkUldPart* part;
            part = imageNode->PartsList->Parts;
            if (part != null)
            {
                AtkUldAsset* asset;
                asset = imageNode->PartsList->Parts->UldAsset;
                if (asset != null)
                {
                    FreeAsset(asset);
                }
                FreePart(part);
            }
            FreePartsList(partsList);
        }
        imageNode->AtkResNode.Destroy(free: true);
        IMemorySpace.Free(imageNode, (ulong)sizeof(AtkImageNode));
    }

    public unsafe static void LinkNode(AtkComponentNode* rootNode, AtkResNode* beforeNode, AtkImageNode* newNode)
    {
        AtkResNode* prev;
        prev = beforeNode->PrevSiblingNode;
        newNode->AtkResNode.ParentNode = beforeNode->ParentNode;
        beforeNode->PrevSiblingNode = (AtkResNode*)newNode;
        prev->NextSiblingNode = (AtkResNode*)newNode;
        newNode->AtkResNode.PrevSiblingNode = prev;
        newNode->AtkResNode.NextSiblingNode = beforeNode;
        rootNode->Component->UldManager.UpdateDrawNodeList();
    }
}
