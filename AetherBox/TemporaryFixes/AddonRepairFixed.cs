// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.TemporaryFixes.AddonRepairFixed
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;
namespace AetherBox.TemporaryFixes;
[StructLayout(LayoutKind.Explicit, Size = 63464)]
[Addon(new string[] { "Repair" })]
public struct AddonRepairFixed
{
	[FieldOffset(0)]
	public AtkUnitBase AtkUnitBase;

	[FieldOffset(552)]
	public unsafe AtkTextNode* UnusedText1;

	[FieldOffset(560)]
	public unsafe AtkTextNode* JobLevel;

	[FieldOffset(568)]
	public unsafe AtkImageNode* JobIcon;

	[FieldOffset(576)]
	public unsafe AtkTextNode* JobName;

	[FieldOffset(584)]
	public unsafe AtkTextNode* UnusedText2;

	[FieldOffset(592)]
	public unsafe AtkComponentDropDownList* Dropdown;

	[FieldOffset(616)]
	public unsafe AtkComponentButton* RepairAllButton;

	[FieldOffset(624)]
	public unsafe AtkResNode* HeaderContainer;

	[FieldOffset(632)]
	public unsafe AtkTextNode* UnusedText3;

	[FieldOffset(640)]
	public unsafe AtkTextNode* NothingToRepairText;

	[FieldOffset(648)]
	public unsafe AtkComponentList* ItemList;
}
