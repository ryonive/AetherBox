using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Helpers;

public static class Node
{
	public unsafe static T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId) where T : unmanaged
	{
		foreach (int index in Enumerable.Range(0, uldManager.NodeListCount))
		{
			AtkResNode* currentNode;
			currentNode = uldManager.NodeList[index];
			if (currentNode->NodeID == nodeId)
			{
				return (T*)currentNode;
			}
		}
		return null;
	}

	public unsafe static void LinkNodeAtEnd(AtkResNode* resNode, AtkUnitBase* parent)
	{
		AtkResNode* node;
		node = parent->RootNode->ChildNode;
		while (node->PrevSiblingNode != null)
		{
			node = node->PrevSiblingNode;
		}
		node->PrevSiblingNode = resNode;
		resNode->NextSiblingNode = node;
		resNode->ParentNode = node->ParentNode;
		ushort* childCount;
		childCount = &node->ChildCount;
		(*childCount)++;
		parent->UldManager.UpdateDrawNodeList();
	}

	public unsafe static void UnlinkNodeAtEnd(AtkResNode* resNode, AtkUnitBase* parent)
	{
		if (resNode->PrevSiblingNode != null)
		{
			resNode->PrevSiblingNode->NextSiblingNode = resNode->NextSiblingNode;
		}
		if (resNode->NextSiblingNode != null)
		{
			resNode->NextSiblingNode->PrevSiblingNode = resNode->PrevSiblingNode;
		}
		parent->UldManager.UpdateDrawNodeList();
	}

	public unsafe static void LinkNodeAtStart(AtkResNode* resNode, AtkUnitBase* parent)
	{
		AtkResNode* rootNode;
		rootNode = (resNode->ParentNode = parent->RootNode);
		resNode->PrevSiblingNode = rootNode->ChildNode;
		resNode->NextSiblingNode = null;
		if (rootNode->ChildNode->NextSiblingNode != null)
		{
			rootNode->ChildNode->NextSiblingNode = resNode;
		}
		rootNode->ChildNode = resNode;
		parent->UldManager.UpdateDrawNodeList();
	}

	public unsafe static void UnlinkNodeAtStart(AtkResNode* resNode, AtkUnitBase* parent)
	{
		if (IsAddonReady(parent) && parent->RootNode->ChildNode->NodeID == resNode->NodeID)
		{
			AtkResNode* rootNode;
			rootNode = parent->RootNode;
			if (resNode->PrevSiblingNode != null)
			{
				resNode->PrevSiblingNode->NextSiblingNode = null;
			}
			rootNode->ChildNode = resNode->PrevSiblingNode;
			parent->UldManager.UpdateDrawNodeList();
		}
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
}
