using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    public struct ChannelEvent
    {
        public static readonly byte MaxDeltaTimeBits = 5;
        public static readonly byte MaxDeltaTime = (byte)((1 << MaxDeltaTimeBits) - 1);

        public ChannelEventType Type;
        public byte DeltaTime; // time between last event and this, in frames
        public byte Data0;
        public byte Data1;

        public ChannelEvent(ChannelEventType type, byte deltaTime, byte data0 = 0, byte data1 = 0)
        {
            Type = type;
            DeltaTime = deltaTime;
            Data0 = data0;
            Data1 = data1;
        }
    }

    // When adding types, make sure that the most used ones have the fewest bits set
    public enum ChannelEventType : byte
    {
        Wait = 0b_000,
        PlayNote = 0b_001,
        StopCurrentNote = 0b_010,
        SetInstrument = 0b_011,
        //SetChannelVolume,
    }
}
