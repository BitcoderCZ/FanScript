using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    public sealed class FcSong
    {
        public static readonly Vector2I ChannelSize = new Vector2I(2, 8);

        public readonly List<Channel> Channels;

        public readonly HashSet<byte> UsedSounds;

        public FcSong()
        {
            Channels = new List<Channel>();
            UsedSounds = new HashSet<byte>();
        }
        public FcSong(IEnumerable<Channel> channels, IEnumerable<byte> usedSounds)
        {
            Channels = new List<Channel>(channels);
            UsedSounds = new HashSet<byte>(usedSounds);
        }

        public (Vector3I Size, List<Vector3US> Blocks) ToBlocks()
        {
            int length = Channels.Max(channel => channel.Frames.Count);

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

            for (ushort fIndex = 0; fIndex < length; fIndex++)
            {
                for (int cIndex = 0; cIndex < Channels.Count; cIndex++)
                {
                    Channel channel = Channels[cIndex];
                    if (fIndex >= channel.Frames.Count)
                        continue;

                    Vector3US pos = new Vector3US(fIndex, 0, (ushort)cIndex * ChannelSize.X);

                    ChannelFrame frame = channel.Frames[fIndex];

                    byte noteValue = 0;

                    if (frame.StopCurrentNote)
                        noteValue |= 0b_1000_0000;

                    if (frame.StartNewNote)
                        noteValue |= Math.Min((byte)(frame.NewNote + 1), (byte)0b_0111_1111);

                    setBinary(noteValue, pos);

                    if (frame.SetNewSound)
                        setBinary(frame.NewSound, pos + new Vector3US(0, 0, 1));
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
            public readonly List<ChannelFrame> Frames;

            public Channel()
            {
                Frames = new List<ChannelFrame>();
            }
            public Channel(List<ChannelFrame> frames)
            {
                Frames = frames;
            }
        }
    }
}
