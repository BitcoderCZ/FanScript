using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    public sealed class MidiConvertSettings
    {
        public static MidiConvertSettings Default => new MidiConvertSettings();

        public int MaxFrames { get; set; }

        private int frameStep;
        public int FrameStep
        {
            get => frameStep;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

                frameStep = value;
            }
        }

        public int MaxChannels { get; set; }

        public MidiConvertSettings()
        {
            MaxFrames = 60 * 60; // 60s
            FrameStep = 4;
            MaxChannels = 10;
        }
        public MidiConvertSettings(MidiConvertSettings other)
            : this(other.MaxFrames, other.FrameStep, other.MaxChannels)
        {

        }
        public MidiConvertSettings(int maxFrames, int frameStep, int maxChannels)
        {
            MaxFrames = maxFrames;
            FrameStep = frameStep;
            MaxChannels = maxChannels;
        }
    }
}
