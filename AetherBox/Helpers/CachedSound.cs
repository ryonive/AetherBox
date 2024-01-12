using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace AetherBox.Helpers;

internal class CachedSound
{
	internal float[] AudioData { get; private set; }

	internal WaveFormat WaveFormat { get; private set; }

	internal CachedSound(string audioFileName)
	{
		using AudioFileReader audioFileReader = new AudioFileReader(audioFileName);
		WaveFormat = audioFileReader.WaveFormat;
		List<float> wholeFile;
		wholeFile = new List<float>((int)(audioFileReader.Length / 4));
		float[] readBuffer;
		readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
		int samplesRead;
		while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
		{
			wholeFile.AddRange(readBuffer.Take(samplesRead));
		}
		AudioData = wholeFile.ToArray();
	}
}
