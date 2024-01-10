// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Helpers.CustomNodes
using System.Collections.Generic;
using AetherBox.Features;
namespace AetherBox.Helpers;
public static class CustomNodes
{
	private static readonly Dictionary<string, uint> NodeIds = new Dictionary<string, uint>();

	private static readonly Dictionary<uint, string> NodeNames = new Dictionary<uint, string>();

	private static uint _nextId = 1398018048u;

	public const int TargetHP = 1398013953;

	public const int SlideCastMarker = 1398013954;

	public const int TimeUntilGpMax = 1398013955;

	public const int ComboTimer = 1398013956;

	public const int PartyListStatusTimer = 1398013957;

	public const int InventoryGil = 1398013958;

	public const int GearPositionsBg = 1398013959;

	public const int ClassicSlideCast = 1398013961;

	public const int PaintingPreview = 1398013962;

	public const int AdditionalInfo = 1398013963;

	public const int CraftingGhostBar = 1398013964;

	public const int CraftingGhostText = 1398013965;

	public const int FelicitousToken = 1398014372;

	public const int SimpleTweaksNodeBase = 1398013952;

	public static uint Get(BaseFeature tweak, string label = "", int index = 0)
	{
		if (!string.IsNullOrEmpty(label))
		{
			return Get(tweak.GetType().Name + "::" + label, index);
		}
		return Get(tweak.GetType().Name ?? "", index);
	}

	public static uint Get(string name, int index = 0)
	{
		if (TryGet(name, index, out var id))
		{
			return id;
		}
		lock (NodeIds)
		{
			lock (NodeNames)
			{
				id = _nextId;
				_nextId += 16u;
				NodeIds.Add($"{name}#{index}", id);
				NodeNames.Add(id, $"{name}#{index}");
				return id;
			}
		}
	}

	public static bool TryGet(string name, out uint id)
	{
		return TryGet(name, 0, out id);
	}

	public static bool TryGet(string name, int index, out uint id)
	{
		return NodeIds.TryGetValue($"{name}#{index}", out id);
	}

	public static bool TryGet(uint id, out string name)
	{
		return NodeNames.TryGetValue(id, out name);
	}
}
