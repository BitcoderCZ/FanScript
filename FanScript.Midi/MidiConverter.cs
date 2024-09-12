using MathUtils.Vectors;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    public sealed class MidiConverter
    {
        // TODO: per note and per channel volume, combine, rescale to 0-1, in builder rescale to note range

        public const int MaxNumbChannels = 10;

        private MidiFile file;
        private MidiConvertSettings settings;

        private long microsecondsPerQuaterNote;
        private readonly short ticksPerQuaterNote;

        private FcSongBuilder builder;

        public MidiConverter(MidiFile file, MidiConvertSettings? settings = null)
        {
            this.file = file;

            if (settings is null)
                this.settings = MidiConvertSettings.Default;
            else
                this.settings = new MidiConvertSettings(settings); // clone

            switch (file.TimeDivision)
            {
                case TicksPerQuarterNoteTimeDivision tpqnDivision:
                    ticksPerQuaterNote = tpqnDivision.TicksPerQuarterNote;
                    Console.WriteLine($"TicksPerQuarterNote: {ticksPerQuaterNote}");
                    break;
                case SmpteTimeDivision smpteDivision: // TODO:
                default:
                    throw new Exception($"Unknown TimeDivision '{file.TimeDivision.GetType()}'");
            }

            if (ticksPerQuaterNote == 0)
                throw new Exception($"TicksPerQuarterNote cannot be zero."); // divisor - can't be zero

            builder = new FcSongBuilder(this.settings);
        }

        public FcSong Convert()
        {
            builder.Clear();

            foreach (MidiChunk midiChunk in file.Chunks)
            {
                Console.WriteLine($"Chunk, Id: {midiChunk.ChunkId}");

                if (midiChunk is TrackChunk trackChunk)
                    processTrackChunk(trackChunk);
                else
                    Console.WriteLine(midiChunk.GetType());
            }

            return builder.Build();
        }

        private void processTrackChunk(TrackChunk chunk)
        {
            foreach (var @event in chunk.Events)
            {
                switch (@event)
                {
                    case SetTempoEvent setTempo:
                        handleSetTempo(setTempo);
                        break;
                    case SequenceTrackNameEvent sequenceTrackName:
                        handleSequenceTractName(sequenceTrackName);
                        break;
                    case ControlChangeEvent controlChange:
                        handleControlChange(controlChange);
                        break;
                    case PitchBendEvent pitchBend:
                        handlePitchBend(pitchBend);
                        break;
                    case NoteOnEvent noteOn:
                        handleNoteOn(noteOn);
                        break;
                    case NoteOffEvent noteOff:
                        handleNoteOff(noteOff);
                        break;
                    case ProgramChangeEvent programChange:
                        handleProgramChange(programChange);
                        break;
                    default:
                        Console.WriteLine(@event.GetType());
                        break;
                }
            }
        }

        private void handleSetTempo(SetTempoEvent @event)
        {
            microsecondsPerQuaterNote = @event.MicrosecondsPerQuarterNote;
            Console.WriteLine($"Set tempo to {@event.MicrosecondsPerQuarterNote} mc/qnote ({@event.MicrosecondsPerQuarterNote / 1000f} ms/qnote)");
        }

        private void handleSequenceTractName(SequenceTrackNameEvent @event)
        {
            Console.WriteLine($"Sequence track name: '{@event.Text}'");
        }

        private void handleControlChange(ControlChangeEvent @event)
        {
            Console.WriteLine($"Control event: Channel: {@event.Channel}, Value: {@event.ControlValue}, Number: {@event.ControlNumber} (Delta time: {@event.DeltaTime})");
        }

        private void handlePitchBend(PitchBendEvent @event)
        {
            if (@event.PitchValue == 0x2000)
                Console.WriteLine($"Pitch bend: Channel: {@event.Channel}, Pitch Value: default ({0x2000}) (Delta time: {@event.DeltaTime})");
            else
                Console.WriteLine($"Pitch bend: Channel: {@event.Channel}, Pitch Value: {@event.PitchValue} (Delta time: {@event.DeltaTime})");
        }

        private void handleNoteOn(NoteOnEvent @event)
        {
            Note note = Note.Get(@event.NoteNumber);
            TimeSpan deltaTime = getDeltaTimeTime(@event.DeltaTime);

            builder.PlayNote(@event.Channel, deltaTime, note, @event.Velocity);
        }

        private void handleNoteOff(NoteOffEvent @event)
        {
            Note note = Note.Get(@event.NoteNumber);
            TimeSpan deltaTime = getDeltaTimeTime(@event.DeltaTime);

            builder.EndPlayNote(@event.Channel, deltaTime, note);
        }

        private void handleProgramChange(ProgramChangeEvent @event)
        {
            TimeSpan deltaTime = getDeltaTimeTime(@event.DeltaTime);

            builder.SetInstrument(@event.Channel, deltaTime, @event.ProgramNumber);
        }

        #region Utils
        private TimeSpan getDeltaTimeTime(long deltaTime)
        {
            TimeSpan span = TimeSpan.FromMicroseconds((microsecondsPerQuaterNote / ticksPerQuaterNote) * deltaTime);
            return span;
        }
        #endregion
    }
}
