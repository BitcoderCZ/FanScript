using MathUtils.Vectors;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FanScript.Midi
{
    internal sealed class FcSongBuilder
    {
        private readonly ConvertSettings settings;

        private readonly ChannelTimeInfo[] channelTime = new ChannelTimeInfo[MidiConverter.MaxNumbChannels];
        private readonly Dictionary<int, Note> playingNotes = new(); // also array, Note?
        private readonly SongStats stats = new();
        private readonly FcSong.Channel[] channels = new FcSong.Channel[MidiConverter.MaxNumbChannels];
        private readonly HashSet<byte> usedSounds = [6]; // piano - default

        public FcSongBuilder(ConvertSettings settings)
        {
            this.settings = settings;

            for (int i = 0; i < channelTime.Length; i++)
                channelTime[i] = new ChannelTimeInfo(this.settings.FrameStep);

            for (int i = 0; i < channels.Length; i++)
                channels[i] = new FcSong.Channel();
        }

        public FcSong Build()
        {
            int channelCount = 0;

            return new FcSong(channels.Where((channel, index)
                =>
                {
                    bool used = stats.UsedChannels[index];

                    if (used && channelCount++ < settings.MaxChannels)
                        return true;

                    return false;
                }), usedSounds)
            {
                MinNote = stats.MinNote,
                MaxNote = stats.MaxNote,
                MaxPos = stats.MaxPos,
            };
        }

        public void Clear()
        {
            for (int i = 0; i < channelTime.Length; i++)
                channelTime[i] = new ChannelTimeInfo(settings.FrameStep);

            playingNotes.Clear();
            stats.Clear();
            for (int i = 0; i < channels.Length; i++)
                channels[i] = new FcSong.Channel();

            usedSounds.Clear();
            usedSounds.Add(6); // piano - default
        }

        public void PlayNote(int channel, Note note, TimeSpan deltaTime)
        {
            ChannelTimeInfo time = getTime(channel);

            if (time.CurrentFrame != 0)
                time.AddDeltaTime(deltaTime);

            if (time.CurrentTime >= time.FrameTime)
            {
                playNote(note, channel);

                time.StepFrame();
            }
        }
        public void EndPlayNote(int channel, Note note, TimeSpan deltaTime)
        {
            ChannelTimeInfo time = getTime(channel);

            endPlayNote(note, channel);

            time.AddDeltaTime(deltaTime);
        }

        public void SetInstrument(FourBitNumber channel, SevenBitNumber instrument)
        {
            ChannelTimeInfo time = getTime(channel);

            if (time.CurrentFrame * settings.FrameStep > settings.MaxFrames)
                return;

            byte sound = (byte)Utils.InstrumentToFcSound(instrument);

            ref ChannelFrame frame = ref getChannelFrame(channel, time.CurrentFrame);
            frame.NewSound = sound;

            usedSounds.Add(sound);
        }

        private void playNote(Note note, int channel)
        {
            if (channel < 0 || channel >= MidiConverter.MaxNumbChannels)
            {
                Console.WriteLine($"Out of bounds channel ({channel})");
                return;
            }

            ChannelTimeInfo time = getTime(channel);

            if (time.CurrentFrame * settings.FrameStep > settings.MaxFrames)
                return;

            if (playingNotes.TryGetValue(channel, out Note? currentlyPlaying))
            {
                Console.WriteLine($"Tried to start the note playing on channel {channel}, but one is aready playing ({currentlyPlaying})");
            }

            playingNotes[channel] = note;

            byte noteNumb = toNumb(note);

            stats.ChannelUsed(channel);
            stats.NotePlayed(noteNumb);
            stats.NotePlaced(time.CurrentFrame);

            Console.WriteLine($"Started playing note '{note}' ({note.NoteNumber}) - {time.CurrentTime.Minutes}:{time.CurrentTime.Seconds}.{time.CurrentTime.Milliseconds}.{time.CurrentTime.Microseconds} {time.FrameTime.Minutes}:{time.FrameTime.Seconds}.{time.FrameTime.Milliseconds}.{time.FrameTime.Microseconds} ({time.CurrentFrame})");

            ref ChannelFrame frame = ref getChannelFrame(channel, time.CurrentFrame);
            frame.NewNote = noteNumb;
        }

        private void endPlayNote(Note note, int channel)
        {
            if (channel < 0 || channel >= MidiConverter.MaxNumbChannels)
            {
                Console.WriteLine($"Out of bounds channel ({channel})");
                return;
            }

            ChannelTimeInfo time = getTime(channel);

            if (time.CurrentFrame * settings.FrameStep > settings.MaxFrames)
                return;

            if (!playingNotes.TryGetValue(channel, out Note? currentlyPlaying))
            {
                Console.WriteLine($"Tried to end the note playing on channel {channel}, but nothing was playing");
                return;
            }
            else if (note.NoteNumber != currentlyPlaying.NoteNumber)
            {
                Console.WriteLine($"Tried to end the note playing on channel {channel} ({currentlyPlaying}), but it didn't match the note ({note})");
                return;
            }

            playingNotes.Remove(channel);

            stats.ChannelUsed(channel);
            stats.NotePlayed(toNumb(note));
            stats.NotePlaced(time.CurrentFrame);

            Console.WriteLine($"Stopped playing note '{note}' ({note.NoteNumber}) - {time.CurrentTime.Minutes}:{time.CurrentTime.Seconds}.{time.CurrentTime.Milliseconds}.{time.CurrentTime.Microseconds} {time.FrameTime.Minutes}:{time.FrameTime.Seconds}.{time.FrameTime.Milliseconds}.{time.FrameTime.Microseconds} ({time.CurrentFrame})");

            //stepFrame();

            ref ChannelFrame frame = ref getChannelFrame(channel, time.CurrentFrame);
            frame.StopCurrentNote = true;
        }

        private ChannelTimeInfo getTime(int channel)
            => channelTime[channel];

        private byte toNumb(Note note)
        {
            return (byte)note.NoteName;
        }

        private ref ChannelFrame getChannelFrame(int channelIndex, int frame)
        {
            var channel = channels[channelIndex];

            if (frame >= channel.Frames.Count)
                CollectionsMarshal.SetCount(channel.Frames, frame + 1);

            Span<ChannelFrame> span = CollectionsMarshal.AsSpan(channel.Frames);
            return ref span[frame];
        }

        private class SongStats
        {
            public readonly bool[] UsedChannels = new bool[MidiConverter.MaxNumbChannels];
            public int MaxPos = -1;
            public int MinNote = int.MaxValue;
            public int MaxNote = int.MinValue;

            public void ChannelUsed(int channel)
            {
                Debug.Assert(channel >= 0 && channel < MidiConverter.MaxNumbChannels);
                UsedChannels[channel] = true;
            }

            public void NotePlayed(int noteNumb)
            {
                if (noteNumb < MinNote)
                    MinNote = noteNumb;
                if (noteNumb > MaxNote)
                    MaxNote = noteNumb;
            }

            public void NotePlaced(int pos)
            {
                if (pos > MaxPos)
                    MaxPos = pos;
            }

            public void Clear()
            {
                Array.Clear(UsedChannels);
                MaxPos = -1;
                MinNote = int.MaxValue;
                MaxNote = int.MinValue;
            }
        }
    }
}
