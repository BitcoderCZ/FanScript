namespace FanScript.Midi;

public sealed class MidiConvertSettings
{
	public static MidiConvertSettings Default => new MidiConvertSettings();

	public int MaxFrames { get; set; }

	public int MaxChannels { get; set; }

	public MidiConvertSettings()
	{
		MaxFrames = 60 * 60; // 60s
		MaxChannels = 10;
	}
	public MidiConvertSettings(MidiConvertSettings other)
		: this(other.MaxFrames, other.MaxChannels)
	{

	}
	public MidiConvertSettings(int maxFrames, int maxChannels)
	{
		MaxFrames = maxFrames;
		MaxChannels = maxChannels;
	}
}
