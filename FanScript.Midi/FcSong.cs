using MathUtils.Vectors;

namespace FanScript.Midi;

public sealed class FcSong
{
    public static readonly Vector2I ChannelSize = new Vector2I(1, 8);

    public readonly List<Channel> Channels;

    public FcSong()
    {
        Channels = new List<Channel>();
    }
    public FcSong(IEnumerable<Channel> channels)
    {
        Channels = new List<Channel>(channels);
    }

    public (Vector3I Size, List<Vector3US> Blocks) ToBlocks()
    {
        int length = Channels.Max(channel => channel.Events.Count);

        //double sqrt2 = Math.Sqrt(length);

        //int noteRange = (MaxNote - MinNote) + 1;

        //int ySize = Math.Max((int)(sqrt2 / noteRange), 1);

        //int xzSize = length / ySize;

        //int xSize = (int)Math.Sqrt(xzSize);
        //int zSize = xzSize / xSize;

        //while ((xSize * ySize * zSize) < length)
        //    xSize++;

        //const int xStep = 1;
        //int yStep = noteRange;
        //int zStep = Channels.Count * 3;

        //xSize *= xStep;
        //ySize *= yStep;
        //zSize *= zStep;

        List<Vector3US> blocks = new List<Vector3US>(length);

        for (int channelIndex = 0; channelIndex < Channels.Count; channelIndex++)
        {
            Channel channel = Channels[channelIndex];

            Vector3US pos = new Vector3US(0, 0, (ushort)channelIndex * ChannelSize.X);

            for (ushort i = 0; i < channel.Events.Count; i++)
            {
                ChannelEvent cEvent = channel.Events[i];

                // channel event structure:
                // t - type
                // d - delta time since last event (in frames)
                // a - data0 (optional - depends on type)
                // b - data1 (optional - depends on type)
                // tttd_dddd - z_pos: 0
                // aaaa_aaaa - z_pos: 1
                // bbbb_bbbb - z_pos: 2

                setBinary((byte)((byte)cEvent.Type | (cEvent.DeltaTime << 3)), pos);

                pos.X++;

                switch (cEvent.Type)
                {
                    case ChannelEventType.Wait:
                        {
                            setBinary(cEvent.Data0, pos); // delay
                            pos.X++;
                        }
                        break;
                    case ChannelEventType.PlayNote:
                        if (cEvent.Data1 == 255)
                        {
                            setBinary(cEvent.Data0, pos); // note
                            pos.X++;
                        }
                        else
                        {
                            setBinary((byte)(cEvent.Data0 | 0b_1000_0000), pos); // note
                            pos.X++;
                            setBinary(cEvent.Data1, pos); // velocity
                            pos.X++;
                        }
                        break;
                    case ChannelEventType.SetInstrument:
                        setBinary(cEvent.Data0, pos); // the "instrument" - fc sound
                        pos.X++;
                        break;
                    case ChannelEventType.StopCurrentNote:
                    default:
                        break;
                }
            }
        }

        return (new Vector3I(length, 1, 1), blocks);

        void setBinary(byte value, Vector3US pos)
        {
            byte mask = 1;

            for (int i = 0; i < 8; i++)
            {
                if ((value & mask) == mask)
                    blocks.Add(pos);

                pos.Y++;
                mask <<= 1;
            }
        }
    }

    public sealed class Channel
    {
        public readonly List<ChannelEvent> Events;

        public Channel()
        {
            Events = new List<ChannelEvent>();
        }
        public Channel(List<ChannelEvent> events)
        {
            Events = events;
        }
    }
}
