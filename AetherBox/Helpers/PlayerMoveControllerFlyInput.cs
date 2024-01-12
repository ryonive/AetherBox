using System.Runtime.InteropServices;

namespace AetherBox.Helpers;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct PlayerMoveControllerFlyInput
{
	[FieldOffset(0)]
	public float Forward;

	[FieldOffset(4)]
	public float Left;

	[FieldOffset(8)]
	public float Up;

	[FieldOffset(12)]
	public float Turn;

	[FieldOffset(16)]
	public float u10;

	[FieldOffset(20)]
	public byte DirMode;

	[FieldOffset(21)]
	public byte HaveBackwardOrStrafe;
}
