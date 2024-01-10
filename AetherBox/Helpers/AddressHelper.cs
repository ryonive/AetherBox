namespace AetherBox.Helpers;
internal static class AddressHelper
{
	public unsafe static T ReadField<T>(void* address, int offset) where T : unmanaged
	{
		return *(T*)((byte*)address + offset);
	}

	public unsafe static void WriteField<T>(void* address, int offset, T value) where T : unmanaged
	{
		*(T*)((byte*)address + offset) = value;
	}
}
