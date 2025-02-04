﻿using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace FanScript.Midi;

internal class Program
{
	private static Playback _playback = null!;

	private static void Main()
	{
		string path = "test.mid";
		MidiFile file;
		using (FileStream stream = File.OpenRead(path))
			file = MidiFile.Read(stream, new ReadingSettings());

		MidiConverter converter = new MidiConverter(file);

		FcSong song = converter.Convert();
		song.ToBlocks();

		Console.WriteLine("Converted, press any key to play...");
		Console.ReadKey(true);

		var outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");

		_playback = file.GetPlayback(outputDevice);
		_playback.Start();

		SpinWait.SpinUntil(() => !_playback.IsRunning);

		Console.WriteLine("Playback stopped or finished.");

		outputDevice.Dispose();
		_playback.Dispose();

		Console.ReadKey(true);
	}
}
