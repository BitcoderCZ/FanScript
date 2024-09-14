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
        private readonly MidiConvertSettings settings;

        private readonly ChannelTimeInfo[] channelTime = new ChannelTimeInfo[MidiConverter.MaxNumbChannels];
        private readonly Dictionary<int, Note> playingNotes = new(); // also array, Note?
        private readonly SongStats stats = new();
        private readonly FcSong.Channel[] channels = new FcSong.Channel[MidiConverter.MaxNumbChannels];

        public FcSongBuilder(MidiConvertSettings settings)
        {
            this.settings = settings;

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
                }));
        }

        public void Clear()
        {
            Array.Clear(channelTime);
            playingNotes.Clear();
            stats.Clear();
            for (int i = 0; i < channels.Length; i++)
                channels[i] = new FcSong.Channel();
        }

        public void PlayNote(byte channel, TimeSpan deltaTime, Note note, float velocity)
        {
            if (getLongDelta(channel, deltaTime, out long longDelta))
                return;

            if (channel < 0 || channel >= MidiConverter.MaxNumbChannels)
            {
                Console.WriteLine($"[{channel}] Out of bounds channel");
                return;
            }

            if (playingNotes.TryGetValue(channel, out Note? currentlyPlaying))
                Console.WriteLine($"[{channel}] Tried to start note, but one is aready playing ({currentlyPlaying})");

            playingNotes[channel] = note;

            byte noteNumb = noteToNumb(note);
            byte delta = addLongDelta(channel, longDelta);
            byte bVelocity = (byte)Math.Min(velocity * 255f, 255f);

            stats.ChannelUsed(channel);

            ChannelTimeInfo time = channelTime[channel];
            Console.WriteLine($"[{channel}] Started playing note '{note}' ({note.NoteNumber}) - {time.CurrentTime.Minutes}:{time.CurrentTime.Seconds}.{time.CurrentTime.Milliseconds}.{time.CurrentTime.Microseconds} Frame: {time.CurrentFrame}");

            channels[channel].Events.Add(new ChannelEvent(ChannelEventType.PlayNote, delta, noteNumb, bVelocity));
        }
        public void EndPlayNote(byte channel, TimeSpan deltaTime, Note note)
        {
            if (getLongDelta(channel, deltaTime, out long longDelta))
                return;

            if (channel < 0 || channel >= MidiConverter.MaxNumbChannels)
            {
                Console.WriteLine($"Out of bounds channel ({channel})");
                return;
            }

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

            byte delta = addLongDelta(channel, longDelta);

            stats.ChannelUsed(channel);

            ChannelTimeInfo time = channelTime[channel];
            Console.WriteLine($"[{channel}] Stopped playing note '{note}' ({note.NoteNumber}) - {time.CurrentTime.Minutes}:{time.CurrentTime.Seconds}.{time.CurrentTime.Milliseconds}.{time.CurrentTime.Microseconds} Frame: {time.CurrentFrame}");

            channels[channel].Events.Add(new ChannelEvent(ChannelEventType.StopCurrentNote, delta));
        }

        public void SetInstrument(byte channel, TimeSpan deltaTime, SevenBitNumber instrument)
        {
            if (getLongDelta(channel, deltaTime, out long longDelta))
                return;

            byte sound = (byte)Utils.InstrumentToFcSound(instrument);

            byte delta = addLongDelta(channel, longDelta);

            Console.WriteLine($"[{channel}] Set instrument to '{instrument}' (sound: {sound}");

            channels[channel].Events.Add(new ChannelEvent(ChannelEventType.SetInstrument, delta, sound));
        }

        private byte noteToNumb(Note note)
        {
            return (byte)note.NoteName;
        }

        private bool getLongDelta(byte channel, TimeSpan deltaTime, out long longDelta)
        {
            ref ChannelTimeInfo time = ref channelTime[channel];

            longDelta = time.AddDelta(deltaTime);

            return time.CurrentFrame > settings.MaxFrames;
        }

        private byte addLongDelta(byte channel, long wholeDelta)
        {
            uint maxWait = (uint)(ChannelEvent.MaxDeltaTime + 255);

            while (wholeDelta > ChannelEvent.MaxDeltaTime)
            {
                uint wait;
                if (wholeDelta > maxWait)
                {
                    wait = maxWait;
                    wholeDelta -= maxWait;
                }
                else
                {
                    wait = (uint)wholeDelta;
                    wholeDelta = 0;
                }

                byte wait1;
                byte wait2;
                if (wait > ChannelEvent.MaxDeltaTime)
                {
                    wait1 = ChannelEvent.MaxDeltaTime;
                    wait -= ChannelEvent.MaxDeltaTime;
                    wait2 = (byte)wait;
                }
                else
                {
                    wait1 = (byte)wait;
                    wait2 = 0;
                }

                channels[channel].Events.Add(new ChannelEvent(ChannelEventType.Wait, wait1, wait2));
            }

            return (byte)wholeDelta;
        }

        private class SongStats
        {
            public readonly bool[] UsedChannels = new bool[MidiConverter.MaxNumbChannels];

            public void ChannelUsed(int channel)
            {
                Debug.Assert(channel >= 0 && channel < MidiConverter.MaxNumbChannels);
                UsedChannels[channel] = true;
            }

            public void Clear()
            {
                Array.Clear(UsedChannels);
            }
        }
    }
}
