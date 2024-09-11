using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    public sealed class ConvertSettings
    {
        public static ConvertSettings Default => new ConvertSettings();

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

        public ConvertSettings()
        {
            MaxFrames = 60 * 60; // 60s
            FrameStep = 4;
            MaxChannels = 10;
        }
        public ConvertSettings(ConvertSettings other)
            : this(other.MaxFrames, other.FrameStep, other.MaxChannels)
        {

        }
        public ConvertSettings(int maxFrames, int frameStep, int maxChannels)
        {
            MaxFrames = maxFrames;
            FrameStep = frameStep;
            MaxChannels = maxChannels;
        }
    }
}
