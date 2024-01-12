using System.Runtime.InteropServices;

namespace AetherBox.Helpers;

[StructLayout(LayoutKind.Explicit, Size = 688)]
public struct CameraEx
{
	[FieldOffset(304)]
	public float DirH;

	[FieldOffset(308)]
	public float DirV;

	[FieldOffset(312)]
	public float InputDeltaHAdjusted;

	[FieldOffset(316)]
	public float InputDeltaVAdjusted;

	[FieldOffset(320)]
	public float InputDeltaH;

	[FieldOffset(324)]
	public float InputDeltaV;

	[FieldOffset(328)]
	public float DirVMin;

	[FieldOffset(332)]
	public float DirVMax;
}
