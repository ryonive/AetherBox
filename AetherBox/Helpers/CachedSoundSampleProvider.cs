using System;
using AetherBox.Helpers;
using NAudio.Wave;
namespace AetherBox.Helpers;
internal class CachedSoundSampleProvider : ISampleProvider
{
	private readonly CachedSound cachedSound;

	private long position;

	public WaveFormat WaveFormat => cachedSound.WaveFormat;

	public CachedSoundSampleProvider(CachedSound cachedSound)
	{
		this.cachedSound = cachedSound;
	}

	public int Read(float[] buffer, int offset, int count)
	{
		long samplesToCopy = Math.Min(cachedSound.AudioData.Length - position, count);
		Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
		position += samplesToCopy;
		return (int)samplesToCopy;
	}
}
