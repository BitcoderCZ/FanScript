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
        public readonly List<Channel> Channels;

        public readonly HashSet<byte> UsedSounds;

        public int MinNote { get; set; }
        public int MaxNote { get; set; }

        public int MaxPos { get; set; }

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

        public (Vector3I Size, List<Vector3I> Blocks) ToBlocks()
        {
            int length = MaxPos + 1;

            double sqrt2 = Math.Sqrt(length);

            int noteRange = (MaxNote - MinNote) + 1;

            int ySize = Math.Max((int)(sqrt2 / noteRange), 1);

            int xzSize = length / ySize;

            int xSize = (int)Math.Sqrt(xzSize);
            int zSize = xzSize / xSize;

            while ((xSize * ySize * zSize) < length)
                xSize++;

            const int xStep = 1;
            int yStep = noteRange;
            int zStep = Channels.Count * 3;

            xSize *= xStep;
            ySize *= yStep;
            zSize *= zStep;

            List<Vector3I> blocks = new List<Vector3I>(length);

            int x = 0;
            int y = 0;
            int z = 0;
            for (int fIndex = 0; fIndex < length; fIndex++)
            {
                Vector3I pos = new Vector3I(x, y, z);

                for (int cIndex = 0; cIndex < Channels.Count; cIndex++)
                {
                    Channel channel = Channels[cIndex];
                    if (fIndex >= channel.Frames.Count)
                        continue;

                    ChannelFrame frame = channel.Frames[fIndex];

                    if (frame.StopCurrentNote)
                        blocks.Add(pos + new Vector3I(0, 0, cIndex * 3 + 0));

                    if (frame.StartNewNote)
                    {
                        Debug.Assert(frame.NewNote >= MinNote);
                        Debug.Assert(frame.NewNote <= MaxNote);
                        blocks.Add(pos + new Vector3I(0, frame.NewNote - MinNote, cIndex * 3 + 1));
                    }

                    if (frame.SetNewSound)
                        blocks.Add(pos + new Vector3I(0, frame.NewSound, cIndex * 3 + 2));
                }

                x += xStep;

                if (x >= xSize)
                {
                    x = 0;
                    z += zStep;

                    if (z >= zSize)
                    {
                        z = 0;
                        y += yStep;
                    }
                }
            }

            return (new Vector3I(xSize, ySize, zSize), blocks);
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
