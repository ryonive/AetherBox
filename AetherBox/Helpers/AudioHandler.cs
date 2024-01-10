using System;
using System.Collections.Generic;
using System.IO;
using AetherBox.Helpers;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace AetherBox.Helpers;
public class AudioHandler
{
	private readonly IWavePlayer outputDevice;

	private readonly Dictionary<AudioTrigger, CachedSound> sounds;

	private readonly VolumeSampleProvider sampleProvider;

	private readonly MixingSampleProvider mixer;

	public float Volume
	{
		get
		{
			return sampleProvider.Volume;
		}
		set
		{
			sampleProvider.Volume = value;
		}
	}

	public AudioHandler(string soundsPath)
	{
		outputDevice = new WaveOutEvent();
		sounds = new Dictionary<AudioTrigger, CachedSound>
		{
			{
				AudioTrigger.Light,
				new CachedSound(Path.Combine(soundsPath, "Light.wav"))
			},
			{
				AudioTrigger.Strong,
				new CachedSound(Path.Combine(soundsPath, "Strong.wav"))
			},
			{
				AudioTrigger.Legendary,
				new CachedSound(Path.Combine(soundsPath, "Legendary.wav"))
			}
		};
		mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
		mixer.ReadFully = true;
		sampleProvider = new VolumeSampleProvider(mixer);
		outputDevice.Init(sampleProvider);
		outputDevice.Play();
	}

	private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
	{
		if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
		{
			return input;
		}
		if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
		{
			return new MonoToStereoSampleProvider(input);
		}
		throw new NotImplementedException("Not yet implemented this channel count conversion");
	}

	private void AddMixerInput(ISampleProvider input)
	{
		mixer.AddMixerInput(ConvertToRightChannelCount(input));
	}

	public void PlaySound(AudioTrigger trigger)
	{
		AddMixerInput(new WdlResamplingSampleProvider(new CachedSoundSampleProvider(sounds[trigger]), mixer.WaveFormat.SampleRate));
	}
}
