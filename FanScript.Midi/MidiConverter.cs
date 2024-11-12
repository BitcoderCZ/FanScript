using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;

namespace FanScript.Midi;

public sealed class MidiConverter
{
    // TODO: per note and per channel volume, combine, rescale to 0-1, in builder rescale to note range

    public const int MaxNumbChannels = 10;

    private readonly MidiFile file;
    private readonly MidiConvertSettings settings;

    private long microsecondsPerQuaterNote;
    private readonly short ticksPerQuaterNote;

    private readonly FcSongBuilder builder;

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
            case SmpteTimeDivision: // TODO:
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
                ProcessTrackChunk(trackChunk);
            else
                Console.WriteLine(midiChunk.GetType());
        }

        return builder.Build();
    }

    private void ProcessTrackChunk(TrackChunk chunk)
    {
        foreach (var @event in chunk.Events)
        {
            switch (@event)
            {
                case SetTempoEvent setTempo:
                    HandleSetTempo(setTempo);
                    break;
                case SequenceTrackNameEvent sequenceTrackName:
                    HandleSequenceTractName(sequenceTrackName);
                    break;
                case ControlChangeEvent controlChange:
                    HandleControlChange(controlChange);
                    break;
                case PitchBendEvent pitchBend:
                    HandlePitchBend(pitchBend);
                    break;
                case NoteOnEvent noteOn:
                    HandleNoteOn(noteOn);
                    break;
                case NoteOffEvent noteOff:
                    HandleNoteOff(noteOff);
                    break;
                case ProgramChangeEvent programChange:
                    HandleProgramChange(programChange);
                    break;
                default:
                    Console.WriteLine(@event.GetType());
                    break;
            }
        }
    }

    private void HandleSetTempo(SetTempoEvent @event)
    {
        microsecondsPerQuaterNote = @event.MicrosecondsPerQuarterNote;
        Console.WriteLine($"Set tempo to {@event.MicrosecondsPerQuarterNote} mc/qnote ({@event.MicrosecondsPerQuarterNote / 1000f} ms/qnote)");
    }

    private static void HandleSequenceTractName(SequenceTrackNameEvent @event)
        => Console.WriteLine($"Sequence track name: '{@event.Text}'");

    private static void HandleControlChange(ControlChangeEvent @event)
        => Console.WriteLine($"Control event: Channel: {@event.Channel}, Value: {@event.ControlValue}, Number: {@event.ControlNumber} (Delta time: {@event.DeltaTime})");

    private static void HandlePitchBend(PitchBendEvent @event)
    {
        if (@event.PitchValue == 0x2000)
            Console.WriteLine($"Pitch bend: Channel: {@event.Channel}, Pitch Value: default ({0x2000}) (Delta time: {@event.DeltaTime})");
        else
            Console.WriteLine($"Pitch bend: Channel: {@event.Channel}, Pitch Value: {@event.PitchValue} (Delta time: {@event.DeltaTime})");
    }

    private void HandleNoteOn(NoteOnEvent @event)
    {
        Note note = Note.Get(@event.NoteNumber);
        TimeSpan deltaTime = GetDeltaTimeTime(@event.DeltaTime);

        builder.PlayNote(@event.Channel, deltaTime, note, @event.Velocity);
    }

    private void HandleNoteOff(NoteOffEvent @event)
    {
        Note note = Note.Get(@event.NoteNumber);
        TimeSpan deltaTime = GetDeltaTimeTime(@event.DeltaTime);

        builder.EndPlayNote(@event.Channel, deltaTime, note);
    }

    private void HandleProgramChange(ProgramChangeEvent @event)
    {
        TimeSpan deltaTime = GetDeltaTimeTime(@event.DeltaTime);

        builder.SetInstrument(@event.Channel, deltaTime, @event.ProgramNumber);
    }

    #region Utils
    private TimeSpan GetDeltaTimeTime(long deltaTime)
    {
        TimeSpan span = TimeSpan.FromMicroseconds((microsecondsPerQuaterNote / ticksPerQuaterNote) * deltaTime);
        return span;
    }
    #endregion
}
